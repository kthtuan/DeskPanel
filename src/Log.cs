namespace DeskPanel;

public static class Log
{
    static readonly object Lock = new();
    static string _path = Path.Combine(AppContext.BaseDirectory, "deskpanel.log");

    public static void Write(string msg)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {msg}";
        Console.WriteLine(line);
        lock (Lock)
        {
            try
            {
                var fi = new FileInfo(_path);
                if (fi.Exists && fi.Length > 1_000_000) File.Move(_path, _path + ".old", true);
                File.AppendAllText(_path, line + Environment.NewLine);
            }
            catch { }
        }
    }
}
