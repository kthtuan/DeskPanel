using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace DeskPanel;

/// <summary>Kiem tra ban moi tren GitHub Releases (repo cong khai), 6 gio/lan hoac khi bam "Kiem tra".</summary>
public sealed class UpdateChecker : BackgroundService
{
    readonly ConfigStore _store;
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

    public UpdateChecker(ConfigStore store) { _store = store; }

    static HttpClient CreateHttp()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        c.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "DeskPanel-updater");
        c.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/vnd.github+json");
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
            using var resp = await Http.GetAsync($"{ApiBase}/repos/{repo}/releases/latest", ct);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) { Error = "Không thấy repo hoặc repo chưa có bản phát hành (repo phải để công khai)"; LatestVersion = null; return; }
            if ((int)resp.StatusCode == 403 || (int)resp.StatusCode == 429) { Error = "GitHub tạm giới hạn số lần hỏi, thử lại sau"; return; }
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
                if (name.EndsWith("-win-x64.zip", StringComparison.OrdinalIgnoreCase)) { _assetUrl = a?["browser_download_url"]?.GetValue<string>(); break; }
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
        if (!url.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase) && !url.StartsWith(ApiBase, StringComparison.OrdinalIgnoreCase))
            return new(false, "Đường dẫn tải không phải của GitHub");
        using var resp = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        await using var s = await resp.Content.ReadAsStreamAsync(ct);
        return await Updater.ApplyAsync(s, baseDir, ct);
    }
}
