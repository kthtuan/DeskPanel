using System.Diagnostics;
using System.IO.Compression;

namespace DeskPanel;

/// <summary>
/// Cap nhat tu file zip tai len trang cai dat: giai nen vao thu muc tam, kiem tra,
/// roi giao cho mot tac vu Task Scheduler rieng (DeskPanelUpdate) dung ban cu, chep ban moi, chay lai.
/// Dung tac vu rieng de viec chep khong bi dung theo khi DeskPanel tat.
/// </summary>
public static class Updater
{
    const long MaxZip = 200L * 1024 * 1024;
    static int _busy;

    public sealed record Result(bool Ok, string Message, string? NewVersion = null);

    public static async Task<Result> ApplyAsync(Stream body, string baseDir, CancellationToken ct)
    {
        if (Interlocked.Exchange(ref _busy, 1) == 1) return new(false, "Đang cập nhật, hãy chờ");
        try
        {
            CleanOld();
            var work = Path.Combine(Path.GetTempPath(), "DeskPanel-update-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..6]);
            Directory.CreateDirectory(work);
            var zipPath = Path.Combine(work, "update.zip");
            await using (var f = File.Create(zipPath))
            {
                var buf = new byte[81920]; long total = 0; int n;
                while ((n = await body.ReadAsync(buf, ct)) > 0)
                {
                    total += n;
                    if (total > MaxZip) return new(false, "File quá lớn (tối đa 200 MB)");
                    await f.WriteAsync(buf.AsMemory(0, n), ct);
                }
            }

            var src = Path.Combine(work, "files");
            try { ZipFile.ExtractToDirectory(zipPath, src); }
            catch (InvalidDataException) { return new(false, "File không phải zip hợp lệ"); }

            // Zip co the chua thu muc DeskPanel\ ben trong, hoac de file ngay goc
            var root = FindRoot(src);
            if (root == null) return new(false, "Trong zip không có DeskPanel.exe và thư mục wwwroot. Hãy chọn file DeskPanel-…-win-x64.zip");

            // Ban build cheo tu Linux khong co thong tin phien ban trong .exe, nen doc version.txt di kem
            string? newVer = null;
            var vf = Path.Combine(root, "version.txt");
            if (File.Exists(vf)) newVer = File.ReadAllText(vf).Trim();
            if (newVer != null && (newVer.Length > 20 || !System.Text.RegularExpressions.Regex.IsMatch(newVer, @"^\d+\.\d+\.\d+$"))) newVer = null;
            var curVer = typeof(Updater).Assembly.GetName().Version?.ToString(3);
            if (!OperatingSystem.IsWindows()) return new(true, $"Zip hợp lệ (chế độ thử, không phải Windows nên không cài)", newVer);

            // Lenh cua tac vu Task Scheduler bi gioi han 261 ky tu, nen dat script o thu muc cai (duong dan ngan)
            // va ghi san duong dan nguon/dich vao trong script thay vi truyen tham so.
            var dst = baseDir.TrimEnd('\\', '/');
            var scriptDir = Path.Combine(dst, "_update");
            Directory.CreateDirectory(scriptDir);
            var script = Path.Combine(scriptDir, "apply.ps1");
            static string Q(string x) => "'" + x.Replace("'", "''") + "'";
            File.WriteAllText(script, $"$Src = {Q(root)}\r\n$Dst = {Q(dst)}\r\n" + ApplyScript, new System.Text.UTF8Encoding(true));
            var cmd = $"powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{script}\"";
            if (cmd.Length > 261)
            {
                // Thu muc cai qua sau: dung ProgramData
                scriptDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DeskPanel");
                Directory.CreateDirectory(scriptDir);
                var s2 = Path.Combine(scriptDir, "apply.ps1");
                File.Copy(script, s2, true); script = s2;
                cmd = $"powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{script}\"";
            }
            Run("schtasks.exe", "/Create", "/F", "/TN", "DeskPanelUpdate", "/SC", "ONCE", "/ST", "00:00", "/RL", "HIGHEST", "/TR", cmd);
            Run("schtasks.exe", "/Run", "/TN", "DeskPanelUpdate");
            Log.Write($"Cap nhat: {curVer} -> {newVer ?? "?"} tu {root}");
            return new(true, $"Đang cài bản {newVer ?? "mới"} (bản hiện tại {curVer}). DeskPanel sẽ tắt và chạy lại trong khoảng 10–20 giây.", newVer);
        }
        catch (Exception ex)
        {
            Log.Write("Cap nhat loi: " + ex);
            return new(false, "Cập nhật lỗi: " + ex.Message);
        }
        finally { Interlocked.Exchange(ref _busy, 0); }
    }

    // Xoa thu muc tam cua cac lan cap nhat truoc (qua 1 ngay)
    static void CleanOld()
    {
        try
        {
            foreach (var d in Directory.GetDirectories(Path.GetTempPath(), "DeskPanel-update-*"))
                if (Directory.GetCreationTime(d) < DateTime.Now.AddDays(-1)) try { Directory.Delete(d, true); } catch { }
        }
        catch { }
    }

    static string? FindRoot(string dir)
    {
        bool Ok(string d) => File.Exists(Path.Combine(d, "DeskPanel.exe")) && File.Exists(Path.Combine(d, "wwwroot", "index.html")) && File.Exists(Path.Combine(d, "wwwroot", "settings.html"));
        if (Ok(dir)) return dir;
        foreach (var d in Directory.GetDirectories(dir)) if (Ok(d)) return d;
        return null;
    }

    static void Run(string file, params string[] args)
    {
        var psi = new ProcessStartInfo(file) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var err = p.StandardError.ReadToEnd(); p.StandardOutput.ReadToEnd();
        p.WaitForExit(15000);
        if (p.ExitCode != 0) throw new Exception($"{file} {args[0]} loi {p.ExitCode}: {err.Trim()}");
    }

    const string ApplyScript = """
$log = Join-Path $Dst 'update.log'
function W($m) { Add-Content -Path $log -Value ((Get-Date -Format 'yyyy-MM-dd HH:mm:ss') + ' ' + $m) -Encoding UTF8 }
try {
    W "Bat dau cap nhat tu $Src"
    Start-Sleep -Seconds 2
    Stop-ScheduledTask -TaskName 'DeskPanel' -ErrorAction SilentlyContinue
    for ($i = 0; $i -lt 20 -and (Get-Process DeskPanel -ErrorAction SilentlyContinue); $i++) { Start-Sleep -Milliseconds 500 }
    Get-Process DeskPanel -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 1
    Get-ChildItem -Path $Src -Recurse | Unblock-File -ErrorAction SilentlyContinue
    robocopy $Src $Dst /E /XF config.json config.bad.json deskpanel.log deskpanel.log.old update.log /XD platform-tools /R:5 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
    W "robocopy ma $LASTEXITCODE"
} catch { W ("Loi: " + $_) }
finally {
    if (Get-ScheduledTask -TaskName 'DeskPanel' -ErrorAction SilentlyContinue) { Start-ScheduledTask -TaskName 'DeskPanel' }
    else { Start-Process (Join-Path $Dst 'DeskPanel.exe') -WorkingDirectory $Dst }
    W "Da chay lai DeskPanel"
    Unregister-ScheduledTask -TaskName 'DeskPanelUpdate' -Confirm:$false -ErrorAction SilentlyContinue
}
""";
}
