using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using QRCoder;

namespace DeskPanel;

/// <summary>
/// Che do Wi-Fi: dien thoai trong cung mang LAN vao thang DeskPanel, khong can cap USB.
/// Moi thiet bi LAN phai co ma ghep noi (lan-key.txt, chi nam tren PC). Ma di kem trong ma QR
/// tren trang cai dat; lan dau vao bang ?key=... thi DeskPanel dat cookie roi bo ma khoi dia chi.
/// Tu LAN chi xem bang dieu khien va bam nut; khong mo duoc trang cai dat, cap nhat hay doi cau hinh.
/// </summary>
public sealed class LanAccess
{
    public const string CookieName = "dp_key";
    public const string FirewallRule = "DeskPanel LAN";
    readonly string _path;
    string _key;

    public LanAccess(string baseDir)
    {
        _path = Path.Combine(baseDir, "lan-key.txt");
        string? k = null;
        try { if (File.Exists(_path)) k = File.ReadAllText(_path).Trim(); } catch { }
        if (k == null || k.Length < 16 || !k.All(char.IsAsciiLetterOrDigit)) k = Rotate();
        _key = k;
    }

    public string Key => _key;

    public string Rotate()
    {
        const string abc = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        var k = new string(RandomNumberGenerator.GetItems<char>(abc, 24));
        try { File.WriteAllText(_path, k); } catch (Exception ex) { Log.Write("Khong ghi duoc lan-key.txt: " + ex.Message); }
        _key = k;
        return k;
    }

    public bool Matches(string? candidate) =>
        !string.IsNullOrEmpty(candidate) &&
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(candidate), Encoding.UTF8.GetBytes(_key));

    public static bool IsLoopback(IPAddress? ip)
    {
        if (ip == null) return true; // ket noi noi bo (test server)
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        return IPAddress.IsLoopback(ip);
    }

    /// <summary>Duong dan thiet bi LAN duoc phep goi (da co ma). Con lai deu bi chan.</summary>
    public static bool Allowed(string method, string path)
    {
        path = path.ToLowerInvariant();
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method))
        {
            if (path.StartsWith("/api/")) return path is "/api/config" or "/api/stats" or "/api/status" or "/api/media/art" || path.StartsWith("/api/icon/");
            if (path.Contains("settings")) return false;
            return true; // index.html, manifest, /ws, deskpanel.apk
        }
        if (HttpMethods.IsPost(method)) return path.StartsWith("/api/action/") || path.StartsWith("/api/media/");
        return false;
    }

    /// <summary>Dia chi IPv4 cua PC trong mang noi bo, uu tien card Wi-Fi/Ethernet dang chay.</summary>
    public static List<string> Addresses()
    {
        var list = new List<(string ip, int score)>();
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;
                var name = (ni.Name + " " + ni.Description).ToLowerInvariant();
                bool virt = name.Contains("virtual") || name.Contains("vmware") || name.Contains("hyper-v") || name.Contains("vethernet") || name.Contains("wsl") || name.Contains("docker") || name.Contains("vpn") || name.Contains("tap") || name.Contains("bluetooth");
                var props = ni.GetIPProperties();
                bool gw = props.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.Equals(IPAddress.Any));
                foreach (var ua in props.UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    var b = ua.Address.GetAddressBytes();
                    if (b[0] == 169 && b[1] == 254) continue;
                    bool priv = b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168);
                    int score = (priv ? 4 : 0) + (gw ? 2 : 0) + (virt ? 0 : 1) + (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? 1 : 0);
                    list.Add((ua.Address.ToString(), score));
                }
            }
        }
        catch (Exception ex) { Log.Write("Khong doc duoc dia chi mang: " + ex.Message); }
        return list.OrderByDescending(x => x.score).Select(x => x.ip).Distinct().ToList();
    }

    public static string QrSvg(string text)
    {
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
        return new SvgQRCode(data).GetGraphic(6, "#000000", "#ffffff", true, SvgQRCode.SizingMode.ViewBoxAttribute);
    }

    /// <summary>Mo/dong cong tren Windows Firewall (chi mang Private/Domain). Tra ve loi neu co.</summary>
    public static string? SetFirewall(bool open, int port)
    {
        if (!OperatingSystem.IsWindows()) return null;
        try
        {
            NetSh("advfirewall", "firewall", "delete", "rule", $"name={FirewallRule}");
            if (!open) return null;
            var (code, output) = NetSh("advfirewall", "firewall", "add", "rule", $"name={FirewallRule}", "dir=in", "action=allow", "protocol=TCP", $"localport={port}", "profile=private,domain");
            return code == 0 ? null : "Không mở được cổng trên Windows Firewall: " + output.Trim();
        }
        catch (Exception ex) { return "Không mở được cổng trên Windows Firewall: " + ex.Message; }
    }

    static (int, string) NetSh(params string[] args)
    {
        var psi = new ProcessStartInfo("netsh.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit(10000);
        return (p.ExitCode, o);
    }
}
