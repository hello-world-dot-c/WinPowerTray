using System;
using System.Runtime.InteropServices;

namespace WinPowerTray;

/// <summary>
/// The three Windows 11 power-mode overlays exposed in Settings → System → Power → Power mode.
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
    // Windows 11 overlay GUIDs
    //   Best power efficiency : 961cc777-2547-4f9d-8174-7d86181b8a7a
    //   Balanced              : 00000000-0000-0000-0000-000000000000  (no overlay)
    //   Best performance      : ded574b5-45a0-4f42-8737-46345c09c238
    // These match the overlays used by Settings and powercfg /overlaysetactive.
    // -------------------------------------------------------------------------
    private static readonly Guid GuidEfficiency  = new("961cc777-2547-4f9d-8174-7d86181b8a7a");
    private static readonly Guid GuidBalanced     = Guid.Empty;
    private static readonly Guid GuidPerformance  = new("ded574b5-45a0-4f42-8737-46345c09c238");

    // powrprof.dll exports
    [DllImport("powrprof.dll", ExactSpelling = true)]
    private static extern uint PowerSetActiveOverlayScheme(Guid OverlaySchemeGuid);

    [DllImport("powrprof.dll", ExactSpelling = true)]
    private static extern uint PowerGetActualOverlayScheme(out Guid ActualOverlaySchemeGuid);

    /// <summary>Returns the currently active power mode, defaulting to Balanced on any error.</summary>
    public static PowerMode GetCurrentMode()
    {
        uint hr = PowerGetActualOverlayScheme(out Guid current);
        if (hr != 0)
            return PowerMode.Balanced;

        if (current == GuidEfficiency)  return PowerMode.BestEfficiency;
        if (current == GuidPerformance) return PowerMode.BestPerformance;
        return PowerMode.Balanced;
    }

    /// <summary>
    /// Activates the requested power mode.
    /// Returns <c>true</c> on success, <c>false</c> if the API call fails.
    /// </summary>
    public static bool SetMode(PowerMode mode)
    {
        Guid target = mode switch
        {
            PowerMode.BestEfficiency  => GuidEfficiency,
            PowerMode.BestPerformance => GuidPerformance,
            _                         => GuidBalanced
        };

        uint hr = PowerSetActiveOverlayScheme(target);
        return hr == 0;
    }

    /// <summary>Human-readable label for a power mode.</summary>
    public static string Label(PowerMode mode) => mode switch
    {
        PowerMode.BestEfficiency  => "Best power efficiency",
        PowerMode.BestPerformance => "Best performance",
        _                         => "Balanced"
    };
}
