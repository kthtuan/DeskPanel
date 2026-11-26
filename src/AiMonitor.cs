using System.Text.Json;

namespace DeskPanel;

/// <summary>
/// Theo doi cong cu AI chay tren PC bang cach doc nhat ky chung ghi ra may:
///  - Claude Code: %USERPROFILE%\.claude\projects\**\*.jsonl (moi tin nhan co "usage").
///  - Codex CLI:   %USERPROFILE%\.codex\sessions\**\*.jsonl (su kien token_count, kem rate_limits).
/// Chi doc, khong gui di dau. Doc tang dan theo vi tri file nen nhe.
/// </summary>
public sealed class AiMonitor : BackgroundService
{
    public AiInfo? Latest { get; private set; }

    sealed class FileState { public long Pos; public string? Cwd; }
    readonly Dictionary<string, FileState> _files = new();
    // Claude: moi tin nhan (dedupe theo message id + request id)
    readonly Dictionary<string, (DateTime t, long tok)> _claude = new();
    // Codex: token theo thoi diem, va rate limit moi nhat
    readonly List<(DateTime t, long tok)> _codex = new();
    CodexLimit? _codexPrimary, _codexSecondary; DateTime _codexLimitAt;

    sealed record CodexLimit(double Used, int? WindowMin, DateTime? ResetAt);

