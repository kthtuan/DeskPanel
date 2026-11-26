using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace DeskPanel;

/// <summary>
/// Mot file DeskPanel-Setup.exe lam ca viec cai dat:
///  - Chay tu ngoai thu muc da cai (vd. Downloads) => hoi, chep vao C:\DeskPanel, tao tac vu tu chay, dang ky "Apps",
///    cai PawnIO (nhiet do CPU), chay DeskPanel va mo trang cai dat.
///  - "DeskPanel.exe --uninstall" (Windows Settings > Apps > Go cai dat) => go sach.
///  - Chay tu thu muc da cai (tac vu khi dang nhap) => chay binh thuong.
/// Giao dien, APK Android nam san trong exe (xem EmbeddedWeb).
/// </summary>
public static class Setup
{
    public const string DefaultDir = @"C:\DeskPanel";
    const string TaskName = "DeskPanel";
    const string UninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\DeskPanel";
    static string Ver => typeof(Setup).Assembly.GetName().Version?.ToString(3) ?? "";
    const string Title = "DeskPanel";

    /// <summary>True neu da xu ly xong (cai/go) va chuong trinh nen thoat.</summary>
    public static bool TryRun(string[] args)
    {
        if (!OperatingSystem.IsWindows()) return false;
        if (args.Contains("--serve")) return false;
        if (args.Contains("--uninstall")) { Uninstall(); return true; }
        var self = Environment.ProcessPath;
        if (string.IsNullOrEmpty(self)) return false;
        var selfDir = Path.GetDirectoryName(self)!;
        var taskExe = TaskExe();
        if (taskExe != null && SameDir(Path.GetDirectoryName(taskExe)!, selfDir)) return false;
        try { Install(self, taskExe); }
        catch (Exception ex) { Log.Write("Loi cai dat: " + ex); Box("Cài đặt không thành công:\n\n" + ex.Message, 0x10); }
        return true;
    }

    static bool SameDir(string a, string b) =>
        string.Equals(Path.GetFullPath(a).TrimEnd('\\'), Path.GetFullPath(b).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);

    // Duong dan exe cua tac vu "DeskPanel" da co (ban cai bang zip cu hoac ban truoc)
    static string? TaskExe()
    {
        var (code, xml) = Run("schtasks.exe", "/Query", "/TN", TaskName, "/XML");
        if (code != 0) return null;
        var m = System.Text.RegularExpressions.Regex.Match(xml, "<Command>([^<]+)</Command>");
        return m.Success ? System.Net.WebUtility.HtmlDecode(m.Groups[1].Value).Trim().Trim('"') : null;
    }

    static void Install(string self, string? taskExe)
    {
        var dir = taskExe != null ? Path.GetDirectoryName(taskExe)! : DefaultDir;
        bool update = taskExe != null;
        var q = update
            ? $"Cập nhật DeskPanel lên bản {Ver}?\n\nThư mục: {dir}\nCài đặt nút và trang của bạn được giữ nguyên."
            : $"Cài DeskPanel {Ver} vào {dir}?\n\nDeskPanel sẽ tự chạy ngầm mỗi khi bạn đăng nhập Windows. Gỡ được trong Settings → Apps.";
        if (Box(q, 0x24 /* YESNO | QUESTION */) != 6) return;

        // Dung ban dang chay
        Run("schtasks.exe", "/End", "/TN", TaskName);
        foreach (var p in Process.GetProcessesByName("DeskPanel"))
            if (p.Id != Environment.ProcessId) { try { p.Kill(); p.WaitForExit(5000); } catch { } }

        Directory.CreateDirectory(dir);
        var exe = Path.Combine(dir, "DeskPanel.exe");
        if (!string.Equals(Path.GetFullPath(self), Path.GetFullPath(exe), StringComparison.OrdinalIgnoreCase))
        {
            for (int i = 0; ; i++)
            {
                try { File.Copy(self, exe, true); break; }
                catch (IOException) when (i < 10) { Thread.Sleep(500); }
            }
        }
        try { File.Delete(exe + ":Zone.Identifier"); } catch { }
        EmbeddedWeb.Extract(dir);
        foreach (var old in new[] { "install.cmd", "install.ps1", "update.cmd", "update.ps1", "uninstall.cmd", "uninstall.ps1" })
            try { File.Delete(Path.Combine(dir, old)); } catch { }

        RegisterTask(exe, dir);
        RegisterApp(exe, dir);
        bool pawnNew = EnsurePawnIO();

        Run("schtasks.exe", "/Run", "/TN", TaskName);
        int port = ReadPort(dir);
        bool up = false;
        for (int i = 0; i < 60 && !up; i++)
        {
            Thread.Sleep(1000);
            try { using var c = new System.Net.Sockets.TcpClient(); c.Connect("127.0.0.1", port); up = true; } catch { }
        }
        if (up)
        {
            // Mo qua explorer de trinh duyet khong chay bang quyen admin
            try { Process.Start("explorer.exe", $"http://localhost:{port}/settings.html"); } catch { }
            Box((update ? "Đã cập nhật DeskPanel " + Ver + "." : "Đã cài xong DeskPanel " + Ver + ".") +
                "\n\nTrang cài đặt đang mở trong trình duyệt." +
                (pawnNew ? "\n\nĐã cài PawnIO để đọc nhiệt độ CPU: khởi động lại máy một lần để có nhiệt độ." : ""), 0x40);
        }
        else
        {
            var log = Path.Combine(dir, "deskpanel.log");
            var tail = File.Exists(log) ? string.Join("\n", File.ReadLines(log).TakeLast(8)) : "(chưa có deskpanel.log)";
            bool running = Process.GetProcessesByName("DeskPanel").Any(p => p.Id != Environment.ProcessId);
            Box("Đã cài nhưng DeskPanel chưa chạy được sau 60 giây.\n\n" +
                (running ? "DeskPanel đang chạy nhưng chưa mở cổng " + port + " (cổng bị phần mềm khác chiếm?)."
                         : "DeskPanel không chạy. Xem Windows Security → Protection history có chặn DeskPanel.exe không.") +
                "\n\nNhật ký:\n" + tail + "\n\nChụp màn hình thông báo này gửi lại để được hỗ trợ.", 0x30);
        }
    }

