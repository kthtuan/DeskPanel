using LibreHardwareMonitor.Hardware;

namespace DeskPanel;

public sealed class Snapshot
{
    public long Time { get; set; }
    public int CfgVer { get; set; }
    public string? CpuName { get; set; }
    public double? CpuLoad { get; set; }
    public double? CpuTemp { get; set; }
    public double? CpuPower { get; set; }
    public double? CpuClock { get; set; }
    public string? GpuName { get; set; }
    public double? GpuLoad { get; set; }
    public double? GpuTemp { get; set; }
    public double? GpuMemUsed { get; set; }
    public double? GpuMemTotal { get; set; }
    public double? GpuPower { get; set; }
    public double? NetDown { get; set; }
    public double? NetUp { get; set; }
    public string? NetName { get; set; }
    public long Uptime { get; set; }
    public string? AppVer { get; set; }
    public MediaInfo? Media { get; set; }
    public WeatherInfo? Weather { get; set; }
    public AiInfo? Ai { get; set; }
    public string? Host { get; set; }
    public double? RamLoad { get; set; }
    public double? RamUsed { get; set; }
    public double? RamTotal { get; set; }
    public List<DiskInfo> Disks { get; set; } = new();
    public string? Note { get; set; }
    /// <summary>Ten tien trinh cua cua so dang dung tren PC (vd "acad"), de dien thoai tu chuyen trang.</summary>
    public string? Fg { get; set; }
}

public sealed class DiskInfo
{
    public string Name { get; set; } = "";
    public string? Letters { get; set; }
    /// <summary>Good / Caution / Bad / Unknown (theo SMART, giong CrystalDiskInfo)</summary>
    public string Status { get; set; } = "Unknown";
    public int? Temp { get; set; }
    public int? Life { get; set; }
    public double? UsedPct { get; set; }
    public double? SizeGb { get; set; }
    public ulong? PowerOnHours { get; set; }
    public ulong? WrittenGb { get; set; }
}

public interface ISensorSource : IDisposable
{
    /// <summary>Chi goi tu mot luong duy nhat.</summary>
    Snapshot Read();
    object DumpAll();
}

public static class SensorFactory
{
    public static ISensorSource Create()
    {
        if (OperatingSystem.IsWindows())
        {
            try { return new LhmSource(); }
            catch (Exception ex) { Log.Write("Khong mo duoc LibreHardwareMonitor: " + ex); }
        }
        return new FakeSource();
    }
}

sealed class LhmSource : ISensorSource
{
    readonly Computer _pc;
    readonly DiskReader _disks = new();

    public LhmSource()
    {
        _pc = new Computer { IsCpuEnabled = true, IsGpuEnabled = true, IsMemoryEnabled = true, IsNetworkEnabled = true };
        _pc.Open();
    }

    static IEnumerable<ISensor> AllSensors(IHardware hw) =>
        hw.Sensors.Concat(hw.SubHardware.SelectMany(AllSensors));

    static void UpdateAll(IHardware hw)
    {
        hw.Update();
        foreach (var s in hw.SubHardware) UpdateAll(s);
    }

    static double? Pick(IEnumerable<ISensor> sensors, SensorType type, params string[] names)
    {
        foreach (var n in names)
        {
            var s = sensors.FirstOrDefault(x => x.SensorType == type && x.Name == n && x.Value.HasValue);
            if (s != null) return Math.Round(s.Value!.Value, 1);
        }
        return null;
    }

