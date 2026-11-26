using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DeskPanel;

/// <summary>Doc chuoi "Ctrl+Shift+Esc, Ctrl+S" thanh danh sach ma phim ao (virtual-key).</summary>
public static class KeyParser
{
    static readonly Dictionary<string, ushort> Map = Build();

    static Dictionary<string, ushort> Build()
    {
        var m = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase)
        {
            ["ctrl"] = 0x11, ["control"] = 0x11, ["shift"] = 0x10, ["alt"] = 0x12, ["win"] = 0x5B,
            ["esc"] = 0x1B, ["escape"] = 0x1B, ["enter"] = 0x0D, ["tab"] = 0x09, ["space"] = 0x20,
            ["backspace"] = 0x08, ["delete"] = 0x2E, ["del"] = 0x2E, ["insert"] = 0x2D, ["ins"] = 0x2D,
            ["home"] = 0x24, ["end"] = 0x23, ["pageup"] = 0x21, ["pagedown"] = 0x22,
            ["up"] = 0x26, ["down"] = 0x28, ["left"] = 0x25, ["right"] = 0x27,
            ["printscreen"] = 0x2C, ["prtsc"] = 0x2C, ["pause"] = 0x13, ["capslock"] = 0x14, ["apps"] = 0x5D,
            ["volumemute"] = 0xAD, ["volumedown"] = 0xAE, ["volumeup"] = 0xAF,
            ["medianext"] = 0xB0, ["mediaprev"] = 0xB1, ["mediastop"] = 0xB2, ["mediaplaypause"] = 0xB3,
            ["plus"] = 0xBB, ["minus"] = 0xBD, ["comma"] = 0xBC, ["period"] = 0xBE,
        };
        for (char c = 'A'; c <= 'Z'; c++) m[c.ToString()] = c;
        for (char c = '0'; c <= '9'; c++) m[c.ToString()] = c;
        for (int i = 1; i <= 24; i++) m["F" + i] = (ushort)(0x6F + i);
        return m;
    }

    /// <summary>Tra ve null neu hop le.</summary>
    public static string? TryParseSequence(string text, out List<ushort[]> combos)
    {
        combos = new();
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var keys = new List<ushort>();
            foreach (var k in part.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!Map.TryGetValue(k, out var vk)) return $"không hiểu phím '{k}'";
                keys.Add(vk);
            }
            if (keys.Count == 0) return "tổ hợp phím rỗng";
            combos.Add(keys.ToArray());
        }
        return combos.Count == 0 ? "chưa nhập phím" : null;
    }
}

public readonly record struct ActionResult(bool Ok, string? Error = null);

public static class ActionRunner
{
    static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task<ActionResult> RunAsync(ButtonDef b)
    {
        if (!OperatingSystem.IsWindows()) return new(true); // che do thu tren may khong phai Windows
        if (!await Gate.WaitAsync(TimeSpan.FromSeconds(5))) return new(false, "Đang bận thực hiện nút khác");
        try
        {
            return await Task.Run(() => Run(b));
        }
        catch (Exception ex)
        {
            Log.Write($"Loi nut {b.Id}: {ex}");
            return new(false, ex.Message);
        }
        finally { Gate.Release(); }
    }

    static ActionResult Run(ButtonDef b)
    {
        switch (b.Type)
        {
            case "run":
                return Launch(Environment.ExpandEnvironmentVariables(b.Value.Trim()), b.Args);
            case "url":
                return Launch(b.Value.Trim(), null);
            case "keys":
            {
                if (!string.IsNullOrWhiteSpace(b.Focus) && !Win.FocusProcess(b.Focus!))
                    return new(false, $"Chưa mở chương trình '{b.Focus}'");
                var err = KeyParser.TryParseSequence(b.Value, out var combos);
                if (err != null) return new(false, err);
                foreach (var c in combos) { Win.SendCombo(c); Thread.Sleep(40); }
                return new(true);
            }
            case "text":
                if (!string.IsNullOrWhiteSpace(b.Focus) && !Win.FocusProcess(b.Focus!))
                    return new(false, $"Chưa mở chương trình '{b.Focus}'");
                Win.TypeText(b.Value);
                if (b.Enter) Win.SendCombo(new ushort[] { 0x0D });
                return new(true);
            case "cad":
                if (!Win.FocusProcess(string.IsNullOrWhiteSpace(b.Focus) ? ConfigStore.CadProcesses : b.Focus!))
                    return new(false, "AutoCAD chưa mở");
                // Huy lenh dang do roi go lenh moi. Trong AutoCAD, dau cach tuong duong Enter.
                Win.SendCombo(new ushort[] { 0x1B }); Thread.Sleep(30);
                Win.SendCombo(new ushort[] { 0x1B }); Thread.Sleep(30);
                Win.TypeText(b.Value.Trim());
                Win.SendCombo(new ushort[] { 0x0D });
                return new(true);
        }
        return new(false, "Loại nút không hợp lệ");
    }

