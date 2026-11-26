using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace DeskPanel;

/// <summary>Kiem tra ban moi tren GitHub Releases (repo cong khai), 6 gio/lan hoac khi bam "Kiem tra".</summary>
public sealed class UpdateChecker : BackgroundService
{
    readonly ConfigStore _store;
    readonly TokenStore _tokens;
    static readonly HttpClient Http = CreateHttp();
    public string Current { get; } = typeof(UpdateChecker).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    public string? LatestVersion { get; private set; }
    public string? Notes { get; private set; }
    public string? PageUrl { get; private set; }
    public DateTime? CheckedAt { get; private set; }
    public string? Error { get; private set; }
    string? _assetUrl;
    readonly SemaphoreSlim _lock = new(1, 1);
    static readonly string ApiBase = Environment.GetEnvironmentVariable("DESKPANEL_GH_BASE") ?? "https://api.github.com";

    public UpdateChecker(ConfigStore store, TokenStore tokens) { _store = store; _tokens = tokens; }

    HttpRequestMessage Req(string url, string accept = "application/vnd.github+json")
    {
        var m = new HttpRequestMessage(HttpMethod.Get, url);
        m.Headers.TryAddWithoutValidation("Accept", accept);
        if (_tokens.HasToken) m.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _tokens.Token);
        return m;
    }

    static HttpClient CreateHttp()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        c.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "DeskPanel-updater");
        return c;
    }

    public bool Available => LatestVersion != null && _assetUrl != null && Newer(LatestVersion, Current);

    static bool Newer(string a, string b) => Version.TryParse(a, out var va) && Version.TryParse(b, out var vb) && va > vb;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(20), ct).ContinueWith(_ => { });
        while (!ct.IsCancellationRequested)
        {
            if (!string.IsNullOrWhiteSpace(_store.Current.UpdateRepo)) await CheckAsync(ct);
            await Task.Delay(TimeSpan.FromHours(6), ct).ContinueWith(_ => { });
        }
    }

    public async Task CheckAsync(CancellationToken ct)
    {
        var repo = _store.Current.UpdateRepo?.Trim();
        await _lock.WaitAsync(ct);
        try
        {
            CheckedAt = DateTime.Now; Error = null;
            if (string.IsNullOrEmpty(repo)) { Error = "Chưa điền repo GitHub"; return; }
            using var resp = await Http.SendAsync(Req($"{ApiBase}/repos/{repo}/releases/latest"), ct);
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized) { Error = "Token GitHub sai hoặc đã hết hạn. Tạo token mới rồi dán lại"; return; }
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Error = _tokens.HasToken
                    ? "Không thấy repo, repo chưa có bản phát hành, hoặc token chưa được cấp quyền đọc repo này"
                    : "Không thấy repo hoặc chưa có bản phát hành. Nếu repo để private, hãy dán token GitHub";
                LatestVersion = null; return;
            }
            if ((int)resp.StatusCode == 403 || (int)resp.StatusCode == 429) { Error = "GitHub tạm giới hạn số lần hỏi hoặc token thiếu quyền, thử lại sau"; return; }
            resp.EnsureSuccessStatusCode();
            var j = await resp.Content.ReadFromJsonAsync<JsonObject>(ct);
            var tag = j?["tag_name"]?.GetValue<string>()?.Trim().TrimStart('v', 'V');
            LatestVersion = tag;
            PageUrl = j?["html_url"]?.GetValue<string>();
            var body = j?["body"]?.GetValue<string>() ?? "";
            Notes = body.Length > 800 ? body[..800] + "…" : body;
            _assetUrl = null;
            foreach (var a in j?["assets"] as JsonArray ?? new JsonArray())
            {
                var name = a?["name"]?.GetValue<string>() ?? "";
                if (name.EndsWith("-win-x64.zip", StringComparison.OrdinalIgnoreCase)) { _assetUrl = a?["url"]?.GetValue<string>(); break; }
            }
            if (tag != null && _assetUrl == null) Error = $"Bản {tag} chưa có file …-win-x64.zip";
            if (Available) Log.Write($"Co ban moi {tag} tren GitHub");
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            Error = "Không kiểm tra được: " + ex.Message;
            Log.Write("Kiem tra cap nhat loi: " + ex.Message);
        }
        finally { _lock.Release(); }
    }

    /// <summary>Tai file zip cua ban moi nhat roi giao cho Updater.</summary>
    public async Task<Updater.Result> InstallLatestAsync(string baseDir, CancellationToken ct)
    {
        await CheckAsync(ct);
        if (!Available) return new(false, Error ?? "Không có bản mới hơn bản đang chạy");
        var url = _assetUrl!;
        // Duong dan API cua file (dung duoc ca repo private khi co token); GitHub chuyen huong sang may chu luu file,
        // HttpClient tu bo header Authorization khi chuyen sang ten mien khac.
        if (!url.StartsWith(ApiBase + "/", StringComparison.OrdinalIgnoreCase))
            return new(false, "Đường dẫn tải không phải của GitHub");
        using var resp = await Http.SendAsync(Req(url, "application/octet-stream"), HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        await using var s = await resp.Content.ReadAsStreamAsync(ct);
        return await Updater.ApplyAsync(s, baseDir, ct);
    }
}
