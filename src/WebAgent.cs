using System.Runtime.InteropServices;

namespace DeskPanel;

/// <summary>
/// Dang nhap Claude / ChatGPT tren web ngay trong DeskPanel (cua so WebView2 cua Windows), roi lay % gioi han
/// bang chinh phien dang nhap do, giong trang "Muc su dung" tren web. Dang nhap luu trong thu muc webview canh exe.
/// Khong luu mat khau; DeskPanel chi goi trang claude.ai / chatgpt.com trong trinh duyet an.
/// </summary>
public static partial class WebAgent
{
    public static string LoginUrl(string site) => site == "claude" ? "https://claude.ai/login" : "https://chatgpt.com/auth/login";
    static string ProbeUrl(string site) => site == "claude" ? "https://claude.ai/api/organizations" : "https://chatgpt.com/api/auth/session";
    static string Host(string site) => site == "claude" ? "claude.ai" : "chatgpt.com";

    // Chay trong trang claude.ai: lay to chuc chinh roi gioi han su dung
    const string ClaudeProbe = """
        (async()=>{try{
          const r=await fetch('/api/organizations',{credentials:'include'});
          if(!r.ok) return JSON.stringify({status:r.status});
          const orgs=await r.json();
          const o=(orgs||[]).find(x=>(x.capabilities||[]).includes('chat'))||(orgs||[])[0];
          if(!o) return JSON.stringify({status:404});
          const u=await fetch('/api/organizations/'+o.uuid+'/usage',{credentials:'include'});
          return JSON.stringify({status:u.status,tier:o.rate_limit_tier||'',caps:o.capabilities||[],usage:u.ok?await u.json():null});
        }catch(e){return JSON.stringify({status:0,error:String(e)})}})()
        """;

    // Chay trong trang chatgpt.com: lay ma phien roi goi trang gioi han cua Codex/ChatGPT
    const string ChatGptProbe = """
        (async()=>{try{
          const s=await (await fetch('/api/auth/session',{credentials:'include'})).json();
          if(!s||!s.accessToken) return JSON.stringify({status:401});
          const h={Authorization:'Bearer '+s.accessToken};
          if(s.account&&s.account.id) h['ChatGPT-Account-Id']=s.account.id;
          const r=await fetch('/backend-api/wham/usage',{headers:h,credentials:'include'});
          return JSON.stringify({status:r.status,plan:(s.account&&s.account.planType)||'',usage:r.ok?await r.json():null});
        }catch(e){return JSON.stringify({status:0,error:String(e)})}})()
        """;

    static string Probe(string site) => site == "claude" ? ClaudeProbe : ChatGptProbe;
    static string DataDir => Path.Combine(AppContext.BaseDirectory, "webview");
}

#if WINDOWS
public static partial class WebAgent
{
    static readonly SemaphoreSlim _gate = new(1, 1);
    static Microsoft.Web.WebView2.Core.CoreWebView2Environment? _env;
    public static volatile string? Busy;

    public static bool Available
    {
        get
        {
            try { return !string.IsNullOrEmpty(Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString()); }
            catch { return false; }
        }
    }