    static int ReadPort(string dir)
    {
        try
        {
            var f = Path.Combine(dir, "config.json");
            if (File.Exists(f))
            {
                using var d = System.Text.Json.JsonDocument.Parse(File.ReadAllText(f));
                foreach (var p in d.RootElement.EnumerateObject())
                    if (p.Name.Equals("port", StringComparison.OrdinalIgnoreCase) && p.Value.TryGetInt32(out var v) && v > 0) return v;
            }
        }
        catch { }
        return 8765;
    }

    static void RegisterTask(string exe, string dir)
    {
        var user = Environment.UserDomainName + "\\" + Environment.UserName;
        string X(string s) => System.Security.SecurityElement.Escape(s)!;
        var xml = $"""
            <?xml version="1.0" encoding="UTF-16"?>
            <Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
              <Triggers><LogonTrigger><Enabled>true</Enabled><UserId>{X(user)}</UserId></LogonTrigger></Triggers>
              <Principals><Principal id="Author"><UserId>{X(user)}</UserId><LogonType>InteractiveToken</LogonType><RunLevel>HighestAvailable</RunLevel></Principal></Principals>
              <Settings>
                <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
                <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
                <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
                <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
                <RestartOnFailure><Interval>PT1M</Interval><Count>3</Count></RestartOnFailure>
                <Enabled>true</Enabled>
              </Settings>
              <Actions Context="Author"><Exec><Command>{X(exe)}</Command><WorkingDirectory>{X(dir)}</WorkingDirectory></Exec></Actions>
            </Task>
            """;
        var tmp = Path.Combine(Path.GetTempPath(), "deskpanel-task.xml");
        File.WriteAllText(tmp, xml, Encoding.Unicode);
        try
        {
            var (code, o) = Run("schtasks.exe", "/Create", "/TN", TaskName, "/XML", tmp, "/F");
            if (code != 0) throw new Exception("Không tạo được tác vụ tự chạy: " + o.Trim());
        }
        finally { try { File.Delete(tmp); } catch { } }
    }

