using System.IO;
using System.Text.Json;
using System.Windows.Input;
namespace Timerki;
public sealed class Settings
{
    public int[] Durations { get; set; } = [600,600,600];
    public int[] Hotkeys { get; set; } = [(int)Key.F6,(int)Key.F7,(int)Key.F8];
    public string[] AudioFiles { get; set; } = ["","",""];
    public bool AlwaysOnTop { get; set; } = true;
    public bool ClickThrough { get; set; } = false;
    public bool AutoStart { get; set; } = false;
    public double? MiniLeft { get; set; }
    public double? MiniTop { get; set; }
    // All configuration and audio are stored alongside Timerki.exe (portable mode).
    public static string BaseFolder => AppContext.BaseDirectory;
    public static string SoundsFolder => Path.Combine(BaseFolder, "Sounds");
    public static string FilePath => Path.Combine(BaseFolder, "settings.json");
    public static string ResolveAudioPath(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored)) return "";
        // Never depend on an absolute location outside the portable application.
        var name = Path.GetFileName(stored);
        return Path.Combine(SoundsFolder, name);
    }
    public static string ImportAudio(int index, string source)
    {
        Directory.CreateDirectory(SoundsFolder);
        string filename = $"timer{index + 1}.wav";
        string target = Path.Combine(SoundsFolder, filename);
        if (!string.Equals(Path.GetFullPath(source), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
            File.Copy(source, target, overwrite: true);
        return Path.Combine("Sounds", filename);
    }
    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath)) return Sanitize(JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new());
            // One-time migration from earlier releases using %APPDATA%.
            var oldPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Timerki", "settings.json");
            if (File.Exists(oldPath))
            {
                var old = Sanitize(JsonSerializer.Deserialize<Settings>(File.ReadAllText(oldPath)) ?? new());
                for (int i = 0; i < 3; i++)
                {
                    string previous = old.AudioFiles[i];
                    if (File.Exists(previous)) old.AudioFiles[i] = ImportAudio(i, previous);
                    else old.AudioFiles[i] = "";
                }
                old.Save();
                return old;
            }
        }
        catch { /* defaults */ }
        return new();
    }
    public static Settings Sanitize(Settings s)
    {
        if (s.Durations?.Length != 3) s.Durations = [600,600,600];
        if (s.Hotkeys?.Length != 3) s.Hotkeys = [(int)Key.F6,(int)Key.F7,(int)Key.F8];
        if (s.AudioFiles?.Length != 3) s.AudioFiles = ["","",""];
        for(int i=0;i<3;i++)
        {
            s.Durations[i] = Math.Clamp(s.Durations[i], 1, 86400);
            if (!Enum.IsDefined(typeof(Key), s.Hotkeys[i])) s.Hotkeys[i] = (int)Key.F6+i;
            s.AudioFiles[i] ??= "";
        }
        return s;
    }
    public void Save()
    {
        Directory.CreateDirectory(BaseFolder);
        Directory.CreateDirectory(SoundsFolder);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
