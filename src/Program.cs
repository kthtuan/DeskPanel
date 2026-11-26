using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using DeskPanel;

// File cai dat: chay tu ngoai thu muc da cai thi cai/cap nhat; --uninstall thi go
if (Setup.TryRun(args)) return;

// Chi cho phep mot ban DeskPanel chay cung luc
using var mutex = new Mutex(true, @"Global\DeskPanel_SingleInstance", out bool first);
if (!first) { Log.Write("DeskPanel da dang chay, thoat ban moi."); return; }

var baseDir = AppContext.BaseDirectory;
EmbeddedWeb.Extract(baseDir);
var store = new ConfigStore(Path.Combine(baseDir, "config.json"));
int port = store.Current.Port;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = baseDir,
    WebRootPath = Path.Combine(baseDir, "wwwroot"),
});
builder.Logging.SetMinimumLevel(LogLevel.Warning);
// Mac dinh chi nghe tren localhost: dien thoai toi qua "adb reverse" (cap USB).
// Bat che do Wi-Fi thi nghe them tren mang LAN; thiet bi LAN phai co ma ghep noi.
bool lanOn = store.Current.LanEnabled;
builder.WebHost.ConfigureKestrel(o => { if (lanOn) o.ListenAnyIP(port); else o.ListenLocalhost(port); });
var lan = new LanAccess(baseDir);
string? lanError = null;
_ = Task.Run(() => { lanError = LanAccess.SetFirewall(lanOn, port); if (lanError != null) Log.Write(lanError); });

var adb = new AdbBridge(store, port);
builder.Services.AddSingleton(store);
builder.Services.AddHostedService(_ => adb);
var media = new MediaWatcher();
builder.Services.AddHostedService(_ => media);
var weather = new WeatherService(store);
builder.Services.AddHostedService(_ => weather);
var tokens = new TokenStore(baseDir);
var updater = new UpdateChecker(store, tokens);
builder.Services.AddHostedService(_ => updater);
var ai = new AiMonitor(store);
builder.Services.AddHostedService(_ => ai);
var hub = new StatsHub(store, media, weather, ai);
builder.Services.AddHostedService(_ => hub);

var app = builder.Build();

// Phai dang ky truoc lop bao ve, neu khong IsWebSocketRequest luon la false va kiem tra Origin bi bo qua
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(15) });

// --- Bao ve ---
// May nay (localhost/adb reverse): chong trang web la tren PC goi vao (DNS rebinding, CSRF).
// Thiet bi LAN: phai bat che do Wi-Fi, co ma ghep noi, va chi duoc xem/bam nut.
app.Use(async (ctx, next) =>
{
    var host = ctx.Request.Host.Host;
    bool local = LanAccess.IsLoopback(ctx.Connection.RemoteIpAddress);
    if (local)
    {
        if (host is not ("localhost" or "127.0.0.1" or "[::1]" or "::1")) { ctx.Response.StatusCode = 403; return; }
    }
    else
    {
        // Host phai la dia chi IP (QR luon dung IP), chan ten mien tro lai (DNS rebinding)
        if (!lanOn || !System.Net.IPAddress.TryParse(host.Trim('[', ']'), out _)) { ctx.Response.StatusCode = 403; return; }
        var path = ctx.Request.Path.Value ?? "/";
        bool apk = path.Equals("/deskpanel.apk", StringComparison.OrdinalIgnoreCase);
        if (!apk)
        {
            var qk = ctx.Request.Query["key"].ToString();
            if (qk.Length > 0)
            {
                if (!lan.Matches(qk)) { await Deny(ctx, "Mã ghép nối không đúng hoặc đã đổi. Hãy quét lại mã QR trong trang cài đặt DeskPanel trên PC."); return; }
                ctx.Response.Cookies.Append(LanAccess.CookieName, qk, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Strict, MaxAge = TimeSpan.FromDays(400), Path = "/" });
                // Giu ?key= tren dia chi (khong chuyen huong): iPhone "Them vao MH chinh" luu dung dia chi nay,
                // ma app tren man hinh chinh cua iOS khong dung chung cookie voi Safari.
            }
            else if (!lan.Matches(ctx.Request.Cookies[LanAccess.CookieName]) && !lan.Matches(ctx.Request.Headers["X-DeskPanel-Key"]))
            {
                await Deny(ctx, "Thiết bị này chưa được ghép nối. Hãy quét mã QR trong trang cài đặt DeskPanel trên PC (tab Chung → Wi-Fi).");
                return;
            }
            if (!LanAccess.Allowed(ctx.Request.Method, path)) { ctx.Response.StatusCode = 403; return; }
            if (path.Equals("/manifest.json", StringComparison.OrdinalIgnoreCase))
            {
                // Bieu tuong cai len man hinh chinh mo kem ma ghep noi
                var mf = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.Combine(baseDir, "wwwroot", "manifest.json")))!;
                mf["start_url"] = "./?key=" + lan.Key;
                ctx.Response.ContentType = "application/manifest+json; charset=utf-8";
                ctx.Response.Headers.CacheControl = "no-store";
                await ctx.Response.WriteAsync(mf.ToJsonString());
                return;
            }
        }
    }
    var m = ctx.Request.Method;
    if ((HttpMethods.IsPost(m) || HttpMethods.IsPut(m) || HttpMethods.IsDelete(m))
        && ctx.Request.Headers["X-DeskPanel"] != "1")
    {
        // Header tu dat buoc trinh duyet hoi truoc (CORS preflight); ta khong tra loi CORS nen trang la bi chan.
        ctx.Response.StatusCode = 403;
        return;
    }
    if (ctx.WebSockets.IsWebSocketRequest)
    {
        var origin = ctx.Request.Headers.Origin.ToString();
        if (origin.Length > 0 && !(local ? IsLocalOrigin(origin) : SameHostOrigin(origin, ctx.Request.Host))) { ctx.Response.StatusCode = 403; return; }
    }
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["Cache-Control"] = "no-cache";
    await next();
});

