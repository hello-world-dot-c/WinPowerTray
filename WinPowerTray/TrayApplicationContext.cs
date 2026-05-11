using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace WinPowerTray;

/// <summary>
/// Owns the <see cref="NotifyIcon"/> and its context menu.
/// Drives the entire lifetime of the tray application.
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    private const string ProjectUrl = "https://github.com/hello-world-dot-c/WinPowerTray";

    private static readonly string AppVersion =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";

    private readonly NotifyIcon          _trayIcon;
    private readonly ContextMenuStrip    _menu;
    private readonly ToolStripMenuItem   _itemEfficiency;
    private readonly ToolStripMenuItem   _itemBalanced;
    private readonly ToolStripMenuItem   _itemPerformance;
    private readonly ToolStripMenuItem   _itemStyleStandard;
    private readonly ToolStripMenuItem   _itemStyleRecommended;
    private readonly ToolStripMenuItem   _itemShowNotification;
    private readonly System.Windows.Forms.Timer _pollTimer;

    // Track the icon we last gave to NotifyIcon so we can dispose it.
    private Icon?      _currentIcon;
    private PowerMode  _lastKnownMode;

    public TrayApplicationContext()
    {
        // --- Title bar ----------------------------------------------------------
        // Non-interactive label at the top of the menu showing the app name + version.
        var itemTitle = new ToolStripLabel($"WinPowerTray v{AppVersion}")
        {
            Font      = new Font(SystemFonts.MenuFont!, FontStyle.Bold),
            ForeColor = SystemColors.GrayText,
            Enabled   = false
        };

        // --- Power mode items ---------------------------------------------------
        _itemEfficiency  = new ToolStripMenuItem($"🍃  {PowerModeManager.Label(PowerMode.BestEfficiency)}",  null, OnEfficiencyClick);
        _itemBalanced    = new ToolStripMenuItem($"⚖️  {PowerModeManager.Label(PowerMode.Balanced)}",         null, OnBalancedClick);
        _itemPerformance = new ToolStripMenuItem($"⚡  {PowerModeManager.Label(PowerMode.BestPerformance)}",  null, OnPerformanceClick);

        // Behave like radio buttons (show tick on the active one)
        _itemEfficiency .CheckOnClick = false;
        _itemBalanced   .CheckOnClick = false;
        _itemPerformance.CheckOnClick = false;

        // --- Settings submenu ---------------------------------------------------
        _itemStyleStandard    = new ToolStripMenuItem(LabelsFor(LabelStyle.Standard),    null, OnStyleStandardClick);
        _itemStyleRecommended = new ToolStripMenuItem(LabelsFor(LabelStyle.Recommended), null, OnStyleRecommendedClick);
        var itemLabelStyle    = new ToolStripMenuItem("Label style");
        itemLabelStyle.DropDownItems.AddRange(new ToolStripItem[]
        {
            _itemStyleStandard,
            _itemStyleRecommended
        });

        _itemShowNotification = new ToolStripMenuItem("Show power mode change notification", null, OnToggleNotificationClick)
        {
            CheckOnClick = true,
            Checked      = Settings.ShowChangeNotification
        };

        var itemSettings = new ToolStripMenuItem("Settings");
        itemSettings.DropDownItems.AddRange(new ToolStripItem[]
        {
            itemLabelStyle,
            new ToolStripSeparator(),
            _itemShowNotification
        });

        // --- Footer items -------------------------------------------------------
        var itemProjectPage = new ToolStripMenuItem("Go to project page", null, OnProjectPageClick);
        var itemExit        = new ToolStripMenuItem("Exit", null, OnExitClick);

        // --- Assemble menu ------------------------------------------------------
        // ShowImageMargin must stay true (default) so checkmarks render in the left column.
        _menu = new ContextMenuStrip();
        _menu.Items.AddRange(new ToolStripItem[]
        {
            itemTitle,
            new ToolStripSeparator(),
            _itemEfficiency,
            _itemBalanced,
            _itemPerformance,
            new ToolStripSeparator(),
            itemSettings,
            new ToolStripSeparator(),
            itemProjectPage,
            itemExit
        });

        // Refresh checked state whenever the menu opens so it always reflects
        // changes made outside the app (e.g. via Settings or powercfg).
        _menu.Opening += (_, _) => RefreshMenuState();

        // --- Build tray icon ----------------------------------------------------
        _trayIcon = new NotifyIcon
        {
            ContextMenuStrip = _menu,
            Visible          = true
        };

        // Show the menu on left-click as well as right-click.
        _trayIcon.MouseClick += OnTrayMouseClick;

        // Initial state (also seeds _lastKnownMode)
        RefreshMenuState();
        RefreshLabelStyleChecks();

        // --- Poll for external mode changes -------------------------------------
        _pollTimer = new System.Windows.Forms.Timer { Interval = 5000 };
        _pollTimer.Tick += OnPollTick;
        _pollTimer.Start();
    }

    // -------------------------------------------------------------------------
    // Mode switching
    // -------------------------------------------------------------------------

    private void ApplyMode(PowerMode mode)
    {
        if (!PowerModeManager.SetMode(mode))
        {
            MessageBox.Show(
                "Could not change the power mode.\n\n" +
                "This is unexpected — the power overlay API should not require elevation.\n" +
                "Try running WinPowerTray as Administrator if the problem persists.",
                "WinPowerTray",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        RefreshMenuState();
        NotifyModeChange($"Switched to {PowerModeManager.Label(mode)}");
    }

    private void NotifyModeChange(string text)
    {
        if (!Settings.ShowChangeNotification) return;

        _trayIcon.ShowBalloonTip(
            timeout:  2000,
            tipTitle: "WinPowerTray",
            tipText:  text,
            tipIcon:  ToolTipIcon.Info);
    }

    private void RefreshMenuState()
    {
        PowerMode current = PowerModeManager.GetCurrentMode();
        _lastKnownMode = current;

        _itemEfficiency .Checked = current == PowerMode.BestEfficiency;
        _itemBalanced   .Checked = current == PowerMode.Balanced;
        _itemPerformance.Checked = current == PowerMode.BestPerformance;

        // Swap the tray icon colour to match the new mode
        Icon newIcon  = IconHelper.Create(current);
        Icon? oldIcon = _currentIcon;

        _trayIcon.Icon    = newIcon;
        _trayIcon.Text    = $"Power mode: {PowerModeManager.Label(current)}";
        _currentIcon      = newIcon;

        // Dispose previous icon *after* assigning the new one
        oldIcon?.Dispose();
    }

    private void OnPollTick(object? s, EventArgs e)
    {
        PowerMode current = PowerModeManager.GetCurrentMode();
        if (current == _lastKnownMode) return;

        // Mode was changed externally (Settings, powercfg, another app…)
        RefreshMenuState();
        NotifyModeChange($"Power mode changed to {PowerModeManager.Label(current)}");
    }

    // -------------------------------------------------------------------------
    // Label style
    // -------------------------------------------------------------------------

    /// <summary>Returns a "A / B / C" preview of the labels used by the given style.</summary>
    private static string LabelsFor(LabelStyle style)
    {
        LabelStyle previous = Settings.LabelStyle;
        Settings.LabelStyle = style;
        try
        {
            return $"{PowerModeManager.Label(PowerMode.BestEfficiency)} / " +
                   $"{PowerModeManager.Label(PowerMode.Balanced)} / " +
                   $"{PowerModeManager.Label(PowerMode.BestPerformance)}";
        }
        finally
        {
            Settings.LabelStyle = previous;
        }
    }

    private void ApplyLabelStyle(LabelStyle style)
    {
        if (Settings.LabelStyle == style) return;
        Settings.LabelStyle = style;
        Settings.Save();

        // Push the new labels into every place they appear.
        _itemEfficiency .Text = $"🍃  {PowerModeManager.Label(PowerMode.BestEfficiency)}";
        _itemBalanced   .Text = $"⚖️  {PowerModeManager.Label(PowerMode.Balanced)}";
        _itemPerformance.Text = $"⚡  {PowerModeManager.Label(PowerMode.BestPerformance)}";
        _trayIcon.Text        = $"Power mode: {PowerModeManager.Label(_lastKnownMode)}";

        RefreshLabelStyleChecks();
    }

    private void RefreshLabelStyleChecks()
    {
        _itemStyleStandard   .Checked = Settings.LabelStyle == LabelStyle.Standard;
        _itemStyleRecommended.Checked = Settings.LabelStyle == LabelStyle.Recommended;
    }

    // -------------------------------------------------------------------------
    // Event handlers
    // -------------------------------------------------------------------------

    private void OnStyleStandardClick   (object? s, EventArgs e) => ApplyLabelStyle(LabelStyle.Standard);
    private void OnStyleRecommendedClick(object? s, EventArgs e) => ApplyLabelStyle(LabelStyle.Recommended);

    private void OnToggleNotificationClick(object? s, EventArgs e)
    {
        // CheckOnClick already flipped the visual state; mirror it into settings.
        Settings.ShowChangeNotification = _itemShowNotification.Checked;
        Settings.Save();
    }

    private void OnTrayMouseClick(object? s, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;

        // NotifyIcon only shows ContextMenuStrip automatically on right-click.
        // Call the private ShowContextMenu method to replicate that behaviour for
        // left-click, keeping positioning and focus-loss dismissal correct.
        typeof(NotifyIcon)
            .GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(_trayIcon, null);
    }

    private void OnEfficiencyClick (object? s, EventArgs e) => ApplyMode(PowerMode.BestEfficiency);
    private void OnBalancedClick   (object? s, EventArgs e) => ApplyMode(PowerMode.Balanced);
    private void OnPerformanceClick(object? s, EventArgs e) => ApplyMode(PowerMode.BestPerformance);

    private void OnProjectPageClick(object? s, EventArgs e)
    {
        Process.Start(new ProcessStartInfo { FileName = ProjectUrl, UseShellExecute = true });
    }

    private void OnExitClick(object? s, EventArgs e)
    {
        _trayIcon.Visible = false;
        Application.Exit();
    }

    // -------------------------------------------------------------------------
    // Cleanup
    // -------------------------------------------------------------------------

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pollTimer.Stop();
            _pollTimer.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _menu.Dispose();
            _currentIcon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
