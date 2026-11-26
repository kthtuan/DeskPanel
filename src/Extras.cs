using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DeskPanel;

public sealed class MediaInfo
{
    public string? App { get; set; }
    public string? Title { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public double? Pos { get; set; }
    public double? Dur { get; set; }
    public bool Playing { get; set; }
    public string? ArtVer { get; set; }
}

public sealed class WeatherInfo
{
    public string City { get; set; } = "";
    public double? Temp { get; set; }
    public double? Feels { get; set; }
    public double? Max { get; set; }
    public double? Min { get; set; }
    public int? Code { get; set; }
    public string? Desc { get; set; }
    public string? Icon { get; set; }
    public string? Error { get; set; }
    public string? Detail { get; set; }
    public string? Source { get; set; }
}

/// <summary>
/// Doc bai dang phat tren Windows (Spotify, YouTube tren trinh duyet, Windows Media Player...)
/// qua Global System Media Transport Controls (cung nguon voi bang dieu khien am luong cua Windows).
/// </summary>
public sealed class MediaWatcher : BackgroundService
{
    public MediaInfo? Latest { get; private set; }
    public byte[]? Art { get; private set; }
    public string? ArtType { get; private set; }

#if WINDOWS
    Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager? _mgr;
    string? _artKey;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await Tick(); }
            catch (Exception ex) { Log.Write("Media: " + ex.Message); _mgr = null; Latest = null; }
            await Task.Delay(1000, ct).ContinueWith(_ => { });
        }
    }

    async Task Tick()
    {
        _mgr ??= await Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        var s = _mgr.GetCurrentSession();
        if (s == null) { Latest = null; return; }
        var props = await s.TryGetMediaPropertiesAsync();
        var tl = s.GetTimelineProperties();
        var pb = s.GetPlaybackInfo();
        bool playing = pb.PlaybackStatus == Windows.Media.Control.GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        double? dur = tl.EndTime > TimeSpan.Zero ? (tl.EndTime - tl.StartTime).TotalSeconds : null;
        double pos = tl.Position.TotalSeconds;
        if (playing && tl.LastUpdatedTime.Year > 2000)
            pos += (DateTimeOffset.Now - tl.LastUpdatedTime).TotalSeconds;
        if (dur != null) pos = Math.Clamp(pos, 0, dur.Value);

        var key = $"{s.SourceAppUserModelId}|{props?.Title}|{props?.Artist}";
        if (key != _artKey)
        {
            _artKey = key;
            Art = null; ArtType = null;
            try
            {
                if (props?.Thumbnail != null)
                {
                    using var stream = await props.Thumbnail.OpenReadAsync();
                    using var ms = new MemoryStream();
                    await System.IO.WindowsRuntimeStreamExtensions.AsStreamForRead(stream).CopyToAsync(ms);
                    if (ms.Length > 0 && ms.Length < 4_000_000) { Art = ms.ToArray(); ArtType = string.IsNullOrEmpty(stream.ContentType) ? "image/png" : stream.ContentType; }
                }
            }
            catch (Exception ex) { Log.Write("Media art: " + ex.Message); }
        }

        Latest = new MediaInfo
        {
            App = AppName(s.SourceAppUserModelId),
            Title = props?.Title, Artist = props?.Artist, Album = props?.AlbumTitle,
            Pos = dur != null ? Math.Round(pos, 1) : null, Dur = dur != null ? Math.Round(dur.Value, 1) : null,
            Playing = playing,
            ArtVer = Art == null ? null : ((uint)key.GetHashCode()).ToString("x"),
        };
    }

    static string AppName(string id)
    {
        var l = id.ToLowerInvariant();
        if (l.Contains("spotify")) return "Spotify";
        if (l.Contains("chrome")) return "Chrome";
        if (l.Contains("msedge")) return "Edge";
        if (l.Contains("firefox")) return "Firefox";
        if (l.Contains("zunemusic") || l.Contains("media")) return "Media Player";
        var n = Path.GetFileNameWithoutExtension(id.Split('!')[0]);
        return string.IsNullOrEmpty(n) ? id : n;
    }

    public async Task<bool> Control(string cmd)
    {
        _mgr ??= await Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        var s = _mgr.GetCurrentSession();
        if (s == null) return false;
        return cmd switch
        {
            "toggle" => await s.TryTogglePlayPauseAsync(),
            "next" => await s.TrySkipNextAsync(),
            "prev" => await s.TrySkipPreviousAsync(),
            _ => false,
        };
    }
