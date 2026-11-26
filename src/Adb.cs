using System.Diagnostics;

namespace DeskPanel;

/// <summary>
/// Moi 5 giay: tim dien thoai cam USB va chay "adb reverse tcp:PORT tcp:PORT"
/// de dien thoai mo http://localhost:PORT la toi duoc PC. Khong mo cong nao ra mang LAN.
/// </summary>
public sealed class AdbBridge : BackgroundService
{
    readonly ConfigStore _store;
    readonly int _port;
    public string Status { get; private set; } = "Đang khởi động";

    public AdbBridge(ConfigStore store, int port) { _store = store; _port = port; }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { Tick(); }
            catch (Exception ex) { Status = "Lỗi adb: " + ex.Message; }
            await Task.Delay(TimeSpan.FromSeconds(5), ct).ContinueWith(_ => { });
        }
    }

    string? FindAdb()
    {
        var cfg = _store.Current.AdbPath;
        var exe = OperatingSystem.IsWindows() ? "adb.exe" : "adb";
        var candidates = new List<string?>
        {
            cfg,
            Path.Combine(AppContext.BaseDirectory, "platform-tools", exe),
            Path.Combine(AppContext.BaseDirectory, exe),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk", "platform-tools", exe),
        };
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
            if (!string.IsNullOrWhiteSpace(dir)) candidates.Add(Path.Combine(dir.Trim(), exe));
        return candidates.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p));
    }

    void Tick()
    {
        if (!_store.Current.AutoAdbReverse) { Status = "Đã tắt tự động adb reverse"; return; }
        var adb = FindAdb();
        if (adb == null) { Status = "Không tìm thấy adb.exe (đặt thư mục platform-tools cạnh DeskPanel.exe)"; return; }

        var (_, devOut) = Run(adb, "devices");
        var serials = devOut.Split('\n')
            .Select(l => l.Trim().Split('\t'))
            .Where(p => p.Length == 2)
            .ToList();
        var ready = serials.Where(p => p[1] == "device").Select(p => p[0]).ToList();
        if (ready.Count == 0)
        {
            Status = serials.Any(p => p[1] == "unauthorized")
                ? "Điện thoại chưa cho phép gỡ lỗi USB: mở khóa điện thoại và bấm Cho phép"
                : "Chưa thấy điện thoại (cắm cáp USB, bật Gỡ lỗi USB)";
            return;
        }
        foreach (var s in ready)
        {
            var (_, list) = Run(adb, $"-s {s} reverse --list");
            if (!list.Contains($"tcp:{_port} tcp:{_port}"))
            {
                var (code, outp) = Run(adb, $"-s {s} reverse tcp:{_port} tcp:{_port}");
                if (code != 0) { Status = $"adb reverse lỗi: {outp.Trim()}"; return; }
                Log.Write($"adb reverse tcp:{_port} cho {s}");
            }
        }
        Status = $"Đã kết nối {ready.Count} điện thoại qua USB";
    }

    static (int code, string output) Run(string exe, string args)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true,
        };
        using var p = Process.Start(psi)!;
        var o = p.StandardOutput.ReadToEndAsync();
        var e = p.StandardError.ReadToEndAsync();
        if (!p.WaitForExit(15000)) { try { p.Kill(); } catch { } return (-1, "adb không phản hồi"); }
        return (p.ExitCode, o.Result + e.Result);
    }
}