    public Snapshot Read()
    {
        var snap = new Snapshot();
        IHardware? gpuBest = null;
        double gpuBestVram = -1;
        foreach (var hw in _pc.Hardware)
        {
            UpdateAll(hw);
            var ss = AllSensors(hw).ToList();
            switch (hw.HardwareType)
            {
                case HardwareType.Cpu:
                    snap.CpuName ??= hw.Name;
                    snap.CpuLoad ??= Pick(ss, SensorType.Load, "CPU Total");
                    snap.CpuTemp ??= Pick(ss, SensorType.Temperature, "CPU Package", "Core (Tctl/Tdie)", "Core (Tctl)", "Core Average", "Core Max");
                    snap.CpuPower ??= Pick(ss, SensorType.Power, "CPU Package", "Package");
                    var clocks = ss.Where(x => x.SensorType == SensorType.Clock && x.Name.StartsWith("Core #") && x.Value > 0).Select(x => (double)x.Value!.Value).ToList();
                    if (clocks.Count > 0) snap.CpuClock ??= Math.Round(clocks.Average());
                    break;
                case HardwareType.GpuNvidia:
                case HardwareType.GpuAmd:
                    // Neu co ca GPU tich hop va card roi: chon card co VRAM lon nhat
                    var vram = Pick(ss, SensorType.SmallData, "GPU Memory Total", "D3D Dedicated Memory Total") ?? 0;
                    if (gpuBest == null || vram > gpuBestVram) { gpuBest = hw; gpuBestVram = vram; }
                    break;
                case HardwareType.GpuIntel:
                    break;
                case HardwareType.Network:
                    // Chon card mang dang co luu luong lon nhat
                    var dn = Pick(ss, SensorType.Throughput, "Download Speed") ?? 0;
                    var up = Pick(ss, SensorType.Throughput, "Upload Speed") ?? 0;
                    if (snap.NetDown == null || dn + up > snap.NetDown + (snap.NetUp ?? 0))
                    { snap.NetDown = dn; snap.NetUp = up; snap.NetName = hw.Name; }
                    break;
                case HardwareType.Memory:
                    if (hw.Name.Contains("Virtual", StringComparison.OrdinalIgnoreCase)) break;
                    snap.RamLoad ??= Pick(ss, SensorType.Load, "Memory");
                    snap.RamUsed ??= Pick(ss, SensorType.Data, "Memory Used");
                    var avail = Pick(ss, SensorType.Data, "Memory Available");
                    if (snap.RamUsed != null && avail != null) snap.RamTotal ??= Math.Round(snap.RamUsed.Value + avail.Value, 1);
                    break;
            }
        }
        gpuBest ??= _pc.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuIntel);
        if (gpuBest != null)
        {
            var ss = AllSensors(gpuBest).ToList();
            snap.GpuName = gpuBest.Name;
            snap.GpuLoad = Pick(ss, SensorType.Load, "GPU Core", "D3D 3D");
            snap.GpuTemp = Pick(ss, SensorType.Temperature, "GPU Core", "GPU Hot Spot");
            snap.GpuMemUsed = Pick(ss, SensorType.SmallData, "GPU Memory Used", "D3D Dedicated Memory Used");
            snap.GpuMemTotal = Pick(ss, SensorType.SmallData, "GPU Memory Total", "D3D Dedicated Memory Total");
            snap.GpuPower = Pick(ss, SensorType.Power, "GPU Package", "GPU Power");
        }
        if (snap.CpuTemp == null)
            snap.Note = "Chưa đọc được nhiệt độ CPU: cần cài PawnIO và chạy DeskPanel bằng quyền admin.";
        snap.Disks = _disks.Read();
        snap.Uptime = Environment.TickCount64 / 1000;
        return snap;
    }

    public object DumpAll()
    {
        object Hw(IHardware h) => new
        {
            name = h.Name,
            type = h.HardwareType.ToString(),
            sensors = h.Sensors.Select(s => new { name = s.Name, type = s.SensorType.ToString(), value = s.Value }),
            sub = h.SubHardware.Select(Hw),
        };
        return new { hardware = _pc.Hardware.Select(Hw), disks = _disks.Read() };
    }

    public void Dispose()
    {
        try { _pc.Close(); } catch { }
        _disks.Dispose();
    }
}

/// <summary>SMART o cung qua DiskInfoToolkit (thu vien LibreHardwareMonitor dang dung). Doc 30 giay/lan.</summary>
sealed class DiskReader : IDisposable
{
    DateTime _last = DateTime.MinValue, _lastReload = DateTime.MinValue;
    List<DiskInfo> _cache = new();

