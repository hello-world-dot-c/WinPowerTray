using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinPowerTray;

/// <summary>
/// Owns the <see cref="NotifyIcon"/> and its right-click context menu.
/// Drives the entire lifetime of the tray application.
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon          _trayIcon;
    private readonly ContextMenuStrip    _menu;
    private readonly ToolStripMenuItem   _itemEfficiency;
    private readonly ToolStripMenuItem   _itemBalanced;
    private readonly ToolStripMenuItem   _itemPerformance;

    // Track the icon we last gave to NotifyIcon so we can dispose it.
    private Icon? _currentIcon;

    public TrayApplicationContext()
    {
        // --- Build menu items ---------------------------------------------------
        _itemEfficiency  = new ToolStripMenuItem("🍃  Best power efficiency",  null, OnEfficiencyClick);
        _itemBalanced    = new ToolStripMenuItem("⚖️  Balanced",               null, OnBalancedClick);
        _itemPerformance = new ToolStripMenuItem("⚡  Best performance",        null, OnPerformanceClick);

        // Make items behave like radio buttons (show tick on the active one)
        _itemEfficiency .CheckOnClick = false;
        _itemBalanced   .CheckOnClick = false;
        _itemPerformance.CheckOnClick = false;

        var separator = new ToolStripSeparator();
        var itemExit  = new ToolStripMenuItem("Exit", null, OnExitClick);

        // ShowImageMargin must stay true (default) so checkmarks render in the left column.
        _menu = new ContextMenuStrip();
        _menu.Items.AddRange(new ToolStripItem[]
        {
            _itemEfficiency,
            _itemBalanced,
            _itemPerformance,
            separator,
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

        // Initial state
        RefreshMenuState();
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
    }

    private void RefreshMenuState()
    {
        PowerMode current = PowerModeManager.GetCurrentMode();

        _itemEfficiency .Checked = current == PowerMode.BestEfficiency;
        _itemBalanced   .Checked = current == PowerMode.Balanced;
        _itemPerformance.Checked = current == PowerMode.BestPerformance;

        // Swap the tray icon colour to match the new mode
        Icon newIcon = IconHelper.Create(current);
        Icon? oldIcon = _currentIcon;

        _trayIcon.Icon    = newIcon;
        _trayIcon.Text    = $"Power mode: {PowerModeManager.Label(current)}";
        _currentIcon      = newIcon;

        // Dispose previous icon *after* assigning the new one
        oldIcon?.Dispose();
    }

    // -------------------------------------------------------------------------
    // Event handlers
    // -------------------------------------------------------------------------

    private void OnEfficiencyClick (object? s, EventArgs e) => ApplyMode(PowerMode.BestEfficiency);
    private void OnBalancedClick   (object? s, EventArgs e) => ApplyMode(PowerMode.Balanced);
    private void OnPerformanceClick(object? s, EventArgs e) => ApplyMode(PowerMode.BestPerformance);

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
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _menu.Dispose();
            _currentIcon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
