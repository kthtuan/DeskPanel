namespace DeskPanel;

public static class Defaults
{
    static ButtonDef B(string id, string group, string label, string icon, string type, string value, string? color = null, string? focus = null, string? args = null)
        => new() { Id = id, Group = group, Label = label, Icon = icon, Type = type, Value = value, Color = color, Focus = focus, Args = args };

    /// <summary>Ngon ngu mac dinh cho may moi cai: Windows tieng Viet thi "vi", con lai "en".</summary>
    public static string SystemLang => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "vi" ? "vi" : "en";

    public static PanelConfig Create() => new()
    {
        Lang = SystemLang,
        Buttons = new()
        {
            B("calc", "Chung", "Máy tính", "🧮", "run", @"%SystemRoot%\System32\calc.exe", "#2f6fdf"),
            B("explorer", "Chung", "Thư mục", "📁", "keys", "Win+E", "#c58a1a"),
            B("taskmgr", "Chung", "Task Mgr", "📊", "keys", "Ctrl+Shift+Esc", "#6b4fd8"),
            B("snip", "Chung", "Chụp MH", "✂️", "keys", "Win+Shift+S", "#0f8f7a"),
            B("desktop", "Chung", "Màn hình nền", "🖥️", "keys", "Win+D", "#4a5568"),
            B("playpause", "Chung", "Phát/Dừng", "⏯️", "keys", "MediaPlayPause", "#b83280"),
            B("voldown", "Chung", "Giảm âm", "🔉", "keys", "VolumeDown", "#4a5568"),
            B("volup", "Chung", "Tăng âm", "🔊", "keys", "VolumeUp", "#4a5568"),
            B("mute", "Chung", "Tắt tiếng", "🔇", "keys", "VolumeMute", "#4a5568"),

            B("cad-line", "AutoCAD", "LINE", "📏", "cad", "L", "#c53030"),
            B("cad-pline", "AutoCAD", "PLINE", "〰️", "cad", "PL", "#c53030"),
            B("cad-circle", "AutoCAD", "CIRCLE", "⭕", "cad", "C", "#c53030"),
            B("cad-trim", "AutoCAD", "TRIM", "✂️", "cad", "TR", "#dd6b20"),
            B("cad-extend", "AutoCAD", "EXTEND", "↔️", "cad", "EX", "#dd6b20"),
            B("cad-offset", "AutoCAD", "OFFSET", "⧉", "cad", "O", "#dd6b20"),
            B("cad-move", "AutoCAD", "MOVE", "✥", "cad", "M", "#2b6cb0"),
            B("cad-copy", "AutoCAD", "COPY", "📄", "cad", "CO", "#2b6cb0"),
            B("cad-rotate", "AutoCAD", "ROTATE", "🔄", "cad", "RO", "#2b6cb0"),
            B("cad-mirror", "AutoCAD", "MIRROR", "🪞", "cad", "MI", "#2b6cb0"),
            B("cad-fillet", "AutoCAD", "FILLET", "◜", "cad", "F", "#2b6cb0"),
            B("cad-dim", "AutoCAD", "DIM", "📐", "cad", "DLI", "#2f855a"),
            B("cad-zoome", "AutoCAD", "ZOOM E", "🔍", "cad", "Z E", "#4a5568"),
            B("cad-regen", "AutoCAD", "REGEN", "♻️", "cad", "RE", "#4a5568"),
            B("cad-undo", "AutoCAD", "Hoàn tác", "↩️", "keys", "Ctrl+Z", "#4a5568", "acad,acadlt"),
            B("cad-esc", "AutoCAD", "Esc", "⎋", "keys", "Esc, Esc", "#4a5568", "acad,acadlt"),
            B("cad-save", "AutoCAD", "Lưu", "💾", "keys", "Ctrl+S", "#2f855a", "acad,acadlt"),
        },
    };
}