app.UseDefaultFiles();
var ctp = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
ctp.Mappings[".apk"] = "application/vnd.android.package-archive";
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = ctp });

app.MapGet("/api/config", () => Results.Json(store.Current, Json.Opts));

app.MapPut("/api/config", async (HttpRequest req) =>
{
    PanelConfig? cfg;
    try { cfg = await JsonSerializer.DeserializeAsync<PanelConfig>(req.Body, Json.Opts); }
    catch (JsonException ex) { return Results.BadRequest(new { error = "Dữ liệu không hợp lệ: " + ex.Message }); }
    if (cfg != null && (cfg.Pages == null || cfg.Pages.Count == 0)) cfg.Pages = ConfigStore.DefaultPages(cfg);
    var err = ConfigStore.Validate(cfg);
    if (err != null) return Results.BadRequest(new { error = err });
    cfg!.LanEnabled = store.Current.LanEnabled; // chi doi qua /api/lan
    cfg.AiWebClaude = store.Current.AiWebClaude; cfg.AiWebChatGpt = store.Current.AiWebChatGpt; // chi doi qua /api/ai/login
    store.Save(cfg!);
    Log.Write($"Da luu cau hinh: {cfg!.Buttons.Count} nut");
    return Results.Ok(new { ok = true, restartNeeded = cfg.Port != port });
});

app.MapPost("/api/action/{id}", async (string id) =>
{
    var b = store.Current.Buttons.FirstOrDefault(x => x.Id == id);
    if (b == null) return Results.NotFound(new { error = "Không có nút này" });
    var r = await ActionRunner.RunAsync(b);
    return r.Ok ? Results.Ok(new { ok = true }) : Results.BadRequest(new { error = r.Error });
});

app.MapPost("/api/media/{cmd}", async (string cmd) =>
{
    ushort vk = cmd switch { "volup" => 0xAF, "voldown" => 0xAE, "mute" => 0xAD, _ => 0 };
    if (vk != 0) { if (OperatingSystem.IsWindows()) Win.SendCombo(new[] { vk }); return Results.Ok(new { ok = true }); }
    if (cmd is not ("toggle" or "next" or "prev")) return Results.BadRequest(new { error = "Lệnh không hợp lệ" });
    try { return await media.Control(cmd) ? Results.Ok(new { ok = true }) : Results.BadRequest(new { error = "Không có gì đang phát" }); }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});
