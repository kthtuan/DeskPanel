using System.Security.Cryptography;
using System.Text;

namespace DeskPanel;

/// <summary>
/// Token GitHub de doc repo private. Luu rieng (khong nam trong config.json, khong gui ra dien thoai),
/// ma hoa bang DPAPI theo tai khoan Windows dang dung: chep file sang may/tai khoan khac se khong giai ma duoc.
/// </summary>
public sealed class TokenStore
{
    readonly string _path;
    string? _token;
    static readonly byte[] Entropy = Encoding.UTF8.GetBytes("DeskPanel.UpdateToken.v1");

    public TokenStore(string baseDir)
    {
        _path = Path.Combine(baseDir, "update-token.bin");
        try
        {
            if (File.Exists(_path))
            {
                var raw = File.ReadAllBytes(_path);
                _token = Encoding.UTF8.GetString(OperatingSystem.IsWindows()
                    ? ProtectedData.Unprotect(raw, Entropy, DataProtectionScope.CurrentUser)
                    : raw); // chi de thu tren Linux
            }
        }
        catch (Exception ex) { Log.Write("Khong doc duoc token: " + ex.Message); _token = null; }
    }

    public string? Token => _token;
    public bool HasToken => !string.IsNullOrEmpty(_token);

    public void Set(string? token)
    {
        token = token?.Trim();
        if (string.IsNullOrEmpty(token)) { _token = null; try { File.Delete(_path); } catch { } return; }
        var data = Encoding.UTF8.GetBytes(token);
        File.WriteAllBytes(_path, OperatingSystem.IsWindows() ? ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser) : data);
        _token = token;
    }

    /// <summary>Token GitHub hop le chi gom chu, so va dau gach duoi.</summary>
    public static bool LooksValid(string t) => t.Length is >= 20 and <= 255 && t.All(c => char.IsAsciiLetterOrDigit(c) || c == '_');
}