    static async Task<Microsoft.Web.WebView2.Core.CoreWebView2Environment> Env()
    {
        if (_env != null) return _env;
        Directory.CreateDirectory(DataDir);
        _env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, DataDir);
        return _env;
    }

    static async Task<string?> Eval(Microsoft.Web.WebView2.Core.CoreWebView2 cw, string expr)
    {
        var arg = System.Text.Json.JsonSerializer.Serialize(new { expression = expr, awaitPromise = true, returnByValue = true });
        var res = await cw.CallDevToolsProtocolMethodAsync("Runtime.evaluate", arg);
        using var d = System.Text.Json.JsonDocument.Parse(res);
        return d.RootElement.TryGetProperty("result", out var r) && r.TryGetProperty("value", out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String ? v.GetString() : null;
    }

    static bool LoggedIn(string? probe)
    {
        if (probe == null) return false;
        try { using var d = System.Text.Json.JsonDocument.Parse(probe); return d.RootElement.TryGetProperty("status", out var s) && s.GetInt32() == 200; }
        catch { return false; }
    }

    /// <summary>Mo cua so dang nhap; tra ve true khi dang nhap xong (cua so tu dong).</summary>
    public static async Task<bool> LoginAsync(string site)
    {
        await _gate.WaitAsync();
        Busy = site;
        try
        {
            return await Win.OnUi(async () =>
            {
                var title = (site == "claude" ? "Đăng nhập Claude" : "Đăng nhập ChatGPT") + " · DeskPanel (cửa sổ tự đóng khi xong)";
                var closed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var hwnd = Win.CreateWindow(title, 1040, 820, true, () => closed.TrySetResult(true));
                Microsoft.Web.WebView2.Core.CoreWebView2Controller? ctl = null;
                try
                {
                    ctl = await (await Env()).CreateCoreWebView2ControllerAsync(hwnd);
                    Win.Attach(hwnd, ctl);
                    ctl.CoreWebView2.Navigate(LoginUrl(site));
                    while (!closed.Task.IsCompleted)
                    {
                        await Task.WhenAny(closed.Task, Task.Delay(2500));
                        if (closed.Task.IsCompleted) break;
                        try
                        {
                            if (Uri.TryCreate(ctl.CoreWebView2.Source, UriKind.Absolute, out var u) && u.Host.EndsWith(Host(site), StringComparison.OrdinalIgnoreCase)
                                && LoggedIn(await Eval(ctl.CoreWebView2, Probe(site))))
                            { Win.Close(hwnd); return true; }
                        }
                        catch { }
                    }
                    return false;
                }
                catch (Exception ex) { Log.Write("Loi mo WebView2: " + ex.Message); Win.Close(hwnd); return false; }
                finally { try { ctl?.Close(); } catch { } }
            });
        }
        finally { Busy = null; _gate.Release(); }
    }

    /// <summary>Lay JSON gioi han bang phien da dang nhap, trong trinh duyet an. Null neu loi.</summary>
    public static async Task<string?> FetchAsync(string site)
    {
        if (!await _gate.WaitAsync(TimeSpan.FromSeconds(1))) return null; // dang mo cua so dang nhap
        try
        {
            var job = Win.OnUi(async () =>
            {
                var ctl = await (await Env()).CreateCoreWebView2ControllerAsync(Win.Hidden);
                try
                {
                    ctl.IsVisible = false;
                    var cw = ctl.CoreWebView2;
                    var nav = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    cw.NavigationCompleted += (_, e) => nav.TrySetResult(e.IsSuccess);
                    cw.Navigate(ProbeUrl(site));
                    await Task.WhenAny(nav.Task, Task.Delay(30000));
                    return await Eval(cw, Probe(site));
                }
                finally { ctl.Close(); }
            });
            var finished = await Task.WhenAny(job, Task.Delay(60000));
            return finished == job ? await job : null;
        }
        finally { _gate.Release(); }
    }

    /// <summary>Xoa cookie dang nhap cua trang.</summary>
    public static async Task LogoutAsync(string site)
    {
        await _gate.WaitAsync();
        try
        {
            await Win.OnUi(async () =>
            {
                var ctl = await (await Env()).CreateCoreWebView2ControllerAsync(Win.Hidden);
                try
                {
                    var cm = ctl.CoreWebView2.CookieManager;
                    var hosts = site == "claude" ? new[] { "https://claude.ai", "https://anthropic.com" }
                                                 : new[] { "https://chatgpt.com", "https://auth.openai.com", "https://openai.com" };
                    foreach (var h in hosts)
                        foreach (var c in await cm.GetCookiesAsync(h)) cm.DeleteCookie(c);
                }
                finally { ctl.Close(); }
                return true;
            });
        }
        finally { _gate.Release(); }
    }

    /// <summary>Cua so Win32 toi gian + vong lap thong diep tren luong STA rieng (WebView2 can).</summary>
    static class Win
    {
        const uint WM_SIZE = 0x0005, WM_CLOSE = 0x0010, WM_APP_RUN = 0x8001;
        const uint WS_OVERLAPPEDWINDOW = 0x00CF0000, WS_VISIBLE = 0x10000000;
        static readonly object _lock = new();
        static IntPtr _hidden;
        static WndProcD? _proc;
        static readonly System.Collections.Concurrent.ConcurrentQueue<(SendOrPostCallback, object?)> _queue = new();
        static readonly Dictionary<IntPtr, (Microsoft.Web.WebView2.Core.CoreWebView2Controller? ctl, Action? onClose)> _wins = new();

        public static IntPtr Hidden { get { Start(); return _hidden; } }

        sealed class Ctx : SynchronizationContext
        {
            public override void Post(SendOrPostCallback d, object? state) { _queue.Enqueue((d, state)); PostMessageW(_hidden, WM_APP_RUN, IntPtr.Zero, IntPtr.Zero); }
            public override void Send(SendOrPostCallback d, object? state) => throw new NotSupportedException();
        }

        static void Start()
        {
            lock (_lock)
            {
                if (_hidden != IntPtr.Zero) return;
                var ready = new ManualResetEventSlim();
                var t = new Thread(() =>
                {
                    _proc = WndProc;
                    var wc = new WNDCLASSEX
                    {
                        cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(), lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_proc),
                        hInstance = GetModuleHandleW(null), hCursor = LoadCursorW(IntPtr.Zero, 32512), hbrBackground = (IntPtr)6, lpszClassName = "DeskPanelWeb",
                    };
                    RegisterClassExW(ref wc);
                    _hidden = CreateWindowExW(0, "DeskPanelWeb", "DeskPanel", 0, 0, 0, 1, 1, IntPtr.Zero, IntPtr.Zero, wc.hInstance, IntPtr.Zero);
                    SynchronizationContext.SetSynchronizationContext(new Ctx());
                    ready.Set();
                    while (GetMessageW(out var msg, IntPtr.Zero, 0, 0) > 0) { TranslateMessage(ref msg); DispatchMessageW(ref msg); }
                });
                t.SetApartmentState(ApartmentState.STA);
                t.IsBackground = true;
                t.Start();
                ready.Wait();
            }
        }

        public static Task<T> OnUi<T>(Func<Task<T>> f)
        {
            Start();
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.Enqueue((async _ => { try { tcs.SetResult(await f()); } catch (Exception ex) { tcs.SetException(ex); } }, null));
            PostMessageW(_hidden, WM_APP_RUN, IntPtr.Zero, IntPtr.Zero);
            return tcs.Task;
        }

        public static IntPtr CreateWindow(string title, int w, int h, bool visible, Action onClose)
        {
            int sw = GetSystemMetrics(0), sh = GetSystemMetrics(1);
            w = Math.Min(w, sw - 40); h = Math.Min(h, sh - 80);
            var hwnd = CreateWindowExW(0, "DeskPanelWeb", title, WS_OVERLAPPEDWINDOW | (visible ? WS_VISIBLE : 0), (sw - w) / 2, (sh - h) / 2, w, h, IntPtr.Zero, IntPtr.Zero, GetModuleHandleW(null), IntPtr.Zero);
            _wins[hwnd] = (null, onClose);
            SetForegroundWindow(hwnd);
            return hwnd;
        }

        public static void Attach(IntPtr hwnd, Microsoft.Web.WebView2.Core.CoreWebView2Controller ctl)
        {
            if (_wins.TryGetValue(hwnd, out var s)) _wins[hwnd] = (ctl, s.onClose);
            Fit(hwnd);
        }

        public static void Close(IntPtr hwnd) => PostMessageW(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

        static void Fit(IntPtr hwnd)
        {
            if (_wins.TryGetValue(hwnd, out var s) && s.ctl != null && GetClientRect(hwnd, out var r))
                s.ctl.Bounds = new System.Drawing.Rectangle(0, 0, r.right - r.left, r.bottom - r.top);
        }

        static IntPtr WndProc(IntPtr h, uint m, IntPtr w, IntPtr l)
        {
            if (m == WM_APP_RUN && h == _hidden)
            {
                while (_queue.TryDequeue(out var job)) { try { job.Item1(job.Item2); } catch (Exception ex) { Log.Write("Loi WebView2: " + ex.Message); } }
                return IntPtr.Zero;
            }
            if (m == WM_SIZE) { Fit(h); return IntPtr.Zero; }
            if (m == WM_CLOSE && _wins.TryGetValue(h, out var s))
            {
                _wins.Remove(h);
                try { s.ctl?.Close(); } catch { }
                DestroyWindow(h);
                s.onClose?.Invoke();
                return IntPtr.Zero;
            }
            return DefWindowProcW(h, m, w, l);
        }

        delegate IntPtr WndProcD(IntPtr h, uint m, IntPtr w, IntPtr l);
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct WNDCLASSEX
        {
            public uint cbSize, style; public IntPtr lpfnWndProc; public int cbClsExtra, cbWndExtra;
            public IntPtr hInstance, hIcon, hCursor, hbrBackground; public string? lpszMenuName; public string lpszClassName; public IntPtr hIconSm;
        }
        [StructLayout(LayoutKind.Sequential)] struct MSG { public IntPtr hwnd; public uint message; public IntPtr wParam, lParam; public uint time; public int x, y, priv; }
        [StructLayout(LayoutKind.Sequential)] struct RECT { public int left, top, right, bottom; }
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern ushort RegisterClassExW(ref WNDCLASSEX wc);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr CreateWindowExW(uint ex, string cls, string title, uint style, int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr inst, IntPtr param);
        [DllImport("user32.dll")] static extern IntPtr DefWindowProcW(IntPtr h, uint m, IntPtr w, IntPtr l);
        [DllImport("user32.dll")] static extern int GetMessageW(out MSG msg, IntPtr h, uint min, uint max);
        [DllImport("user32.dll")] static extern bool TranslateMessage(ref MSG msg);
        [DllImport("user32.dll")] static extern IntPtr DispatchMessageW(ref MSG msg);
        [DllImport("user32.dll")] static extern bool PostMessageW(IntPtr h, uint m, IntPtr w, IntPtr l);
        [DllImport("user32.dll")] static extern bool DestroyWindow(IntPtr h);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] static extern int GetSystemMetrics(int i);
        [DllImport("user32.dll")] static extern IntPtr LoadCursorW(IntPtr inst, int id);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandleW(string? name);
    }
}
#else
public static partial class WebAgent
{
    public static volatile string? Busy;
    public static bool Available => false;
    public static Task<bool> LoginAsync(string site) => Task.FromResult(false);
    public static Task<string?> FetchAsync(string site) => Task.FromResult<string?>(Environment.GetEnvironmentVariable("DESKPANEL_FAKE_WEB_" + site.ToUpperInvariant()));
    public static Task LogoutAsync(string site) => Task.CompletedTask;
}
#endif
