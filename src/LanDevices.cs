using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DeskPanel;

public sealed class LanDevice
{
    public string Id { get; set; } = "";
    /// <summary>SHA-256 cua ma thiet bi (ma that chi nam trong cookie cua dien thoai).</summary>
    public string Hash { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Ip { get; set; }
    public long Created { get; set; }
    public long Seen { get; set; }
    public bool Revoked { get; set; }
}

/// <summary>
/// Moi dien thoai ghep noi qua Wi-Fi nhan mot ma rieng. Ma ghep noi trong QR chi dung de xin ma rieng.
/// Thu hoi mot thiet bi thi ma cua no het hieu luc ngay, ke ca ket noi dang mo, va no khong tu ghep lai
/// duoc bang ma QR cu dang luu (cookie cua no van bi nhan ra la da thu hoi).
/// </summary>
public sealed class LanDevices
{
    readonly string _path;
    readonly object _lock = new();
    List<LanDevice> _list = new();
    long _dirtyAt;
    readonly ConcurrentDictionary<CancellationTokenSource, string> _live = new();

    public LanDevices(string baseDir)
    {
        _path = Path.Combine(baseDir, "lan-devices.json");
        try { if (File.Exists(_path)) _list = JsonSerializer.Deserialize<List<LanDevice>>(File.ReadAllText(_path), Json.Opts) ?? new(); }
        catch (Exception ex) { Log.Write("Khong doc duoc lan-devices.json: " + ex.Message); }
    }

    static string H(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    LanDevice? Find(string? token)
    {
        if (string.IsNullOrEmpty(token) || token.Length < 20) return null;
        var h = H(token);
        return _list.FirstOrDefault(d => CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(d.Hash), Encoding.ASCII.GetBytes(h)));
    }

    /// <summary>Ma con hieu luc: tra ve thiet bi va cap nhat lan cuoi thay.</summary>
    public LanDevice? Check(string? token, string? ip)
    {
        lock (_lock)
        {
            var d = Find(token);
            if (d == null || d.Revoked) return null;
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (now - d.Seen > 30_000 || d.Ip != ip) { d.Seen = now; d.Ip = ip; MarkDirty(); }
            return d;
        }
    }

    public bool IsRevoked(string? token) { lock (_lock) return Find(token)?.Revoked == true; }

    /// <summary>Tao thiet bi moi, tra ve ma rieng (chi tra ve mot lan).</summary>
    public string Add(string? userAgent, string? ip)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lock (_lock)
        {
            _list.Add(new LanDevice { Id = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant(), Hash = H(token), Name = NameFrom(userAgent), Ip = ip, Created = now, Seen = now });
            Save();
        }
        Log.Write($"Ghep noi thiet bi Wi-Fi moi: {NameFrom(userAgent)} ({ip})");
        return token;
    }

    public List<LanDevice> List() { lock (_lock) return _list.Select(d => new LanDevice { Id = d.Id, Name = d.Name, Ip = d.Ip, Created = d.Created, Seen = d.Seen, Revoked = d.Revoked }).ToList(); }

    public bool Revoke(string id)
    {
        lock (_lock)
        {
            var d = _list.FirstOrDefault(x => x.Id == id);
            if (d == null) return false;
            d.Revoked = true;
            Save();
        }
        Disconnect(id);
        Log.Write("Thu hoi thiet bi Wi-Fi " + id);
        return true;
    }

    public bool Forget(string id)
    {
        lock (_lock)
        {
            if (_list.RemoveAll(x => x.Id == id) == 0) return false;
            Save();
        }
        Disconnect(id);
        return true;
    }

    public bool Rename(string id, string name)
    {
        name = name.Trim();
        if (name.Length == 0 || name.Length > 40) return false;
        lock (_lock)
        {
            var d = _list.FirstOrDefault(x => x.Id == id);
            if (d == null) return false;
            d.Name = name;
            Save();
        }
        return true;
    }

    /// <summary>Doi ma ghep noi: bo het thiet bi, moi dien thoai phai quet lai.</summary>
    public void Clear()
    {
        string[] ids;
        lock (_lock) { ids = _list.Select(d => d.Id).ToArray(); _list.Clear(); Save(); }
        foreach (var id in ids) Disconnect(id);
    }

    /// <summary>Gan ket noi WebSocket voi thiet bi de thu hoi thi ngat ngay.</summary>
    public CancellationTokenSource Track(string id, CancellationToken ct)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _live[cts] = id;
        return cts;
    }
    public void Untrack(CancellationTokenSource cts) { _live.TryRemove(cts, out _); cts.Dispose(); }

    void Disconnect(string id)
    {
        foreach (var (cts, dev) in _live)
            if (dev == id) try { cts.Cancel(); } catch { }
    }

    void MarkDirty()
    {
        // Ghi "lan cuoi thay" toi da moi phut mot lan
        var now = Environment.TickCount64;
        if (now - _dirtyAt < 60_000) return;
        _dirtyAt = now;
        Save();
    }

    void Save()
    {
        try
        {
            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(_list, Json.Opts));
            File.Move(tmp, _path, true);
        }
        catch (Exception ex) { Log.Write("Khong ghi duoc lan-devices.json: " + ex.Message); }
    }

    /// <summary>Ten de nhan ra thiet bi, lay tu User-Agent.</summary>
    public static string NameFrom(string? ua)
    {
        ua ??= "";
        if (ua.Contains("iPhone")) return "iPhone";
        if (ua.Contains("iPad") || (ua.Contains("Macintosh") && ua.Contains("Mobile"))) return "iPad";
        var m = System.Text.RegularExpressions.Regex.Match(ua, @"Android [\d.]+; ([^;)]+?)(?: Build/[^;)]*)?[;)]");
        if (m.Success && m.Groups[1].Value.Trim() is { Length: > 0 } model && model != "K") return "Android · " + model.Trim();
        if (ua.Contains("Android")) return "Android";
        if (ua.Contains("Windows")) return "Windows";
        if (ua.Contains("Macintosh")) return "Mac";
        if (ua.Contains("Linux")) return "Linux";
        return "?";
    }
}
