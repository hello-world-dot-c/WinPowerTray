using System;
using System.Runtime.InteropServices;

namespace WinPowerTray;

/// <summary>
/// The three positions on the Windows 11 power-mode slider, in order from
/// most efficient to most performant.  The actual overlay GUID each position
/// maps to depends on <see cref="Settings.LabelStyle"/> — different machines
/// use different GUID sets for the same three slider positions.
/// </summary>
public enum PowerMode
{
    BestEfficiency,
    Balanced,
    BestPerformance
}

/// <summary>
/// Reads and sets the active Windows 11 power-mode overlay via powrprof.dll.
/// No administrator rights are required; the API operates on the current user session.
/// </summary>
public static class PowerModeManager
{
    // -------------------------------------------------------------------------
    // Windows power-mode overlay GUIDs
    //
    // The slider positions and which GUID activates them depend on the device:
    //
    //   "Standard" devices (most desktops):
    //     low    "Best power efficiency"   961cc777-2547-4f9d-8174-7d86181b8a7a
    //     middle "Balanced"                00000000-0000-0000-0000-000000000000  (no overlay)
    //     high   "Best performance"        ded574b5-45a0-4f42-8737-46345c09c238
    //
    //   "Recommended" devices (some laptops, e.g. Intel Evo):
    //     low    "Recommended"             00000000-0000-0000-0000-000000000000  (no overlay)
    //     middle "Better Performance"      3af9b8d9-7c97-431d-ad78-34a8bfea439f  (legacy Win10 overlay)
    //     high   "Best Performance"        ded574b5-45a0-4f42-8737-46345c09c238
    //
    // The label style is user-selectable in the tray menu and persisted in Settings.
    // -------------------------------------------------------------------------
    private static readonly Guid GuidEfficiency       = new("961cc777-2547-4f9d-8174-7d86181b8a7a");
    private static readonly Guid GuidNone             = Guid.Empty;
    private static readonly Guid GuidBetterPerformance = new("3af9b8d9-7c97-431d-ad78-34a8bfea439f");
    private static readonly Guid GuidBestPerformance   = new("ded574b5-45a0-4f42-8737-46345c09c238");

    // powrprof.dll exports
    [DllImport("powrprof.dll", ExactSpelling = true)]
    private static extern uint PowerSetActiveOverlayScheme(Guid OverlaySchemeGuid);

    [DllImport("powrprof.dll", ExactSpelling = true)]
    private static extern uint PowerGetActualOverlayScheme(out Guid ActualOverlaySchemeGuid);

    /// <summary>Maps a slider position to the overlay GUID for the current label style.</summary>
    private static Guid GuidFor(PowerMode mode) => (Settings.LabelStyle, mode) switch
    {
        (LabelStyle.Recommended, PowerMode.BestEfficiency)  => GuidNone,              // "Recommended"
        (LabelStyle.Recommended, PowerMode.Balanced)        => GuidBetterPerformance, // "Better Performance"
        (LabelStyle.Recommended, PowerMode.BestPerformance) => GuidBestPerformance,   // "Best Performance"
        (_,                      PowerMode.BestEfficiency)  => GuidEfficiency,        // "Best power efficiency"
        (_,                      PowerMode.Balanced)        => GuidNone,              // "Balanced"
        _                                                   => GuidBestPerformance,   // "Best performance"
    };

    /// <summary>Returns the currently active power mode, defaulting to Balanced on any error.</summary>
    public static PowerMode GetCurrentMode()
    {
        if (PowerGetActualOverlayScheme(out Guid current) != 0)
            return PowerMode.Balanced;

        if (Settings.LabelStyle == LabelStyle.Recommended)
        {
            if (current == GuidBestPerformance)   return PowerMode.BestPerformance;
            if (current == GuidBetterPerformance) return PowerMode.Balanced;
            return PowerMode.BestEfficiency; // GuidNone (or unknown) → "Recommended"
        }

        if (current == GuidEfficiency)      return PowerMode.BestEfficiency;
        if (current == GuidBestPerformance) return PowerMode.BestPerformance;
        return PowerMode.Balanced;
    }

    /// <summary>
    /// Activates the requested power mode.
    /// Returns <c>true</c> on success, <c>false</c> if the API call fails.
    /// </summary>
    public static bool SetMode(PowerMode mode) => PowerSetActiveOverlayScheme(GuidFor(mode)) == 0;

    /// <summary>Human-readable label for a power mode, using the current <see cref="Settings.LabelStyle"/>.</summary>
    public static string Label(PowerMode mode) => (Settings.LabelStyle, mode) switch
    {
        (LabelStyle.Recommended, PowerMode.BestEfficiency)  => "Recommended",
        (LabelStyle.Recommended, PowerMode.Balanced)        => "Better Performance",
        (LabelStyle.Recommended, PowerMode.BestPerformance) => "Best Performance",
        (_,                      PowerMode.BestEfficiency)  => "Best power efficiency",
        (_,                      PowerMode.BestPerformance) => "Best performance",
        _                                                   => "Balanced"
    };
}