app.MapGet("/api/media/art", (HttpContext ctx) =>
{
    if (media.Art == null) return Results.NotFound();
    ctx.Response.Headers.CacheControl = "max-age=86400";
    return Results.Bytes(media.Art, media.ArtType ?? "image/png");
});
// --- Cap nhat ---
app.MapGet("/api/update", () => Results.Json(new
{
    current = updater.Current, repo = store.Current.UpdateRepo, latest = updater.LatestVersion, available = updater.Available,
    hasToken = tokens.HasToken, notes = updater.Notes, page = updater.PageUrl, checkedAt = updater.CheckedAt?.ToString("HH:mm dd/MM"), error = updater.Error,
}, Json.Opts));
app.MapPost("/api/update/token", async (HttpRequest req, CancellationToken ct) =>
{
    var j = await JsonSerializer.DeserializeAsync<Dictionary<string, string?>>(req.Body, Json.Opts, ct);
    var t = j != null && j.TryGetValue("token", out var v) ? v?.Trim() : null;
    if (!string.IsNullOrEmpty(t) && !TokenStore.LooksValid(t)) return Results.BadRequest(new { error = "Token không đúng dạng (ví dụ github_pat_…)" });
    tokens.Set(t);
    Log.Write(string.IsNullOrEmpty(t) ? "Da xoa token GitHub" : "Da luu token GitHub");
    await updater.CheckAsync(ct);
    return Results.Ok(new { ok = true, hasToken = tokens.HasToken });
});
app.MapPost("/api/update/check", async (CancellationToken ct) => { await updater.CheckAsync(ct); return Results.Json(new { ok = true }); });
app.MapPost("/api/update/upload", async (HttpContext ctx) =>
{
    var f = ctx.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
    if (f != null && !f.IsReadOnly) f.MaxRequestBodySize = 210L * 1024 * 1024;
    var r = await Updater.ApplyAsync(ctx.Request.Body, baseDir, ctx.RequestAborted);
    return Results.Json(new { ok = r.Ok, message = r.Message, version = r.NewVersion }, Json.Opts, statusCode: r.Ok ? 200 : 400);
});
app.MapPost("/api/update/github", async (CancellationToken ct) =>
{
    var r = await updater.InstallLatestAsync(baseDir, ct);
    return Results.Json(new { ok = r.Ok, message = r.Message, version = r.NewVersion }, Json.Opts, statusCode: r.Ok ? 200 : 400);
});
// --- Phan mem da cai va icon ---
app.MapGet("/api/apps", () => Results.Json(AppCatalog.List(), Json.Opts));
app.MapGet("/api/icon", (string path, HttpContext ctx) => IconResult(AppCatalog.IconPng(path), ctx)); // chi tren PC (trang cai dat)
app.MapGet("/api/icon/{id}", (string id, HttpContext ctx) =>
{
    var b = store.Current.Buttons.FirstOrDefault(x => x.Id == id);
    return b == null || b.Type is not ("run" or "url") ? Results.NotFound() : IconResult(AppCatalog.IconPng(b.Value), ctx);
});

