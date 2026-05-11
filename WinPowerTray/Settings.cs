using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinPowerTray;

/// <summary>
/// Selects which user-facing labels are shown for the three power modes.
/// The underlying overlay GUIDs are identical regardless of style.
/// </summary>
public enum LabelStyle
{
    /// <summary>"Best power efficiency" / "Balanced" / "Best performance" — the default on most desktops.</summary>
    Standard,
    /// <summary>"Recommended" / "Better Performance" / "Best Performance" — used by Settings on some laptops (e.g. Intel Evo).</summary>
    Recommended
}

/// <summary>
/// Persistent per-user settings, stored as JSON in %AppData%\WinPowerTray\settings.json.
/// </summary>
public static class Settings
{
    private static readonly string s_dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinPowerTray");
    private static readonly string s_path = Path.Combine(s_dir, "settings.json");

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static LabelStyle LabelStyle { get; set; } = LabelStyle.Standard;

    /// <summary>Whether to show a toast when the power mode changes (via the tray or externally).</summary>
    public static bool ShowChangeNotification { get; set; } = true;

    static Settings() => Load();

    private sealed class Persisted
    {
        public LabelStyle LabelStyle { get; set; } = LabelStyle.Standard;
        public bool ShowChangeNotification { get; set; } = true;
    }

    public static void Load()
    {
        try
        {
            if (!File.Exists(s_path)) return;
            var data = JsonSerializer.Deserialize<Persisted>(File.ReadAllText(s_path), s_jsonOptions);
            if (data == null) return;
            LabelStyle             = data.LabelStyle;
            ShowChangeNotification = data.ShowChangeNotification;
        }
        catch
        {
            // On any read/parse error keep defaults — settings are non-critical.
        }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(s_dir);
            var data = new Persisted
            {
                LabelStyle             = LabelStyle,
                ShowChangeNotification = ShowChangeNotification
            };
            File.WriteAllText(s_path, JsonSerializer.Serialize(data, s_jsonOptions));
        }
        catch
        {
            // Best-effort: a failed save shouldn't bring down the tray app.
        }
    }
}
