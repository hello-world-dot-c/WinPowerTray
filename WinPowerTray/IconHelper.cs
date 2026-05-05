using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace WinPowerTray;

/// <summary>
/// Generates a 16 × 16 tray icon at runtime — no embedded .ico file needed.
/// Each power mode gets a distinct background colour and a white lightning-bolt glyph.
///   BestEfficiency  → green  (#4CAF50)
///   Balanced        → blue   (#2196F3)
///   BestPerformance → red    (#F44336)
/// </summary>
public static class IconHelper
{
    private static readonly Color ColourEfficiency  = Color.FromArgb(0x4C, 0xAF, 0x50);
    private static readonly Color ColourBalanced    = Color.FromArgb(0x21, 0x96, 0xF3);
    private static readonly Color ColourPerformance = Color.FromArgb(0xF4, 0x43, 0x36);

    /// <summary>Creates (and returns) a new <see cref="Icon"/> for <paramref name="mode"/>.</summary>
    /// <remarks>
    /// The caller owns the returned icon and should dispose it when it is replaced.
    /// </remarks>
    public static Icon Create(PowerMode mode)
    {
        Color bg = mode switch
        {
            PowerMode.BestEfficiency  => ColourEfficiency,
            PowerMode.BestPerformance => ColourPerformance,
            _                         => ColourBalanced
        };

        // 32 × 32 internally for quality, then we let Windows scale it down.
        // Using 32 px internally keeps the bolt crisp before icon handles are created.
        const int size = 32;

        using var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode      = SmoothingMode.AntiAlias;
            g.InterpolationMode  = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode    = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            // Filled circle (the badge background)
            using (var brush = new SolidBrush(bg))
                g.FillEllipse(brush, 1, 1, size - 2, size - 2);

            // White lightning bolt centred in the circle.
            // Points are tuned for a 32 × 32 canvas.
            PointF[] bolt =
            [
                new(20f,  3f),   // top-right of upper segment
                new(12f, 15f),   // mid-left
                new(17f, 15f),   // mid-right (inner)
                new(12f, 29f),   // bottom tip
                new(20f, 17f),   // lower-right
                new(15f, 17f),   // lower-left (inner)
            ];

            using var boltBrush = new SolidBrush(Color.White);
            g.FillPolygon(boltBrush, bolt);
        }

        // Convert Bitmap → Icon.
        // Icon.FromHandle wraps an HICON but does NOT own it, so Clone() is required
        // to produce a managed Icon that disposes cleanly, then we free the raw HICON.
        IntPtr hIcon = bmp.GetHicon();
        Icon icon = (Icon)Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);   // release the HICON we got from GetHicon()
        return icon;
    }

    // Needed to avoid a GDI handle leak from Bitmap.GetHicon()
    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