// --- Wi-Fi (chi goi duoc tu chinh PC) ---
app.MapGet("/api/lan", () =>
{
    var ips = LanAccess.Addresses();
    var ip = ips.FirstOrDefault();
    var url = ip == null ? null : $"http://{ip}:{port}/?key={lan.Key}";
    return Results.Json(new
    {
        enabled = store.Current.LanEnabled, active = lanOn, port, addresses = ips, url,
        qr = url == null ? null : LanAccess.QrSvg(url),
        apkUrl = ip == null ? null : $"http://{ip}:{port}/deskpanel.apk",
        apkQr = ip == null ? null : LanAccess.QrSvg($"http://{ip}:{port}/deskpanel.apk"),
        hasApk = File.Exists(Path.Combine(baseDir, "wwwroot", "deskpanel.apk")),
        error = lanError,
    }, Json.Opts);
});
app.MapPost("/api/lan", async (HttpRequest req, CancellationToken ct) =>
{
    var j = await JsonSerializer.DeserializeAsync<Dictionary<string, bool>>(req.Body, Json.Opts, ct);
    bool on = j != null && j.TryGetValue("enabled", out var v) && v;
    var cfg = JsonSerializer.Deserialize<PanelConfig>(JsonSerializer.Serialize(store.Current, Json.Opts), Json.Opts)!;
    cfg.LanEnabled = on;
    store.Save(cfg);
    Log.Write(on ? "Bat che do Wi-Fi" : "Tat che do Wi-Fi");
    string? rerr = null;
    if (on != lanOn) rerr = Updater.ScheduleRestart(baseDir);
    return Results.Json(new { ok = true, restarting = on != lanOn && rerr == null, error = rerr }, Json.Opts);
});
// Dang nhap Claude / ChatGPT tren web trong DeskPanel (mo cua so tren PC)
app.MapGet("/api/ai/web", () => Results.Json(new
{
    available = WebAgent.Available, busy = WebAgent.Busy,
    extensionAt = ai.PushAt == null ? (long?)null : new DateTimeOffset(ai.PushAt.Value).ToUnixTimeMilliseconds(),
    hasExtension = File.Exists(Path.Combine(baseDir, "wwwroot", "deskpanel-extension.zip")),
    claude = store.Current.AiWebClaude, chatgpt = store.Current.AiWebChatGpt,
}, Json.Opts));
app.MapPost("/api/ai/login/{site}", async (string site) =>
{
    if (site is not ("claude" or "chatgpt")) return Results.NotFound();
    if (!WebAgent.Available) return Results.Json(new { ok = false, error = "Máy chưa có Microsoft Edge WebView2. Tải tại https://go.microsoft.com/fwlink/p/?LinkId=2124703 rồi thử lại." }, Json.Opts, statusCode: 400);
    bool ok = await WebAgent.LoginAsync(site);
    if (ok) SetAiWeb(site, true);
    ai.RefreshSoon();
    return Results.Json(new { ok }, Json.Opts);
});
// Tien ich Chrome/Edge gui so lieu tu phien claude.ai / chatgpt.com cua trinh duyet
app.MapPost("/api/ai/push", async (HttpRequest req, CancellationToken ct) =>
{
    using var doc = await JsonDocument.ParseAsync(req.Body, cancellationToken: ct);
    foreach (var site in new[] { "claude", "chatgpt" })
        if (doc.RootElement.TryGetProperty(site, out var v) && v.ValueKind == JsonValueKind.Object) ai.Push(site, v.GetRawText());
    ai.RefreshSoon();
    return Results.Json(new { ok = true }, Json.Opts);
});
app.MapPost("/api/ai/logout/{site}", async (string site) =>
{
    if (site is not ("claude" or "chatgpt")) return Results.NotFound();
    await WebAgent.LogoutAsync(site);
    SetAiWeb(site, false);
    ai.RefreshSoon();
    return Results.Json(new { ok = true }, Json.Opts);
});
void SetAiWeb(string site, bool on)
{
    var cfg = JsonSerializer.Deserialize<PanelConfig>(JsonSerializer.Serialize(store.Current, Json.Opts), Json.Opts)!;
    if (site == "claude") cfg.AiWebClaude = on; else cfg.AiWebChatGpt = on;
    store.Save(cfg);
    Log.Write((on ? "Dang nhap web " : "Dang xuat web ") + site);
}
app.MapPost("/api/lan/rotate", () => { lan.Rotate(); Log.Write("Da doi ma ghep noi Wi-Fi"); return Results.Json(new { ok = true }); });
app.MapPost("/api/restart", () =>
{
    var e = Updater.ScheduleRestart(baseDir);
    return Results.Json(new { ok = e == null, error = e }, Json.Opts, statusCode: e == null ? 200 : 400);
});
app.MapGet("/api/stats", () => Results.Json(hub.Latest, Json.Opts));
app.MapGet("/api/sensors", async () => Results.Json(await hub.DumpAsync(), Json.Opts));
app.MapGet("/api/status", () => Results.Json(new
{
    version = typeof(Program).Assembly.GetName().Version?.ToString(3),
    port,
    adb = adb.Status,
    note = hub.Latest?.Note,
    weather = weather.Status,
    windows = OperatingSystem.IsWindows(),
    lan = lanOn,
}, Json.Opts));

app.Map("/ws", async ctx =>
{
    if (!ctx.WebSockets.IsWebSocketRequest) { ctx.Response.StatusCode = 400; return; }
    using var ws = await ctx.WebSockets.AcceptWebSocketAsync();
    await hub.ServeAsync(ws, ctx.RequestAborted);
});

Log.Write($"DeskPanel {typeof(Program).Assembly.GetName().Version?.ToString(3)} chay tai http://localhost:{port}");
app.Run();

static IResult IconResult(byte[]? png, HttpContext ctx)
{
    if (png == null) return Results.NotFound();
    ctx.Response.Headers.CacheControl = "max-age=86400";
    return Results.Bytes(png, "image/png");
}

