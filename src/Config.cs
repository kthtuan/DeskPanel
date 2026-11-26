using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DeskPanel;

public sealed class PanelConfig
{
    public int Port { get; set; } = 8765;
    public int Columns { get; set; } = 4;
    public bool AutoAdbReverse { get; set; } = true;
    public string? AdbPath { get; set; }
    /// <summary>Thanh pho de lay thoi tiet; de trong = tat thoi tiet.</summary>
    public string? WeatherCity { get; set; } = "Ho Chi Minh City";
    /// <summary>Repo GitHub chua ban phat hanh, dang "chu/ten". De trong = khong tu kiem tra.</summary>
    public string? UpdateRepo { get; set; }
    public List<ButtonDef> Buttons { get; set; } = new();
    /// <summary>Cac trang tren dien thoai, moi trang la luoi 12 cot x 6 hang chua widget.</summary>
    public List<PageDef>? Pages { get; set; }
    public ThemeDef Theme { get; set; } = new();
}

public sealed class PageDef
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Icon { get; set; }
    public bool Hidden { get; set; }
    public List<WidgetDef> Widgets { get; set; } = new();
}

public sealed class WidgetDef
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; } = 3;
    public int H { get; set; } = 2;
    public string? Title { get; set; }
    /// <summary>buttons: ten nhom nut; button: ma nut; gauge: chi so</summary>
    public string? Ref { get; set; }
    public string? Color { get; set; }
}

public sealed class ThemeDef
{
    public string Accent { get; set; } = "#58a6ff";
    public string Background { get; set; } = "aurora"; // aurora | plain | black
    public string Surface { get; set; } = "cards";    // cards | flat | content
    public int Radius { get; set; } = 16;
    public int Gap { get; set; } = 10;
    public int FontScale { get; set; } = 100;
}

/// <summary>
/// Mot nut tren dien thoai. Dien thoai chi gui Id; noi dung lenh nam o day, tren PC.
/// Type:
///   run  - chay chuong trinh (Value = duong dan exe, Args = tham so)
///   url  - mo trang web / thu muc (Value = dia chi)
///   keys - gui to hop phim (Value = "Ctrl+Shift+Esc", nhieu to hop cach nhau dau phay)
///   text - go mot doan chu (Value = noi dung), co the kem Enter
///   cad  - lenh AutoCAD: dua AutoCAD len truoc, Esc Esc, go lenh, Enter
/// </summary>
public sealed class ButtonDef
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public string Group { get; set; } = "Chung";
    public string Type { get; set; } = "run";
    public string Value { get; set; } = "";
    public string? Args { get; set; }
    /// <summary>Ten tien trinh can dua len truoc khi gui phim, vd "acad". De trong = cua so dang mo.</summary>
    public string? Focus { get; set; }
    /// <summary>Chi dung cho Type=text: nhan Enter sau khi go.</summary>
    public bool Enter { get; set; }
}

public static class Json
{
    public static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}

public sealed class ConfigStore
{
    public static readonly string[] Types = { "run", "url", "keys", "text", "cad" };
    public const string CadProcesses = "acad,acadlt";

    readonly string _path;
    readonly object _lock = new();
    PanelConfig _current;
    public int Version { get; private set; } = 1;

    public ConfigStore(string path)
    {
        _path = path;
        _current = Load();
    }

    public PanelConfig Current { get { lock (_lock) return _current; } }