    static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { Latest = Scan(); }
            catch (Exception ex) { Log.Write("Loi doc nhat ky AI: " + ex.Message); }
            try { await Task.Delay(TimeSpan.FromSeconds(20), ct); } catch { break; }
        }
    }

    AiInfo? Scan()
    {
        var now = DateTime.UtcNow;
        var claudeDir = Path.Combine(Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR") ?? Path.Combine(Home, ".claude"), "projects");
        var codexDir = Path.Combine(Environment.GetEnvironmentVariable("CODEX_HOME") ?? Path.Combine(Home, ".codex"), "sessions");
        var claude = Directory.Exists(claudeDir) ? ScanClaude(claudeDir, now) : null;
        var codex = Directory.Exists(codexDir) ? ScanCodex(codexDir, now) : null;
        return claude == null && codex == null ? null : new AiInfo { Claude = claude, Codex = codex };
    }

    static IEnumerable<FileInfo> Recent(string dir, DateTime since)
    {
        try
        {
            return new DirectoryInfo(dir).EnumerateFiles("*.jsonl", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true })
                .Where(f => f.LastWriteTimeUtc >= since).ToList();
        }
        catch { return Array.Empty<FileInfo>(); }
    }

    // Doc phan moi cua file tu lan truoc
    IEnumerable<string> NewLines(FileInfo f, FileState st)
    {
        if (f.Length < st.Pos) st.Pos = 0; // file bi ghi lai
        if (f.Length == st.Pos) yield break;
        using var fs = new FileStream(f.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        fs.Seek(st.Pos, SeekOrigin.Begin);
        using var sr = new StreamReader(fs);
        string? line; long pos = st.Pos;
        var all = sr.ReadToEnd();
        int start = 0;
        while (true)
        {
            int nl = all.IndexOf('\n', start);
            if (nl < 0) break; // dong chua ghi xong: de lan sau
            line = all[start..nl];
            pos += System.Text.Encoding.UTF8.GetByteCount(all.AsSpan(start, nl - start + 1));
            start = nl + 1;
            if (line.Length > 2) yield return line;
        }
        st.Pos = pos;
    }

    FileState State(string path) { if (!_files.TryGetValue(path, out var s)) _files[path] = s = new FileState(); return s; }

    static string? Str(JsonElement e, string name) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    static long Num(JsonElement e, string name) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : 0;
    static JsonElement Obj(JsonElement e, string name) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) ? v : default;
    static DateTime? Time(JsonElement e) => DateTime.TryParse(Str(e, "timestamp"), null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var t) ? t : null;

    AiTool ScanClaude(string dir, DateTime now)
    {
        string? activeProject = null; DateTime lastActive = DateTime.MinValue;
        foreach (var f in Recent(dir, now.AddDays(-8)))
        {
            var st = State(f.FullName);
            foreach (var line in NewLines(f, st))
            {
                if (!line.Contains("\"usage\"")) { if (st.Cwd == null && line.Contains("\"cwd\"")) TryCwd(line, st); continue; }
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var r = doc.RootElement;
                    st.Cwd ??= Str(r, "cwd");
                    var msg = Obj(r, "message"); var u = Obj(msg, "usage");
                    var t = Time(r);
                    if (u.ValueKind != JsonValueKind.Object || t == null) continue;
                    var key = (Str(msg, "id") ?? Str(r, "uuid") ?? line.GetHashCode().ToString()) + ":" + Str(r, "requestId");
                    long tok = Num(u, "input_tokens") + Num(u, "output_tokens") + Num(u, "cache_creation_input_tokens");
                    _claude[key] = (t.Value, tok);
                }
                catch (JsonException) { }
            }
            if (now - f.LastWriteTimeUtc < TimeSpan.FromMinutes(2) && f.LastWriteTimeUtc > lastActive)
            { lastActive = f.LastWriteTimeUtc; activeProject = st.Cwd; }
        }
        // Bo du lieu qua 8 ngay
        foreach (var k in _claude.Where(kv => now - kv.Value.t > TimeSpan.FromDays(8)).Select(kv => kv.Key).ToList()) _claude.Remove(k);

        var today = DateTime.Now.Date.ToUniversalTime();
        var msgs = _claude.Values.OrderBy(x => x.t).ToList();
        var tool = new AiTool
        {
            Today = msgs.Where(x => x.t >= today).Sum(x => x.tok),
            Week = msgs.Where(x => now - x.t < TimeSpan.FromDays(7)).Sum(x => x.tok),
            MessagesToday = msgs.Count(x => x.t >= today),
            Active = activeProject != null || lastActive > DateTime.MinValue,
            Project = activeProject == null ? null : Leaf(activeProject),
        };
        // Khung 5 gio kieu Claude: bat dau tu gio chan cua tin nhan dau tien sau khi khung cu het
        DateTime? blockStart = null; DateTime last = DateTime.MinValue;
        foreach (var m in msgs.Where(x => now - x.t < TimeSpan.FromHours(24)))
        {
            if (blockStart == null || m.t - blockStart.Value >= TimeSpan.FromHours(5) || m.t - last >= TimeSpan.FromHours(5))
                blockStart = new DateTime(m.t.Year, m.t.Month, m.t.Day, m.t.Hour, 0, 0, DateTimeKind.Utc);
            last = m.t;
        }
        if (blockStart != null && now < blockStart.Value.AddHours(5))
        {
            tool.Block = msgs.Where(x => x.t >= blockStart.Value).Sum(x => x.tok);
            tool.BlockReset = new DateTimeOffset(blockStart.Value.AddHours(5)).ToUnixTimeMilliseconds();
        }
        return tool;
    }

    static void TryCwd(string line, FileState st)
    {
        try { using var d = JsonDocument.Parse(line); st.Cwd = Str(d.RootElement, "cwd"); } catch { }
    }

    AiTool ScanCodex(string dir, DateTime now)
    {
        string? activeProject = null; DateTime lastActive = DateTime.MinValue;
        foreach (var f in Recent(dir, now.AddDays(-8)))
        {
            var st = State(f.FullName);
            foreach (var line in NewLines(f, st))
            {
                if (!line.Contains("token_count") && !(st.Cwd == null && line.Contains("\"cwd\""))) continue;
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var r = doc.RootElement; var p = Obj(r, "payload");
                    st.Cwd ??= Str(p, "cwd") ?? Str(r, "cwd");
                    if (Str(p, "type") != "token_count") continue;
                    var t = Time(r) ?? now;
                    var last = Obj(Obj(p, "info"), "last_token_usage");
                    long tok = Num(last, "total_tokens");
                    if (tok == 0) tok = Num(last, "input_tokens") + Num(last, "output_tokens");
                    if (tok > 0) _codex.Add((t, tok));
                    var rl = Obj(p, "rate_limits");
                    if (rl.ValueKind == JsonValueKind.Object && t >= _codexLimitAt)
                    {
                        _codexLimitAt = t;
                        _codexPrimary = Limit(Obj(rl, "primary"), t) ?? _codexPrimary;
                        _codexSecondary = Limit(Obj(rl, "secondary"), t) ?? _codexSecondary;
                    }
                }
                catch (JsonException) { }
            }
            if (now - f.LastWriteTimeUtc < TimeSpan.FromMinutes(2) && f.LastWriteTimeUtc > lastActive)
            { lastActive = f.LastWriteTimeUtc; activeProject = st.Cwd ?? ""; }
        }
        _codex.RemoveAll(x => now - x.t > TimeSpan.FromDays(8));
        var today = DateTime.Now.Date.ToUniversalTime();
        var tool = new AiTool
        {
            Today = _codex.Where(x => x.t >= today).Sum(x => x.tok),
            Week = _codex.Where(x => now - x.t < TimeSpan.FromDays(7)).Sum(x => x.tok),
            Active = lastActive > DateTime.MinValue,
            Project = string.IsNullOrEmpty(activeProject) ? null : Leaf(activeProject),
        };
        foreach (var l in new[] { _codexPrimary, _codexSecondary })
        {
            if (l == null || (l.ResetAt != null && l.ResetAt < now)) continue; // khung da qua: so cu khong con dung
            var lim = new AiLimit { Used = Math.Round(l.Used, 1), Label = WindowName(l.WindowMin), Reset = l.ResetAt == null ? null : new DateTimeOffset(l.ResetAt.Value).ToUnixTimeMilliseconds() };
            tool.Limits.Add(lim);
        }
        return tool;
    }

    static string Leaf(string p) => p.TrimEnd('\\', '/').Split('\\', '/').Last();

    static string WindowName(int? min) => min switch
    {
        null => "Giới hạn",
        <= 360 => (min / 60) + " giờ",
        >= 10000 => "Tuần",
        _ => (min / 60) + " giờ",
    };

    static CodexLimit? Limit(JsonElement e, DateTime at)
    {
        if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty("used_percent", out var u) || u.ValueKind != JsonValueKind.Number) return null;
        int? win = e.TryGetProperty("window_minutes", out var w) && w.ValueKind == JsonValueKind.Number ? w.GetInt32() : null;
        DateTime? reset = null;
        if (e.TryGetProperty("resets_at", out var ra) && ra.ValueKind == JsonValueKind.Number) reset = DateTimeOffset.FromUnixTimeSeconds(ra.GetInt64()).UtcDateTime;
        else if (e.TryGetProperty("resets_in_seconds", out var rs) && rs.ValueKind == JsonValueKind.Number) reset = at.AddSeconds(rs.GetDouble());
        return new CodexLimit(u.GetDouble(), win, reset);
    }
}

public sealed class AiInfo
{
    public AiTool? Claude { get; set; }
    public AiTool? Codex { get; set; }
}

public sealed class AiTool
{
    public long Today { get; set; }
    public long Week { get; set; }
    public int? MessagesToday { get; set; }
    public long? Block { get; set; }
    public long? BlockReset { get; set; }
    public bool Active { get; set; }
    public string? Project { get; set; }
    public List<AiLimit> Limits { get; set; } = new();
}

public sealed class AiLimit
{
    public string Label { get; set; } = "";
    public double Used { get; set; }
    public long? Reset { get; set; }
}