    static void RegisterApp(string exe, string dir)
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            using var k = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(UninstallKey);
            k.SetValue("DisplayName", "DeskPanel");
            k.SetValue("DisplayVersion", Ver);
            k.SetValue("Publisher", "DeskPanel");
            k.SetValue("DisplayIcon", exe);
            k.SetValue("InstallLocation", dir);
            k.SetValue("UninstallString", $"\"{exe}\" --uninstall");
            k.SetValue("NoModify", 1, Microsoft.Win32.RegistryValueKind.DWord);
            k.SetValue("NoRepair", 1, Microsoft.Win32.RegistryValueKind.DWord);
            k.SetValue("EstimatedSize", (int)(new FileInfo(exe).Length / 1024), Microsoft.Win32.RegistryValueKind.DWord);
        }
        catch (Exception ex) { Log.Write("Khong dang ky duoc muc Apps: " + ex.Message); }
    }

    /// <summary>Cai PawnIO (driver doc nhiet do CPU) qua winget neu chua co. True neu vua cai.</summary>
    static bool EnsurePawnIO()
    {
        try
        {
            if (Run("sc.exe", "query", "PawnIO").Item1 == 0) return false;
            var (code, o) = Run("winget.exe", new[] { "install", "--id", "namazso.PawnIO", "-e", "--silent", "--accept-package-agreements", "--accept-source-agreements", "--disable-interactivity" }, 240_000);
            Log.Write($"Cai PawnIO: ma {code}");
            return code == 0;
        }
        catch (Exception ex) { Log.Write("Khong cai duoc PawnIO: " + ex.Message); return false; }
    }

    static void Uninstall()
    {
        var self = Environment.ProcessPath!;
        var dir = Path.GetDirectoryName(self)!;
        if (Box("Gỡ DeskPanel khỏi máy này?\n\nXóa luôn cài đặt nút và trang trong " + dir + ".", 0x24) != 6) return;
        Run("schtasks.exe", "/End", "/TN", TaskName);
        Run("schtasks.exe", "/Delete", "/TN", TaskName, "/F");
        Run("schtasks.exe", "/Delete", "/TN", "DeskPanelRestart", "/F");
        foreach (var p in Process.GetProcessesByName("DeskPanel"))
            if (p.Id != Environment.ProcessId) { try { p.Kill(); p.WaitForExit(5000); } catch { } }
        LanAccess.SetFirewall(false, 0);
        if (OperatingSystem.IsWindows()) try { Microsoft.Win32.Registry.LocalMachine.DeleteSubKeyTree(UninstallKey, false); } catch { }
        // Chi xoa nhung gi DeskPanel tao ra, sau khi exe nay thoat
        var files = new[] { "DeskPanel.exe", "config.json", "config.bad.json", "deskpanel.log", "deskpanel.log.old", "update.log", "lan-key.txt", "update-token.bin", "version.txt", "web.config", "HUONG-DAN.md",
                            "install.cmd", "install.ps1", "update.cmd", "update.ps1", "uninstall.cmd", "uninstall.ps1" };
        var dirs = new[] { "wwwroot", "platform-tools", "webview" };
        var sb = new StringBuilder("ping 127.0.0.1 -n 4 >nul");
        foreach (var f in files) sb.Append($" & del /f /q \"{Path.Combine(dir, f)}\"");
        foreach (var d in dirs) sb.Append($" & rmdir /s /q \"{Path.Combine(dir, d)}\"");
        sb.Append($" & rmdir \"{dir}\"");
        try { Process.Start(new ProcessStartInfo("cmd.exe", "/c " + sb) { CreateNoWindow = true, UseShellExecute = false }); } catch { }
        Box("Đã gỡ DeskPanel.", 0x40);
    }

    static (int, string) Run(string exe, params string[] args) => Run(exe, args, 30_000);

    static (int, string) Run(string exe, string[] args, int timeoutMs)
    {
        try
        {
            var psi = new ProcessStartInfo(exe) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var a in args) psi.ArgumentList.Add(a);
            using var p = Process.Start(psi)!;
            var o = p.StandardOutput.ReadToEndAsync();
            var e = p.StandardError.ReadToEndAsync();
            if (!p.WaitForExit(timeoutMs)) { try { p.Kill(true); } catch { } return (-1, "het thoi gian"); }
            return (p.ExitCode, o.Result + e.Result);
        }
        catch (Exception ex) { return (-1, ex.Message); }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
    static int Box(string text, uint type) => MessageBoxW(IntPtr.Zero, text, Title, type | 0x10000 /* SETFOREGROUND */);
}

/// <summary>Giao dien dien thoai, trang cai dat va APK nam trong exe; chep ra thu muc wwwroot khi chay.</summary>
public static class EmbeddedWeb
{
    public static void Extract(string baseDir)
    {
        var asm = Assembly.GetExecutingAssembly();
        foreach (var name in asm.GetManifestResourceNames())
        {
            var rel = name.Replace('\\', '/');
            if (!rel.StartsWith("wwwroot/")) continue;
            try
            {
                using var s = asm.GetManifestResourceStream(name)!;
                using var ms = new MemoryStream(); s.CopyTo(ms);
                var bytes = ms.ToArray();
                var path = Path.Combine(baseDir, rel.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(path) && new FileInfo(path).Length == bytes.Length && File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, bytes);
            }
            catch (Exception ex) { Log.Write("Khong chep duoc " + rel + ": " + ex.Message); }
        }
    }
}