static async Task Deny(HttpContext ctx, string msg)
{
    ctx.Response.StatusCode = 401;
    ctx.Response.ContentType = "text/html; charset=utf-8";
    await ctx.Response.WriteAsync("<!doctype html><meta charset=utf-8><meta name=viewport content='width=device-width'><body style='font:16px system-ui;background:#0d1117;color:#e6edf3;padding:24px'><h2>DeskPanel</h2><p>" + System.Net.WebUtility.HtmlEncode(msg) + "</p>");
}

static bool SameHostOrigin(string origin, HostString host) =>
    Uri.TryCreate(origin, UriKind.Absolute, out var u) && string.Equals(u.Authority, host.Value, StringComparison.OrdinalIgnoreCase);

static bool IsLocalOrigin(string origin) =>
    Uri.TryCreate(origin, UriKind.Absolute, out var u) && u.Host is "localhost" or "127.0.0.1" or "[::1]" or "::1";

/// <summary>Doc cam bien moi giay tren MOT luong rieng va day toi moi dien thoai dang ket noi.</summary>
sealed class StatsHub : BackgroundService
{
    readonly ConfigStore _store;
    readonly List<WebSocket> _clients = new();
    readonly object _lock = new();
    TaskCompletionSource<object>? _dumpReq;
    public Snapshot? Latest { get; private set; }
    // Doi moi lan build: dien thoai thay khac thi tu tai lai trang
    static readonly string AppVer = (typeof(StatsHub).Assembly.GetName().Version?.ToString(3) ?? "0") + "-" + File.GetLastWriteTimeUtc(Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html")).Ticks;

    readonly MediaWatcher _media; readonly WeatherService _weather; readonly AiMonitor _ai;
    static readonly string HostName = Environment.MachineName;
    public StatsHub(ConfigStore store, MediaWatcher media, WeatherService weather, AiMonitor ai) { _store = store; _media = media; _weather = weather; _ai = ai; }

    public Task<object> DumpAsync()
    {
        var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        Interlocked.Exchange(ref _dumpReq, tcs);
        return tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    protected override Task ExecuteAsync(CancellationToken ct) => Task.Factory.StartNew(() => Loop(ct), TaskCreationOptions.LongRunning);

    void Loop(CancellationToken ct)
    {
        using var src = SensorFactory.Create();
        while (!ct.IsCancellationRequested)
        {
            var t0 = DateTime.UtcNow;
            try
            {
                var snap = src.Read();
                snap.Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                snap.CfgVer = _store.Version;
                snap.AppVer = AppVer;
                snap.Media = _media.Current;
                snap.Weather = _weather.Latest;
                snap.Ai = _ai.Latest;
                snap.Host = HostName;
                Latest = snap;
                Broadcast(JsonSerializer.SerializeToUtf8Bytes(snap, Json.Opts));
                var req = Interlocked.Exchange(ref _dumpReq, null);
                req?.TrySetResult(src.DumpAll());
            }
            catch (Exception ex) { Log.Write("Loi doc cam bien: " + ex.Message); }
            var wait = 1000 - (int)(DateTime.UtcNow - t0).TotalMilliseconds;
            if (wait > 0) ct.WaitHandle.WaitOne(wait);
        }
    }

    void Broadcast(byte[] data)
    {
        WebSocket[] list;
        lock (_lock) list = _clients.ToArray();
        foreach (var ws in list)
        {
            if (ws.State != WebSocketState.Open) continue;
            _ = SendSafe(ws, data);
        }
    }

    static async Task SendSafe(WebSocket ws, byte[] data)
    {
        try
        {
            using var cts = new CancellationTokenSource(3000);
            await ws.SendAsync(data, WebSocketMessageType.Text, true, cts.Token);
        }
        catch { }
    }

    public async Task ServeAsync(WebSocket ws, CancellationToken ct)
    {
        lock (_lock) _clients.Add(ws);
        try
        {
            if (Latest != null) await SendSafe(ws, JsonSerializer.SerializeToUtf8Bytes(Latest, Json.Opts));
            var buf = new byte[1024];
            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var r = await ws.ReceiveAsync(buf, ct);
                if (r.MessageType == WebSocketMessageType.Close) break;
            }
        }
        catch { }
        finally
        {
            lock (_lock) _clients.Remove(ws);
        }
    }
}