    /// <summary>
    /// DeskPanel chay voi quyen admin. De chuong trinh duoc mo KHONG mang quyen admin,
    /// nho explorer.exe mo ho (explorer chay quyen thuong). Neu co tham so thi buoc phai mo truc tiep.
    /// </summary>
    static ActionResult Launch(string target, string? args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            var explorer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
            Process.Start(new ProcessStartInfo(explorer) { ArgumentList = { target }, UseShellExecute = false });
        }
        else
        {
            var psi = new ProcessStartInfo(target, args) { UseShellExecute = true };
            var dir = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) psi.WorkingDirectory = dir;
            Process.Start(psi);
        }
        return new(true);
    }
}

static class Win
{
    const int INPUT_KEYBOARD = 1;
    const uint KEYEVENTF_KEYUP = 0x2, KEYEVENTF_UNICODE = 0x4, KEYEVENTF_EXTENDEDKEY = 0x1;

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT { public int type; public InputUnion u; }

    [StructLayout(LayoutKind.Explicit)]
    struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }

    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public IntPtr dwExtraInfo; }

    [DllImport("user32.dll", SetLastError = true)] static extern uint SendInput(uint n, INPUT[] inputs, int size);
    [DllImport("user32.dll")] static extern short VkKeyScanW(char ch);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern bool BringWindowToTop(IntPtr h);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint a, uint b, bool attach);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();

    static readonly HashSet<ushort> Extended = new() { 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x2D, 0x2E, 0x5B, 0x5D, 0x2C };

    static INPUT Key(ushort vk, bool up, ushort scan = 0, uint extra = 0)
    {
        uint flags = (up ? KEYEVENTF_KEYUP : 0) | extra;
        if (Extended.Contains(vk)) flags |= KEYEVENTF_EXTENDEDKEY;
        return new INPUT { type = INPUT_KEYBOARD, u = new InputUnion { ki = new KEYBDINPUT { wVk = vk, wScan = scan, dwFlags = flags } } };
    }

    static void Send(List<INPUT> list)
    {
        if (list.Count == 0) return;
        SendInput((uint)list.Count, list.ToArray(), Marshal.SizeOf<INPUT>());
    }

    public static void SendCombo(ushort[] keys)
    {
        var l = new List<INPUT>();
        foreach (var k in keys) l.Add(Key(k, false));
        for (int i = keys.Length - 1; i >= 0; i--) l.Add(Key(keys[i], true));
        Send(l);
    }

    /// <summary>Ky tu co tren ban phim thi gui phim that (AutoCAD nhan tot hon), con lai gui Unicode.</summary>
    public static void TypeText(string text)
    {
        foreach (var raw in text.Replace("\r\n", "\n"))
        {
            if (raw == '\n') { SendCombo(new ushort[] { 0x0D }); Thread.Sleep(10); continue; }
            short r = raw < 128 ? VkKeyScanW(raw) : (short)-1;
            if (r != -1)
            {
                var vk = (ushort)(r & 0xFF);
                int mods = (r >> 8) & 0xFF;
                var keys = new List<ushort>();
                if ((mods & 1) != 0) keys.Add(0x10);
                if ((mods & 2) != 0) keys.Add(0x11);
                if ((mods & 4) != 0) keys.Add(0x12);
                keys.Add(vk);
                SendCombo(keys.ToArray());
            }
            else
            {
                Send(new List<INPUT> { Key(0, false, raw, KEYEVENTF_UNICODE), Key(0, true, raw, KEYEVENTF_UNICODE) });
            }
            Thread.Sleep(8);
        }
    }

    /// <summary>Dua cua so chinh cua tien trinh (vd "acad,acadlt") len truoc. Tra ve false neu khong tim thay.</summary>
    public static bool FocusProcess(string names)
    {
        IntPtr hwnd = IntPtr.Zero;
        foreach (var n in names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var name = n.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? n[..^4] : n;
            foreach (var p in Process.GetProcessesByName(name))
            {
                if (p.MainWindowHandle != IntPtr.Zero) { hwnd = p.MainWindowHandle; break; }
            }
            if (hwnd != IntPtr.Zero) break;
        }
        if (hwnd == IntPtr.Zero) return false;
        if (GetForegroundWindow() == hwnd) return true;

        if (IsIconic(hwnd)) ShowWindow(hwnd, 9 /*SW_RESTORE*/);
        if (SetForegroundWindow(hwnd)) { Thread.Sleep(120); return true; }

        // Windows chan doi cua so tu tien trinh nen; gan tam vao luong cua cua so dang hien
        var fg = GetForegroundWindow();
        uint fgThread = GetWindowThreadProcessId(fg, out _);
        uint me = GetCurrentThreadId();
        bool attached = fgThread != 0 && fgThread != me && AttachThreadInput(me, fgThread, true);
        try
        {
            BringWindowToTop(hwnd);
            SetForegroundWindow(hwnd);
        }
        finally { if (attached) AttachThreadInput(me, fgThread, false); }
        Thread.Sleep(120);
        return true;
    }
}
