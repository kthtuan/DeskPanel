using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DeskPanel;

/// <summary>
/// Danh sach phan mem da cai (lay tu loi tat trong Start Menu) va icon that cua file .exe/.lnk.
/// Icon duoc cache trong bo nho; dien thoai tai qua /api/icon/{ma-nut}.
/// </summary>
public static class AppCatalog
{
    public sealed record AppInfo(string Name, string Path);

    static List<AppInfo>? _cache;
    static DateTime _cacheAt;
    static readonly ConcurrentDictionary<string, byte[]?> Icons = new(StringComparer.OrdinalIgnoreCase);
    static readonly string[] Skip = { "uninstall", "gỡ cài", "go cai", "remove", "readme", "read me", "help", "manual", "documentation", "license", "website", "release notes", "what's new", "support" };

    public static List<AppInfo> List()
    {
        if (_cache != null && DateTime.UtcNow - _cacheAt < TimeSpan.FromMinutes(1)) return _cache;
        var list = new List<AppInfo>();
        if (!OperatingSystem.IsWindows())
        {
            // Che do thu tren Linux
            list.Add(new("AutoCAD 2024", @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Autodesk\AutoCAD 2024.lnk"));
            list.Add(new("Google Chrome", @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Google Chrome.lnk"));
            list.Add(new("Microsoft Excel", @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Excel.lnk"));
            list.Add(new("Zalo", @"C:\Users\me\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Zalo.lnk"));
        }
        else
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in new[] { Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) })
            {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                IEnumerable<string> files;
                try { files = Directory.EnumerateFiles(root, "*.lnk", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true }); }
                catch { continue; }
                foreach (var f in files)
                {
                    var name = Path.GetFileNameWithoutExtension(f).Trim();
                    var low = name.ToLowerInvariant();
                    if (name.Length == 0 || Skip.Any(low.Contains)) continue;
                    if (!seen.Add(name)) continue;
                    list.Add(new(name, f));
                }
            }
        }
        list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        _cache = list; _cacheAt = DateTime.UtcNow;
        return list;
    }

    /// <summary>PNG icon cua file (exe, lnk, thu muc...). Null neu khong lay duoc.</summary>
    public static byte[]? IconPng(string path)
    {
        if (!OperatingSystem.IsWindows()) return null;
        path = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
        if (!File.Exists(path) && !Directory.Exists(path)) return null;
        return Icons.GetOrAdd(path, p => Sta(() => OperatingSystem.IsWindows() ? Extract(p) : null));
    }

    // API Shell can luong STA
    static T? Sta<T>(Func<T?> f)
    {
        T? r = default;
        var t = new Thread(() => { try { r = f(); } catch (Exception ex) { Log.Write("Loi lay icon: " + ex.Message); } });
        if (OperatingSystem.IsWindows()) t.SetApartmentState(ApartmentState.STA);
        t.IsBackground = true;
        t.Start();
        t.Join(5000);
        return r;
    }

    [SupportedOSPlatform("windows")]
    static byte[]? Extract(string path)
    {
        var info = new SHFILEINFO();
        if (SHGetFileInfo(path, 0, ref info, (uint)Marshal.SizeOf<SHFILEINFO>(), SHGFI_SYSICONINDEX) == IntPtr.Zero) return Fallback(path);
        // Uu tien icon 256 px; neu phan mem chi co icon nho (nam goc trong khung 256) thi dung ban 48 px
        var big = FromList(SHIL_JUMBO, info.iIcon);
        if (big != null && ContentSize(big) > 64) return Png(big);
        big?.Dispose();
        var mid = FromList(SHIL_EXTRALARGE, info.iIcon);
        return mid != null ? Png(mid) : Fallback(path);
    }

    [SupportedOSPlatform("windows")]
    static System.Drawing.Bitmap? FromList(int list, int index)
    {
        var iid = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
        if (SHGetImageList(list, ref iid, out var il) != 0 || il == null) return null;
        try
        {
            IntPtr h = IntPtr.Zero;
            if (il.GetIcon(index, ILD_TRANSPARENT, ref h) != 0 || h == IntPtr.Zero) return null;
            try
            {
                using var ic = System.Drawing.Icon.FromHandle(h);
                return ic.ToBitmap();
            }
            finally { DestroyIcon(h); }
        }
        finally { Marshal.ReleaseComObject(il); }
    }

    [SupportedOSPlatform("windows")]
    static int ContentSize(System.Drawing.Bitmap b)
    {
        int minX = b.Width, minY = b.Height, maxX = -1, maxY = -1;
        for (int y = 0; y < b.Height; y += 2)
            for (int x = 0; x < b.Width; x += 2)
                if (b.GetPixel(x, y).A > 16) { if (x < minX) minX = x; if (x > maxX) maxX = x; if (y < minY) minY = y; if (y > maxY) maxY = y; }
        return maxX < 0 ? 0 : Math.Max(maxX - minX, maxY - minY);
    }

    [SupportedOSPlatform("windows")]
    static byte[] Png(System.Drawing.Bitmap b)
    {
        using (b)
        {
            using var ms = new MemoryStream();
            b.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }
    }

    [SupportedOSPlatform("windows")]
    static byte[]? Fallback(string path)
    {
        if (!File.Exists(path)) return null;
        using var ic = System.Drawing.Icon.ExtractAssociatedIcon(path);
        return ic == null ? null : Png(ic.ToBitmap());
    }

    const uint SHGFI_SYSICONINDEX = 0x4000;
    const int SHIL_EXTRALARGE = 2, SHIL_JUMBO = 4, ILD_TRANSPARENT = 1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct SHFILEINFO
    {
        public IntPtr hIcon; public int iIcon; public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr SHGetFileInfo(string path, uint attr, ref SHFILEINFO info, uint size, uint flags);
    [DllImport("shell32.dll", EntryPoint = "#727")]
    static extern int SHGetImageList(int list, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IImageList? ppv);
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);

    // Chi khai bao du thu tu vtable den GetIcon
    [ComImport, Guid("46EB5926-582E-4017-9FDF-E8998DAA0950"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IImageList
    {
        [PreserveSig] int Add(IntPtr a, IntPtr b, ref int c);
        [PreserveSig] int ReplaceIcon(int a, IntPtr b, ref int c);
        [PreserveSig] int SetOverlayImage(int a, int b);
        [PreserveSig] int Replace(int a, IntPtr b, IntPtr c);
        [PreserveSig] int AddMasked(IntPtr a, int b, ref int c);
        [PreserveSig] int Draw(IntPtr a);
        [PreserveSig] int Remove(int a);
        [PreserveSig] int GetIcon(int i, int flags, ref IntPtr icon);
    }
}
