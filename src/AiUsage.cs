using System.Net.Http.Headers;
using System.Text.Json;

namespace DeskPanel;

/// <summary>
/// Lay % gioi han chinh thuc cua goi Claude (Pro/Max) va ChatGPT (Codex), giong trang "Muc su dung" tren web.
/// Dung dang nhap san co cua Claude Code (.claude\.credentials.json) va Codex (.codex\auth.json) tren PC.
/// Chi DOC ma dang nhap, khong tu lam moi (de khong lam Claude Code/Codex bi dang xuat), khong gui ma ra dien thoai.
/// Hai dia chi nay khong co tai lieu chinh thuc; neu doi, widget quay ve so lieu tu nhat ky.
/// </summary>
public sealed partial class AiMonitor
{
    static readonly string ClaudeUsageUrl = Environment.GetEnvironmentVariable("DESKPANEL_CLAUDE_USAGE_URL") ?? "https://api.anthropic.com/api/oauth/usage";
    static readonly string CodexUsageUrl = Environment.GetEnvironmentVariable("DESKPANEL_CODEX_USAGE_URL") ?? "https://chatgpt.com/backend-api/wham/usage";
    static readonly HttpClient Http = CreateHttp();

    sealed class Online
    {
        public string? Plan;
        public List<AiLimit> Limits = new();
        public string? Credits;
        public string? Note;
        public DateTime At;
    }
    Online? _claudeOn, _codexOn;
    DateTime _nextOnline = DateTime.MinValue;

