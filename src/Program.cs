using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using DeskPanel;

// Chi cho phep mot ban DeskPanel chay cung luc
using var mutex = new Mutex(true, @"Global\DeskPanel_SingleInstance", out bool first);
if (!first) { Log.Write("DeskPanel da dang chay, thoat ban moi."); return; }

var baseDir = AppContext.BaseDirectory;
var store = new ConfigStore(Path.Combine(baseDir, "config.json"));
int port = store.Current.Port;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = baseDir,
    WebRootPath = Path.Combine(baseDir, "wwwroot"),
});
builder.Logging.SetMinimumLevel(LogLevel.Warning);
// Chi nghe tren localhost: mang LAN khong truy cap duoc. Dien thoai toi qua "adb reverse" (cap USB).
builder.WebHost.ConfigureKestrel(o => o.ListenLocalhost(port));

var adb = new AdbBridge(store, port);
builder.Services.AddSingleton(store);
builder.Services.AddHostedService(_ => adb);
var media = new MediaWatcher();
builder.Services.AddHostedService(_ => media);
var weather = new WeatherService(store);
builder.Services.AddHostedService(_ => weather);
var updater = new UpdateChecker(store);
builder.Services.AddHostedService(_ => updater);
var hub = new StatsHub(store, media, weather);
builder.Services.AddHostedService(_ => hub);

var app = builder.Build();

// --- Bao ve: chong trang web la tren PC goi vao localhost (DNS rebinding, CSRF) ---
app.Use(async (ctx, next) =>
{
    var host = ctx.Request.Host.Host;
    if (host is not ("localhost" or "127.0.0.1" or "[::1]" or "::1"))
    {
        ctx.Response.StatusCode = 403;
        return;
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
        if (origin.Length > 0 && !IsLocalOrigin(origin)) { ctx.Response.StatusCode = 403; return; }
    }
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["Cache-Control"] = "no-cache";
    await next();
});

app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(15) });
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/config", () => Results.Json(store.Current, Json.Opts));

app.MapPut("/api/config", async (HttpRequest req) =>
{
    PanelConfig? cfg;
    try { cfg = await JsonSerializer.DeserializeAsync<PanelConfig>(req.Body, Json.Opts); }
    catch (JsonException ex) { return Results.BadRequest(new { error = "Dữ liệu không hợp lệ: " + ex.Message }); }
    if (cfg != null && (cfg.Pages == null || cfg.Pages.Count == 0)) cfg.Pages = ConfigStore.DefaultPages(cfg);
    var err = ConfigStore.Validate(cfg);
    if (err != null) return Results.BadRequest(new { error = err });
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
    notes = updater.Notes, page = updater.PageUrl, checkedAt = updater.CheckedAt?.ToString("HH:mm dd/MM"), error = updater.Error,
}, Json.Opts));
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
}, Json.Opts));

app.Map("/ws", async ctx =>
{
    if (!ctx.WebSockets.IsWebSocketRequest) { ctx.Response.StatusCode = 400; return; }
    using var ws = await ctx.WebSockets.AcceptWebSocketAsync();
    await hub.ServeAsync(ws, ctx.RequestAborted);
});

Log.Write($"DeskPanel {typeof(Program).Assembly.GetName().Version?.ToString(3)} chay tai http://localhost:{port}");
app.Run();

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

    readonly MediaWatcher _media; readonly WeatherService _weather;
    static readonly string HostName = Environment.MachineName;
    public StatsHub(ConfigStore store, MediaWatcher media, WeatherService weather) { _store = store; _media = media; _weather = weather; }

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