    PanelConfig Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var cfg = JsonSerializer.Deserialize<PanelConfig>(File.ReadAllText(_path), Json.Opts);
                var err = Validate(cfg);
                if (cfg != null && err == null)
                {
                    if (cfg.Pages == null || cfg.Pages.Count == 0) { cfg.Pages = DefaultPages(cfg); WriteFile(cfg); Log.Write("Da chuyen cau hinh sang dang trang/widget"); }
                    return cfg;
                }
                Log.Write($"config.json loi ({err}); dung cau hinh mac dinh, ban cu luu thanh config.bad.json");
                File.Copy(_path, Path.ChangeExtension(_path, ".bad.json"), true);
            }
        }
        catch (Exception ex)
        {
            Log.Write("Khong doc duoc config.json: " + ex.Message);
            try { File.Copy(_path, Path.ChangeExtension(_path, ".bad.json"), true); } catch { }
        }
        var d = Defaults.Create();
        d.Pages = DefaultPages(d);
        WriteFile(d);
        return d;
    }

    public void Save(PanelConfig cfg)
    {
        lock (_lock)
        {
            WriteFile(cfg);
            _current = cfg;
            Version++;
        }
    }

    void WriteFile(PanelConfig cfg)
    {
        var tmp = _path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(cfg, Json.Opts));
        File.Move(tmp, _path, true);
    }

    static readonly Regex IdRx = new("^[a-z0-9_-]{1,40}$");
    static readonly Regex ColorRx = new("^#[0-9a-fA-F]{6}$");
    static readonly Regex FocusRx = new(@"^[A-Za-z0-9_.\-, ]{1,100}$");

    public static readonly string[] WidgetTypes =
    {
        "clock", "weather", "media", "cpu", "gpu", "ram", "net", "gauge",
        "cpuDetail", "gpuDetail", "memory", "netDetail", "disks", "diskSummary", "buttons", "button",
    };
    public static readonly string[] Metrics = { "cpuLoad", "cpuTemp", "gpuLoad", "gpuTemp", "ramLoad", "vram" };
    public const int GridW = 12, GridH = 6;

    /// <summary>Nhan ca link: https://github.com/chu/repo(.git) -> chu/repo</summary>
    public static string? NormRepo(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        v = v.Trim();
        v = Regex.Replace(v, @"^git@github\.com:", "", RegexOptions.IgnoreCase);
        v = Regex.Replace(v, @"^(https?://)?(www\.)?github\.com/", "", RegexOptions.IgnoreCase);
        v = Regex.Replace(v, @"\.git$", "", RegexOptions.IgnoreCase).TrimEnd('/');
        var m = Regex.Match(v, @"^([A-Za-z0-9-]+)/([A-Za-z0-9._-]+)");
        return m.Success ? m.Groups[1].Value + "/" + m.Groups[2].Value : v;
    }

    static string? ValidatePages(PanelConfig c)
    {
        var t = c.Theme ??= new ThemeDef();
        if (!ColorRx.IsMatch(t.Accent ?? "")) return "Màu nhấn phải dạng #RRGGBB";
        if (t.Background is not ("aurora" or "plain" or "black")) return "Kiểu nền không hợp lệ";
        if (t.Surface is not ("cards" or "flat" or "content")) return "Kiểu thẻ không hợp lệ";
        if (t.Radius < 0 || t.Radius > 32 || t.Gap < 0 || t.Gap > 24 || t.FontScale < 60 || t.FontScale > 180) return "Thông số giao diện ngoài giới hạn";
        if (c.Pages == null) return null;
        if (c.Pages.Count > 30) return "Tối đa 30 trang";
        var pids = new HashSet<string>();
        foreach (var p in c.Pages)
        {
            if (p.Id == null || !IdRx.IsMatch(p.Id) || !pids.Add(p.Id)) return $"Mã trang '{p.Id}' không hợp lệ hoặc bị trùng";
            if (string.IsNullOrWhiteSpace(p.Name) || p.Name.Length > 30) return $"Trang '{p.Id}': tên phải có, tối đa 30 ký tự";
            if (p.Icon != null && p.Icon.Length > 8) return $"Trang '{p.Name}': biểu tượng quá dài";
            if (p.Widgets == null || p.Widgets.Count > 40) return $"Trang '{p.Name}': tối đa 40 widget";
            var used = new bool[GridW, GridH];
            var wids = new HashSet<string>();
            foreach (var w in p.Widgets)
            {
                if (w.Id == null || !IdRx.IsMatch(w.Id) || !wids.Add(w.Id)) return $"Trang '{p.Name}': mã widget '{w.Id}' không hợp lệ";
                if (Array.IndexOf(WidgetTypes, w.Type) < 0) return $"Trang '{p.Name}': loại widget '{w.Type}' không hợp lệ";
                if (w.W < 1 || w.H < 1 || w.X < 0 || w.Y < 0 || w.X + w.W > GridW || w.Y + w.H > GridH) return $"Trang '{p.Name}': widget '{w.Type}' nằm ngoài lưới";
                if (w.Title != null && w.Title.Length > 40) return $"Trang '{p.Name}': tiêu đề widget quá dài";
                if (w.Ref != null && w.Ref.Length > 40) return $"Trang '{p.Name}': tham chiếu widget quá dài";
                if (!string.IsNullOrEmpty(w.Color) && !ColorRx.IsMatch(w.Color)) return $"Trang '{p.Name}': màu widget phải dạng #RRGGBB";
                if (w.Type == "gauge" && Array.IndexOf(Metrics, w.Ref) < 0) return $"Trang '{p.Name}': vòng đo chưa chọn chỉ số";
                for (int x = w.X; x < w.X + w.W; x++)
                    for (int y = w.Y; y < w.Y + w.H; y++)
                    {
                        if (used[x, y]) return $"Trang '{p.Name}': các widget đang chồng lên nhau";
                        used[x, y] = true;
                    }
            }
        }
        return null;
    }

    /// <summary>Cau hinh cu (truoc 0.5) chua co Pages: tao bo trang mac dinh giong ban 0.4.</summary>
    public static List<PageDef> DefaultPages(PanelConfig c)
    {
        static WidgetDef W(string id, string type, int x, int y, int w, int h, string? r = null) => new() { Id = id, Type = type, X = x, Y = y, W = w, H = h, Ref = r };
        var pages = new List<PageDef>
        {
            new() { Id = "home", Name = "Tổng quan", Icon = "🏠", Widgets = { W("clock", "clock", 0, 0, 6, 4), W("media", "media", 6, 0, 6, 4),
                W("cpu", "cpu", 0, 4, 3, 2), W("gpu", "gpu", 3, 4, 3, 2), W("ram", "ram", 6, 4, 3, 2), W("net", "net", 9, 4, 3, 2) } },
            new() { Id = "system", Name = "Hệ thống", Icon = "📊", Widgets = { W("cpud", "cpuDetail", 0, 0, 6, 3), W("gpud", "gpuDetail", 6, 0, 6, 3),
                W("mem", "memory", 0, 3, 6, 3), W("netd", "netDetail", 6, 3, 6, 3) } },
            new() { Id = "disks", Name = "Ổ cứng", Icon = "💽", Widgets = { W("disks", "disks", 0, 0, 12, 6) } },
        };
        int n = 0;
        foreach (var g in c.Buttons.Select(b => b.Group).Distinct())
        {
            n++;
            pages.Add(new PageDef { Id = "btn" + n, Name = g, Icon = "🎛️", Widgets = { W("b" + n, "buttons", 0, 0, 12, 6, g) } });
        }
        return pages;
    }

    /// <summary>Tra ve null neu hop le, nguoc lai la thong bao loi tieng Viet.</summary>
    public static string? Validate(PanelConfig? c)
    {
        if (c == null) return "Cấu hình rỗng";
        if (c.Port < 1024 || c.Port > 65535) return "Cổng (port) phải từ 1024 đến 65535";
        if (c.Columns < 2 || c.Columns > 8) return "Số cột phải từ 2 đến 8";
        if (c.AdbPath != null && c.AdbPath.Length > 260) return "Đường dẫn adb quá dài";
        if (c.WeatherCity != null && c.WeatherCity.Length > 60) return "Tên thành phố quá dài";
        c.UpdateRepo = NormRepo(c.UpdateRepo);
        if (!string.IsNullOrWhiteSpace(c.UpdateRepo) && !Regex.IsMatch(c.UpdateRepo.Trim(), @"^[A-Za-z0-9-]{1,39}/[A-Za-z0-9._-]{1,100}$")) return "Repo GitHub phải có dạng chu-repo/ten-repo";
        var perr = ValidatePages(c);
        if (perr != null) return perr;
        if (c.Buttons == null) return "Thiếu danh sách nút";
        if (c.Buttons.Count > 120) return "Tối đa 120 nút";
        var ids = new HashSet<string>();
        foreach (var b in c.Buttons)
        {
            var name = string.IsNullOrWhiteSpace(b.Label) ? b.Id : b.Label;
            if (b.Id == null || !IdRx.IsMatch(b.Id)) return $"Mã nút '{b.Id}' không hợp lệ";
            if (!ids.Add(b.Id)) return $"Trùng mã nút '{b.Id}'";
            if (string.IsNullOrWhiteSpace(b.Label) || b.Label.Length > 40) return $"Nút '{b.Id}': tên phải có và dài tối đa 40 ký tự";
            if (b.Icon != null && b.Icon.Length > 8) return $"Nút '{name}': biểu tượng quá dài";
            if (!string.IsNullOrEmpty(b.Color) && !ColorRx.IsMatch(b.Color)) return $"Nút '{name}': màu phải dạng #RRGGBB";
            if (string.IsNullOrWhiteSpace(b.Group) || b.Group.Length > 30) return $"Nút '{name}': nhóm phải có và tối đa 30 ký tự";
            if (Array.IndexOf(Types, b.Type) < 0) return $"Nút '{name}': loại '{b.Type}' không hợp lệ";
            if (string.IsNullOrWhiteSpace(b.Value) || b.Value.Length > 1000) return $"Nút '{name}': thiếu nội dung lệnh";
            if (b.Args != null && b.Args.Length > 1000) return $"Nút '{name}': tham số quá dài";
            if (!string.IsNullOrEmpty(b.Focus) && !FocusRx.IsMatch(b.Focus)) return $"Nút '{name}': tên tiến trình không hợp lệ";
            if (b.Type == "keys")
            {
                var err = KeyParser.TryParseSequence(b.Value, out _);
                if (err != null) return $"Nút '{name}': {err}";
            }
        }
        return null;
    }
}