#else
    // May khong phai Windows: du lieu gia de thu giao dien
    readonly DateTime _t0 = DateTime.UtcNow;
    bool _playing = true;
    protected override Task ExecuteAsync(CancellationToken ct) => Task.CompletedTask;
    public MediaInfo? Demo()
    {
        var p = (DateTime.UtcNow - _t0).TotalSeconds % 214;
        return new MediaInfo { App = "Spotify", Title = "Midnight Circuit (demo)", Artist = "Neon Harbor", Album = "Low Tide Lights", Pos = Math.Round(p, 1), Dur = 214, Playing = _playing };
    }
    public Task<bool> Control(string cmd) { if (cmd == "toggle") _playing = !_playing; return Task.FromResult(true); }
#endif

    public MediaInfo? Current =>
#if WINDOWS
        Latest;
#else
        Demo();
#endif
}

/// <summary>Thoi tiet tu Open-Meteo (mien phi, khong can khoa). PC lay 15 phut/lan.</summary>
public sealed class WeatherService : BackgroundService
{
    readonly ConfigStore _store;
    static readonly HttpClient Http = CreateHttp();
    public WeatherInfo? Latest { get; private set; }
    public string Status { get; private set; } = "Chưa lấy";
    string? _city; double _lat = double.NaN, _lon; string _place = "";
    DateTime _lastOk = DateTime.MinValue;

    // Bien moi truong chi de thu nghiem
    static readonly string GeoBase = Environment.GetEnvironmentVariable("DESKPANEL_GEO_BASE") ?? "https://geocoding-api.open-meteo.com";
    static readonly string MetBase = Environment.GetEnvironmentVariable("DESKPANEL_MET_BASE") ?? "https://api.met.no";
    static readonly string WttrBase = Environment.GetEnvironmentVariable("DESKPANEL_WTTR_BASE") ?? "https://wttr.in";
    static readonly string ApiBase = Environment.GetEnvironmentVariable("DESKPANEL_WX_BASE") ?? "https://api.open-meteo.com";

    public WeatherService(ConfigStore store) { _store = store; }