    public List<DiskInfo> Read()
    {
        if ((DateTime.UtcNow - _last).TotalSeconds < 30) return _cache;
        _last = DateTime.UtcNow;
        var list = new List<DiskInfo>();
        try
        {
            // StorageManager.Storages rong cho toi khi goi ReloadStorages(); quet lai moi 10 phut
            if ((DateTime.UtcNow - _lastReload).TotalMinutes >= 10 || DiskInfoToolkit.StorageManager.Storages.Count == 0 && (DateTime.UtcNow - _lastReload).TotalSeconds >= 60)
            {
                _lastReload = DateTime.UtcNow;
                DiskInfoToolkit.StorageManager.ReloadStorages();
                Log.Write($"Tim thay {DiskInfoToolkit.StorageManager.Storages.Count} o dia");
            }
            foreach (var st in DiskInfoToolkit.StorageManager.Storages.ToList())
            {
                try { st.Update(); } catch (Exception ex) { Log.Write($"SMART {st.Model}: {ex.Message}"); }
                var sm = st.Smart;
                var d = new DiskInfo
                {
                    Name = (st.Model ?? "Ổ đĩa").Trim(),
                    Status = sm?.DiskStatus.ToString() ?? "Unknown",
                    Temp = sm?.Temperature,
                    Life = sm?.Life is sbyte l && l >= 0 ? l : null,
                    SizeGb = st.TotalSize > 0 ? Math.Round(st.TotalSize / 1e9) : null,
                    PowerOnHours = sm != null && sm.MeasuredPowerOnHours > 0 ? sm.MeasuredPowerOnHours : sm?.DetectedPowerOnHours,
                    WrittenGb = sm?.HostWrites,
                };
                if (st.TotalFreeSize is ulong free && st.TotalSize > 0)
                    d.UsedPct = Math.Round(100.0 * (st.TotalSize - Math.Min(free, st.TotalSize)) / st.TotalSize, 1);
                try
                {
                    var letters = st.Partitions?.Select(p => p.DriveLetter).Where(c => c != null && c != '\0').Select(c => c + ":").ToList();
                    if (letters is { Count: > 0 }) d.Letters = string.Join(" ", letters);
                }
                catch { }
                list.Add(d);
            }
        }
        catch (Exception ex)
        {
            Log.Write("Khong doc duoc o cung: " + ex.Message);
        }
        _cache = list;
        return list;
    }

    public void Dispose() { }
}

/// <summary>Du lieu gia de thu giao dien tren may khong phai Windows.</summary>
sealed class FakeSource : ISensorSource
{
    readonly DateTime _t0 = DateTime.UtcNow;

    public Snapshot Read()
    {
        double t = (DateTime.UtcNow - _t0).TotalSeconds;
        return new Snapshot
        {
            CpuName = "Demo CPU (dữ liệu giả)",
            CpuLoad = Math.Round(35 + 30 * Math.Sin(t / 4), 1),
            CpuTemp = Math.Round(55 + 15 * Math.Sin(t / 7), 1),
            CpuPower = Math.Round(45 + 20 * Math.Sin(t / 5), 1),
            CpuClock = 4200,
            GpuName = "Demo GPU",
            GpuLoad = Math.Round(50 + 40 * Math.Sin(t / 3), 1),
            GpuTemp = Math.Round(60 + 10 * Math.Sin(t / 6), 1),
            GpuMemUsed = 3120, GpuMemTotal = 8192, GpuPower = Math.Round(120 + 60 * Math.Sin(t / 3)),
            NetDown = Math.Round(Math.Max(0, 3e6 * Math.Sin(t / 2.5)) + 2e4), NetUp = Math.Round(Math.Max(0, 2e5 * Math.Sin(t / 4)) + 5e3), NetName = "Ethernet",
            Uptime = 3 * 86400 + 5 * 3600 + (long)t,
            RamLoad = 62.5, RamUsed = 19.9, RamTotal = 31.8,
            Disks = new()
            {
                new DiskInfo { Name = "Samsung SSD 980 1TB", Letters = "C:", Status = "Good", Temp = 41, Life = 96, UsedPct = 71.2, SizeGb = 1000, PowerOnHours = 6120, WrittenGb = 18400 },
                new DiskInfo { Name = "WDC WD20EZRZ", Letters = "D:", Status = "Caution", Temp = 38, UsedPct = 88.4, SizeGb = 2000, PowerOnHours = 31200 },
            },
            Note = "Đang chạy dữ liệu giả (không phải Windows).",
        };
    }

    public object DumpAll() => Read();
    public void Dispose() { }
}

/// <summary>Phan mem dang o cua so truoc tren PC.</summary>
public static class Foreground
{
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    static uint _pid; static string? _name;
    static readonly string? FakeFile = Environment.GetEnvironmentVariable("DESKPANEL_FAKE_FG_FILE");

    public static string? Name()
    {
        if (FakeFile != null) { try { return File.Exists(FakeFile) ? File.ReadAllText(FakeFile).Trim().ToLowerInvariant() : null; } catch { return null; } }
        if (!OperatingSystem.IsWindows()) return null;
        try
        {
            var h = GetForegroundWindow();
            if (h == IntPtr.Zero) return _name;
            GetWindowThreadProcessId(h, out var pid);
            if (pid == 0) return _name;
            if (pid != _pid) { _pid = pid; using var p = System.Diagnostics.Process.GetProcessById((int)pid); _name = p.ProcessName.ToLowerInvariant(); }
            return _name;
        }
        catch { return _name; }
    }
}