    static HttpClient CreateHttp()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        return c;
    }

    static string ClaudeHome => Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR") ?? Path.Combine(Home, ".claude");
    static string CodexHome => Environment.GetEnvironmentVariable("CODEX_HOME") ?? Path.Combine(Home, ".codex");

    async Task PollOnline(CancellationToken ct)
    {
        if (_store != null && !_store.Current.AiOnline) { _claudeOn = _codexOn = null; return; }
        if (DateTime.UtcNow < _nextOnline) return;
        _nextOnline = DateTime.UtcNow.AddMinutes(3);
        var cfg = _store?.Current;
        var claude = await Safe(() => ClaudeOnline(ct), _claudeOn);
        // Chua co dang nhap Claude Code thi dung dang nhap web trong DeskPanel
        if (cfg != null && cfg.AiWebClaude && (claude == null || claude.Limits.Count == 0))
            claude = await Safe(() => WebOnline("claude"), _claudeOn) ?? claude;
        _claudeOn = claude;
        var codex = await Safe(() => CodexOnline(ct), _codexOn);
        if (cfg != null && cfg.AiWebChatGpt && (codex == null || codex.Limits.Count == 0))
            codex = await Safe(() => WebOnline("chatgpt"), _codexOn) ?? codex;
        _codexOn = codex;
    }

    /// <summary>Hoi lai ngay (sau khi dang nhap/dang xuat).</summary>
    public void RefreshSoon() => _nextOnline = DateTime.MinValue;

    async Task<Online?> WebOnline(string site)
    {
        var json = await WebAgent.FetchAsync(site);
        if (json == null) return null;
        using var doc = JsonDocument.Parse(json);
        var r = doc.RootElement;
        int status = r.TryGetProperty("status", out var st) && st.ValueKind == JsonValueKind.Number ? st.GetInt32() : 0;
        var res = new Online { At = DateTime.UtcNow };
        if (status is 401 or 403)
        {
            res.Note = "Phiên đăng nhập " + (site == "claude" ? "Claude" : "ChatGPT") + " trong DeskPanel đã hết: vào ⚙ Cài đặt → Chung đăng nhập lại";
            return res;
        }
        var usage = Obj(r, "usage");
        if (status != 200 || usage.ValueKind != JsonValueKind.Object) throw new Exception("web " + site + " status " + status);
        if (site == "claude")
        {
            ParseClaude(usage, res);
            var caps = Obj(r, "caps");
            var capList = caps.ValueKind == JsonValueKind.Array ? caps.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : new List<string>();
            var tier = Str(r, "tier") ?? "";
            res.Plan = capList.Contains("claude_max") || tier.Contains("max") ? ClaudePlan("max", tier)
                     : capList.Contains("claude_pro") ? "Pro" : capList.Contains("raven") ? "Team" : "Free";
        }
        else
        {
            ParseCodex(usage, res);
            var plan = Str(r, "plan");
            if (!string.IsNullOrEmpty(plan)) res.Plan = Title(plan);
        }
        return res;
    }

    static async Task<Online?> Safe(Func<Task<Online?>> f, Online? old)
    {
        try { return await f(); }
        catch (Exception ex)
        {
            Log.Write("Loi lay gioi han AI: " + ex.Message);
            // Loi mang tam thoi: giu so cu toi 30 phut
            if (old != null && DateTime.UtcNow - old.At < TimeSpan.FromMinutes(30)) return old;
            return new Online { Note = "Không lấy được giới hạn (mạng?)", At = DateTime.UtcNow };
        }
    }

    // ---------- Claude ----------
    async Task<Online?> ClaudeOnline(CancellationToken ct)
    {
        var path = Path.Combine(ClaudeHome, ".credentials.json");
        if (!File.Exists(path))
            return Directory.Exists(ClaudeHome) ? new Online { Note = "Chưa đăng nhập Claude Code trên PC này: vào ⚙ Cài đặt → Chung → Đăng nhập Claude", At = DateTime.UtcNow } : null;
        using var cred = JsonDocument.Parse(await File.ReadAllTextAsync(path, ct));
        var o = Obj(cred.RootElement, "claudeAiOauth");
        var token = Str(o, "accessToken");
        if (string.IsNullOrEmpty(token)) // dang nhap bang API key: khong co goi de xem
            return new Online { Note = "Chưa có tài khoản Claude: vào ⚙ Cài đặt → Chung → Đăng nhập Claude", At = DateTime.UtcNow };
        var res = new Online { Plan = ClaudePlan(Str(o, "subscriptionType"), Str(o, "rateLimitTier")), At = DateTime.UtcNow };
        if (o.TryGetProperty("expiresAt", out var ex) && ex.ValueKind == JsonValueKind.Number &&
            DateTimeOffset.FromUnixTimeMilliseconds(ex.GetInt64()) < DateTimeOffset.UtcNow)
        {
            res.Note = "Đăng nhập Claude Code đã hết hạn, mở Claude Code một lần để làm mới";
            return res;
        }
        using var req = new HttpRequestMessage(HttpMethod.Get, ClaudeUsageUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.TryAddWithoutValidation("anthropic-beta", "oauth-2025-04-20");
        req.Headers.TryAddWithoutValidation("User-Agent", "DeskPanel");
        using var resp = await Http.SendAsync(req, ct);
        if ((int)resp.StatusCode is 401 or 403) { res.Note = "Claude từ chối, mở Claude Code một lần để làm mới đăng nhập"; return res; }
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException("Claude usage HTTP " + (int)resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        ParseClaude(doc.RootElement, res);
        return res;
    }

    static void ParseClaude(JsonElement r, Online res)
    {
        void Add(string key, string label, int window, bool onlyIfUsed)
        {
            var w = Obj(r, key);
            if (w.ValueKind != JsonValueKind.Object || !w.TryGetProperty("utilization", out var u) || u.ValueKind != JsonValueKind.Number) return;
            var used = u.GetDouble();
            long? reset = DateTimeOffset.TryParse(Str(w, "resets_at"), out var t) ? t.ToUnixTimeMilliseconds() : null;
            if (onlyIfUsed && used <= 0 && reset == null) return; // gioi han theo model chua dung: bo qua cho gon
            res.Limits.Add(new AiLimit { Label = label, Used = Math.Round(used, 1), Reset = reset, Window = window });
        }
        Add("five_hour", "Phiên 5 giờ", 300, false);
        Add("seven_day", "Tuần", 10080, false);
        // Gioi han tuan rieng theo model (seven_day_opus, seven_day_sonnet...): lay het, khong doan ten
        if (r.ValueKind == JsonValueKind.Object)
            foreach (var p in r.EnumerateObject())
            {
                if (!p.Name.StartsWith("seven_day_")) continue;
                var name = p.Name["seven_day_".Length..];
                if (name is "oauth_apps" or "cowork" && (p.Value.ValueKind != JsonValueKind.Object || !p.Value.TryGetProperty("utilization", out var mu) || mu.ValueKind != JsonValueKind.Number || mu.GetDouble() <= 0)) continue;
                Add(p.Name, "Tuần · " + Title(name.Replace('_', ' ')), 10080, true);
            }
        var extra = Obj(r, "extra_usage");
        if (extra.ValueKind == JsonValueKind.Object && extra.TryGetProperty("is_enabled", out var en) && en.ValueKind == JsonValueKind.True &&
            extra.TryGetProperty("used_credits", out var uc) && uc.ValueKind == JsonValueKind.Number)
        {
            var lim = extra.TryGetProperty("monthly_limit", out var ml) && ml.ValueKind == JsonValueKind.Number ? ml.GetDouble() : 0;
            res.Credits = "Dùng thêm: $" + (uc.GetDouble() / 100).ToString("0.00") + (lim > 0 ? " / $" + (lim / 100).ToString("0") : "");
        }
    }

    static string ClaudePlan(string? sub, string? tier)
    {
        tier ??= "";
        var x = tier.Contains("20x") ? " 20x" : tier.Contains("5x") ? " 5x" : "";
        return (sub ?? "").ToLowerInvariant() switch
        {
            "max" => "Max" + x,
            "pro" => "Pro",
            "team" => "Team",
            "enterprise" => "Enterprise",
            "" => "",
            var s => Title(s),
        };
    }

    // ---------- Codex (goi ChatGPT) ----------
    async Task<Online?> CodexOnline(CancellationToken ct)
    {
        var path = Path.Combine(CodexHome, "auth.json");
        if (!File.Exists(path)) return null;
        using var auth = JsonDocument.Parse(await File.ReadAllTextAsync(path, ct));
        var t = Obj(auth.RootElement, "tokens");
        var token = Str(t, "access_token");
        if (string.IsNullOrEmpty(token)) return null; // dang nhap bang API key
        var res = new Online { At = DateTime.UtcNow };
        using var req = new HttpRequestMessage(HttpMethod.Get, CodexUsageUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var acc = Str(t, "account_id");
        if (!string.IsNullOrEmpty(acc)) req.Headers.TryAddWithoutValidation("ChatGPT-Account-Id", acc);
        req.Headers.TryAddWithoutValidation("User-Agent", "codex-cli");
        using var resp = await Http.SendAsync(req, ct);
        if ((int)resp.StatusCode is 401 or 403) { res.Note = "ChatGPT từ chối, mở Codex một lần để làm mới đăng nhập"; return res; }
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException("Codex usage HTTP " + (int)resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        ParseCodex(doc.RootElement, res);
        return res;
    }

    static void ParseCodex(JsonElement r, Online res)
    {
        var plan = Str(r, "plan_type") ?? Str(r, "planType");
        if (!string.IsNullOrEmpty(plan)) res.Plan = Title(plan);
        var rl = Obj(r, "rate_limit");
        if (rl.ValueKind != JsonValueKind.Object) rl = Obj(r, "rateLimit");
        foreach (var name in new[] { "primary_window", "secondary_window" })
        {
            var w = Obj(rl, name);
            if (w.ValueKind != JsonValueKind.Object || !w.TryGetProperty("used_percent", out var u) || u.ValueKind != JsonValueKind.Number) continue;
            int? win = w.TryGetProperty("limit_window_seconds", out var ls) && ls.ValueKind == JsonValueKind.Number ? (int)Math.Ceiling(ls.GetDouble() / 60) : null;
            long? reset = null;
            if (w.TryGetProperty("reset_at", out var ra) && ra.ValueKind == JsonValueKind.Number)
            { var v = ra.GetInt64(); reset = v < 10_000_000_000 ? v * 1000 : v; }
            else if (w.TryGetProperty("reset_after_seconds", out var rs) && rs.ValueKind == JsonValueKind.Number)
                reset = DateTimeOffset.UtcNow.AddSeconds(rs.GetDouble()).ToUnixTimeMilliseconds();
            var label = win == null ? "Giới hạn" : win <= 360 ? "Phiên " + Math.Round(win.Value / 60.0) + " giờ" : win >= 10000 && win <= 10100 ? "Tuần" : win >= 40000 ? "Tháng" : WindowName(win);
            res.Limits.Add(new AiLimit { Label = label, Used = Math.Round(u.GetDouble(), 1), Reset = reset, Window = win });
        }
        var cr = Obj(r, "credits");
        if (cr.ValueKind == JsonValueKind.Object && cr.TryGetProperty("balance", out var b))
        {
            var unlimited = cr.TryGetProperty("unlimited", out var un) && un.ValueKind == JsonValueKind.True;
            var has = !cr.TryGetProperty("has_credits", out var hc) || hc.ValueKind != JsonValueKind.False;
            double? num = b.ValueKind == JsonValueKind.Number ? b.GetDouble() : double.TryParse(b.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var bv) ? bv : null;
            var s = num != null ? num.Value.ToString("#,0.##", System.Globalization.CultureInfo.InvariantCulture) : b.ValueKind == JsonValueKind.String ? b.GetString() : null;
            if (unlimited) res.Credits = "Credits: không giới hạn";
            else if (has && !string.IsNullOrEmpty(s) && s != "0") res.Credits = "Credits: " + s;
        }
    }

    static string Title(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    void ApplyOnline(AiTool? tool, Online? on, bool replaceLimits)
    {
        if (tool == null || on == null) return;
        tool.Plan = string.IsNullOrEmpty(on.Plan) ? null : on.Plan;
        tool.Note = on.Note;
        tool.Credits = on.Credits;
        if (on.Limits.Count > 0)
        {
            if (replaceLimits || tool.Limits.Count == 0) tool.Limits = on.Limits;
            tool.LimitsAt = new DateTimeOffset(on.At).ToUnixTimeMilliseconds();
        }
    }
}