    // Uu tien IPv4: mang co IPv6 hong lam ket noi treo toi het gio
    static HttpClient CreateHttp()
    {
        var h = new SocketsHttpHandler
        {
            ConnectTimeout = TimeSpan.FromSeconds(10),
            ConnectCallback = async (ctx, ct) =>
            {
                var addrs = await System.Net.Dns.GetHostAddressesAsync(ctx.DnsEndPoint.Host, ct);
                Exception? last = null;
                foreach (var a in addrs.OrderBy(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 0 : 1))
                {
                    var sock = new System.Net.Sockets.Socket(a.AddressFamily, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp) { NoDelay = true };
                    try
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        cts.CancelAfter(TimeSpan.FromSeconds(6));
                        await sock.ConnectAsync(new System.Net.IPEndPoint(a, ctx.DnsEndPoint.Port), cts.Token);
                        return new System.Net.Sockets.NetworkStream(sock, ownsSocket: true);
                    }
                    catch (Exception ex) { sock.Dispose(); last = ex; if (ct.IsCancellationRequested) throw; }
                }
                throw last ?? new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.HostNotFound);
            },
        };
        var c = new HttpClient(h) { Timeout = TimeSpan.FromSeconds(25) };
        c.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "DeskPanel/" + (typeof(WeatherService).Assembly.GetName().Version?.ToString(3) ?? "0") + " (personal PC dashboard widget)");
        return c;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (Environment.GetEnvironmentVariable("DESKPANEL_FAKE_WEATHER") == "1")
        { Latest = new WeatherInfo { City = "TP. Hồ Chí Minh", Temp = 29, Feels = 33, Max = 32, Min = 24, Code = 2, Desc = "Có mây", Icon = "⛅" }; Status = "Dữ liệu giả"; return; }
        DateTime nextTry = DateTime.MinValue; int fails = 0; string? want = null;
        while (!ct.IsCancellationRequested)
        {
            var city = _store.Current.WeatherCity?.Trim();
            if (string.IsNullOrEmpty(city)) { Latest = null; want = null; Status = "Đang tắt"; fails = 0; }
            else
            {
                bool changed = city != want;
                if (changed) { want = city; nextTry = DateTime.MinValue; fails = 0; }
                if (DateTime.UtcNow >= nextTry)
                {
                    try
                    {
                        await Fetch(city, ct);
                        fails = 0; nextTry = DateTime.UtcNow.AddMinutes(15);
                    }
                    catch (Exception ex) when (!ct.IsCancellationRequested)
                    {
                        fails++;
                        var (why, detail) = Explain(ex);
                        Log.Write($"Thoi tiet ({city}) lan {fails}: {ex}");
                        Status = $"{why} [{detail}]";
                        // Con du lieu cu (< 1 gio) thi giu lai, khong bao loi
                        if (Latest == null || Latest.Error != null || (DateTime.UtcNow - _lastOk).TotalMinutes > 60 || changed)
                            Latest = new WeatherInfo { City = city, Error = why, Detail = detail };
                        // thu lai sau 1, 2, 5, 10 phut
                        nextTry = DateTime.UtcNow.AddMinutes(fails switch { 1 => 1, 2 => 2, 3 => 5, _ => 10 });
                    }
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(5), ct).ContinueWith(_ => { });
        }
    }

    static (string, string) Explain(Exception ex)
    {
        var inner = ex; while (inner.InnerException != null) inner = inner.InnerException;
        string detail = ex is AllSourcesFailedException ? ex.Message : (inner.GetType().Name + ": " + inner.Message).Replace('\n', ' ');
        if (detail.Length > 200) detail = detail[..200] + "…";
        string why = ex switch
        {
            AllSourcesFailedException => "Các nguồn thời tiết đều không trả lời",
            CityNotFoundException => "Không tìm thấy thành phố (thử tên tiếng Anh hoặc nhập 'vĩ độ,kinh độ')",
            TaskCanceledException or OperationCanceledException => "Hết thời gian chờ máy chủ thời tiết",
            HttpRequestException { StatusCode: not null } h => $"Máy chủ thời tiết trả lỗi {(int)h.StatusCode}",
            HttpRequestException when inner is System.Net.Sockets.SocketException se && se.SocketErrorCode is System.Net.Sockets.SocketError.HostNotFound or System.Net.Sockets.SocketError.TryAgain or System.Net.Sockets.SocketError.NoData
                => "Không phân giải được tên miền (DNS)",
            HttpRequestException when inner is System.Security.Authentication.AuthenticationException
                => "Lỗi chứng chỉ HTTPS (phần mềm diệt virus/proxy chặn?)",
            HttpRequestException => "Không kết nối được máy chủ thời tiết",
            System.Text.Json.JsonException or InvalidOperationException or FormatException => "Dữ liệu thời tiết không đọc được",
            _ => "Không lấy được thời tiết",
        };
        return (why, detail);
    }

    async Task Fetch(string city, CancellationToken ct)
    {
        if (city != _city || double.IsNaN(_lat))
        {
            _lat = double.NaN; _city = city;
            if (TryCoords(city, out var la, out var lo)) { _lat = la; _lon = lo; _place = city; }
            else if (KnownCities.TryGetValue(Norm(city), out var k)) { _lat = k.Lat; _lon = k.Lon; _place = k.Name; }
            else
            {
                JsonNode? r = null;
                foreach (var lang in new[] { "vi", "en" })
                {
                    var g = await Http.GetFromJsonAsync<JsonObject>(
                        $"{GeoBase}/v1/search?count=1&format=json&language={lang}&name=" + Uri.EscapeDataString(city), ct);
                    r = g?["results"]?[0];
                    if (r != null) break;
                }
                if (r == null) throw new CityNotFoundException(city);
                _lat = Num(r["latitude"]) ?? double.NaN; _lon = Num(r["longitude"]) ?? 0;
                _place = r["name"]?.GetValue<string>() ?? city;
            }
        }
        if (double.IsNaN(_lat)) throw new InvalidOperationException("Khong co toa do");
        // Thu lan luot cac nguon; nguon nao bi gioi han (429) thi tam nghi
        var errs = new List<string>();
        foreach (var (name, fn) in new (string, Func<CancellationToken, Task<WeatherInfo>>)[] { ("Open-Meteo", OpenMeteo), ("MET Norway", MetNo), ("wttr.in", Wttr) })
        {
            if (_cool.TryGetValue(name, out var until) && DateTime.UtcNow < until) { errs.Add($"{name}: tạm nghỉ tới {until.ToLocalTime():HH:mm}"); continue; }
            try
            {
                var w = await fn(ct);
                w.City = _place; w.Source = name;
                Latest = w; _lastOk = DateTime.UtcNow;
                Status = $"OK ({name}), cập nhật lúc {DateTime.Now:HH:mm}" + (errs.Count > 0 ? " · " + string.Join("; ", errs) : "");
                return;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                var (why, detail) = Explain(ex);
                Log.Write($"Thoi tiet {name}: {detail}");
                errs.Add($"{name}: {why}");
                if (ex is HttpRequestException { StatusCode: System.Net.HttpStatusCode.TooManyRequests or System.Net.HttpStatusCode.Forbidden })
                    _cool[name] = DateTime.UtcNow.AddMinutes(60);
            }
        }
        throw new AllSourcesFailedException(string.Join("; ", errs));
    }

    readonly Dictionary<string, DateTime> _cool = new();

    async Task<WeatherInfo> OpenMeteo(CancellationToken ct)
    {
        var url = FormattableString.Invariant($"{ApiBase}/v1/forecast?latitude={_lat:0.####}&longitude={_lon:0.####}&current=temperature_2m,apparent_temperature,weather_code,is_day&daily=temperature_2m_max,temperature_2m_min&timezone=auto&forecast_days=1");
        var w = await Http.GetFromJsonAsync<JsonObject>(url, ct);
        var cur = w?["current"] ?? throw new InvalidOperationException("Thieu muc current");
        var d = w?["daily"];
        int code = (int)(Num(cur["weather_code"]) ?? -1);
        bool day = (Num(cur["is_day"]) ?? 1) == 1;
        var (desc, icon) = Describe(code, day);
        return new WeatherInfo
        {
            Temp = Num(cur["temperature_2m"]), Feels = Num(cur["apparent_temperature"]),
            Max = Num(d?["temperature_2m_max"]?[0]), Min = Num(d?["temperature_2m_min"]?[0]),
            Code = code, Desc = desc, Icon = icon,
        };
    }

    // Vien Khi tuong Na Uy: mien phi, khong can khoa
    async Task<WeatherInfo> MetNo(CancellationToken ct)
    {
        var url = FormattableString.Invariant($"{MetBase}/weatherapi/locationforecast/2.0/compact?lat={_lat:0.###}&lon={_lon:0.###}");
        var w = await Http.GetFromJsonAsync<JsonObject>(url, ct);
        var ts = w?["properties"]?["timeseries"] as JsonArray ?? throw new InvalidOperationException("Thieu timeseries");
        if (ts.Count == 0) throw new InvalidOperationException("timeseries rong");
        var first = ts[0]!["data"];
        double? temp = Num(first?["instant"]?["details"]?["air_temperature"]);
        double? hum = Num(first?["instant"]?["details"]?["relative_humidity"]);
        string sym = first?["next_1_hours"]?["summary"]?["symbol_code"]?.GetValue<string>()
                  ?? first?["next_6_hours"]?["summary"]?["symbol_code"]?.GetValue<string>() ?? "";
        double? max = null, min = null;
        foreach (var t in ts.Take(24))
        {
            var v = Num(t?["data"]?["instant"]?["details"]?["air_temperature"]);
            if (v == null) continue;
            max = max == null ? v : Math.Max(max.Value, v.Value);
            min = min == null ? v : Math.Min(min.Value, v.Value);
        }
        int code = MetToWmo(sym);
        var (desc, icon) = Describe(code, !sym.EndsWith("_night"));
        return new WeatherInfo { Temp = temp, Feels = HeatIndex(temp, hum), Max = max, Min = min, Code = code, Desc = desc, Icon = icon };
    }

    static int MetToWmo(string s)
    {
        s = s.Replace("_day", "").Replace("_night", "").Replace("_polartwilight", "");
        if (s.Contains("thunder")) return 95;
        if (s.Contains("snow") || s.Contains("sleet")) return 71;
        if (s.Contains("heavyrainshowers")) return 82;
        if (s.Contains("rainshowers")) return 80;
        if (s.Contains("heavyrain")) return 65;
        if (s.Contains("lightrain")) return 51;
        if (s.Contains("rain")) return 61;
        if (s.Contains("fog")) return 45;
        return s switch { "clearsky" => 0, "fair" => 1, "partlycloudy" => 2, "cloudy" => 3, _ => -1 };
    }

    // Nhiet do cam nhan theo do am (cong thuc Steadman/NOAA rut gon)
    static double? HeatIndex(double? t, double? rh)
    {
        if (t == null || rh == null) return t;
        if (t < 26.7) return t;
        double f = t.Value * 9 / 5 + 32, r = rh.Value;
        double hi = -42.379 + 2.04901523 * f + 10.14333127 * r - .22475541 * f * r - .00683783 * f * f - .05481717 * r * r
                    + .00122874 * f * f * r + .00085282 * f * r * r - .00000199 * f * f * r * r;
        return Math.Round((hi - 32) * 5 / 9, 1);
    }

    async Task<WeatherInfo> Wttr(CancellationToken ct)
    {
        var url = FormattableString.Invariant($"{WttrBase}/{_lat:0.###},{_lon:0.###}?format=j1");
        var w = await Http.GetFromJsonAsync<JsonObject>(url, ct);
        var c = w?["current_condition"]?[0] ?? throw new InvalidOperationException("Thieu current_condition");
        var d = w?["weather"]?[0];
        static double? S(JsonNode? n) => n is JsonValue v && v.TryGetValue<string>(out var s) && double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x) ? x : Num(n);
        int wwo = (int)(S(c["weatherCode"]) ?? 0);
        int code = wwo switch
        {
            113 => 0, 116 => 2, 119 or 122 => 3, 143 or 248 or 260 => 45,
            176 or 263 or 266 or 281 or 284 or 293 or 296 or 353 => 51,
            299 or 302 or 356 => 61, 305 or 308 or 359 => 65,
            200 or 386 or 389 or 392 or 395 => 95,
            >= 179 and <= 377 => 71, _ => -1,
        };
        var hour = DateTime.Now.Hour;
        var (desc, icon) = Describe(code, hour >= 6 && hour < 18);
        return new WeatherInfo { Temp = S(c["temp_C"]), Feels = S(c["FeelsLikeC"]), Max = S(d?["maxtempC"]), Min = S(d?["mintempC"]), Code = code, Desc = desc, Icon = icon };
    }

    // So JSON co the la so nguyen hoac thuc
    static double? Num(JsonNode? n)
    {
        if (n is not JsonValue v) return null;
        if (v.TryGetValue<double>(out var dd)) return dd;
        if (v.TryGetValue<JsonElement>(out var e) && e.ValueKind == JsonValueKind.Number) return e.GetDouble();
        return null;
    }

    static bool TryCoords(string s, out double lat, out double lon)
    {
        lat = lon = 0;
        var p = s.Split(',', ';');
        return p.Length == 2
            && double.TryParse(p[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out lat)
            && double.TryParse(p[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out lon)
            && Math.Abs(lat) <= 90 && Math.Abs(lon) <= 180;
    }

    public static string Norm(string s)
    {
        var f = s.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var ch in f)
            if (char.IsLetterOrDigit(ch) && System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark) sb.Append(ch);
        var r = sb.ToString();
        foreach (var pre in new[] { "thanhpho", "tp" }) if (r.StartsWith(pre) && r.Length > pre.Length + 2) { r = r[pre.Length..]; break; }
        if (r.EndsWith("city")) r = r[..^4];
        return r;
    }

    // Thanh pho Viet Nam hay dung: khong can tra cuu ten (geocoding)
    static readonly Dictionary<string, (string Name, double Lat, double Lon)> KnownCities = BuildCities();
    static Dictionary<string, (string, double, double)> BuildCities()
    {
        var d = new Dictionary<string, (string, double, double)>();
        void Add((string, double, double) v, params string[] names) { foreach (var n in names) d[Norm(n)] = v; }
        Add(("TP. Hồ Chí Minh", 10.7769, 106.7009), "Ho Chi Minh City", "Hồ Chí Minh", "HCM", "TP HCM", "TPHCM", "Sài Gòn", "Saigon");
        Add(("Hà Nội", 21.0285, 105.8542), "Hà Nội", "Hanoi");
        Add(("Đà Nẵng", 16.0544, 108.2022), "Đà Nẵng", "Danang");
        Add(("Hải Phòng", 20.8449, 106.6881), "Hải Phòng", "Haiphong");
        Add(("Cần Thơ", 10.0452, 105.7469), "Cần Thơ", "Cantho");
        Add(("Huế", 16.4637, 107.5909), "Huế", "Hue");
        Add(("Nha Trang", 12.2388, 109.1967), "Nha Trang");
        Add(("Đà Lạt", 11.9404, 108.4583), "Đà Lạt", "Dalat");
        Add(("Vũng Tàu", 10.346, 107.0843), "Vũng Tàu", "Vungtau");
        Add(("Biên Hòa", 10.9574, 106.8427), "Biên Hòa", "Bien Hoa");
        Add(("Thủ Dầu Một", 10.9804, 106.6519), "Thủ Dầu Một", "Bình Dương", "Binh Duong");
        Add(("Quy Nhơn", 13.782, 109.2197), "Quy Nhơn", "Quy Nhon");
        Add(("Buôn Ma Thuột", 12.6667, 108.05), "Buôn Ma Thuột", "Buon Me Thuot");
        Add(("Vinh", 18.6796, 105.6813), "Vinh");
        Add(("Hạ Long", 20.9599, 107.0425), "Hạ Long", "Ha Long");
        Add(("Long Xuyên", 10.3864, 105.4352), "Long Xuyên");
        Add(("Rạch Giá", 10.0125, 105.0809), "Rạch Giá");
        Add(("Phan Thiết", 10.9289, 108.1021), "Phan Thiết");
        Add(("Nam Định", 20.4388, 106.1621), "Nam Định");
        Add(("Thái Nguyên", 21.5928, 105.8442), "Thái Nguyên");
        return d;
    }

    // Ma thoi tiet WMO -> mo ta tieng Viet + bieu tuong
    static (string, string) Describe(int c, bool day) => c switch
    {
        0 => ("Trời quang", day ? "☀️" : "🌙"),
        1 => ("Ít mây", day ? "🌤️" : "🌙"),
        2 => ("Có mây", "⛅"),
        3 => ("Nhiều mây", "☁️"),
        45 or 48 => ("Sương mù", "🌫️"),
        51 or 53 or 55 or 56 or 57 => ("Mưa phùn", "🌦️"),
        61 or 63 or 66 => ("Mưa", "🌧️"),
        65 or 67 => ("Mưa to", "🌧️"),
        71 or 73 or 75 or 77 or 85 or 86 => ("Tuyết", "🌨️"),
        80 or 81 => ("Mưa rào", "🌦️"),
        82 => ("Mưa rào lớn", "⛈️"),
        95 or 96 or 99 => ("Dông", "⛈️"),
        _ => ("", "🌡️"),
    };
}

public sealed class CityNotFoundException(string city) : Exception("Khong tim thay thanh pho: " + city);
public sealed class AllSourcesFailedException(string msg) : Exception(msg);
