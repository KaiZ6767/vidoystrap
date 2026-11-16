using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace Bloxstrap.UI.Utility
{
    /// <summary>
    /// One selectable accent colour. <see cref="Hex"/> is what gets persisted; an empty value means
    /// "follow the Windows accent", which is what the app did before personalization existed.
    /// </summary>
    public class AccentSwatch
    {
        public string Name { get; init; } = "";
        public string Hex { get; init; } = "";
        public string Label => string.IsNullOrEmpty(Hex) ? Name : $"{Name}  {Hex}";

        public Brush Brush
        {
            get
            {
                if (!Branding.TryParseHex(Hex, out Color color))
                    return Brushes.Transparent;

                var brush = new SolidColorBrush(color);
                brush.Freeze();
                return brush;
            }
        }
    }

    /// <summary>
    /// Applies the personalization settings: accent colour and backdrop visibility.
    /// Raises <see cref="Changed"/> so already-open windows can repaint without a restart.
    /// </summary>
    public static class Branding
    {
        public const string DefaultAccent = "#FF1336";

        public static event EventHandler? Changed;

        public static IReadOnlyList<AccentSwatch> Palette { get; } = new List<AccentSwatch>
        {
            new() { Name = "Sistema",          Hex = "" },
            new() { Name = "Rojo neón",        Hex = "#FF1336" },
            new() { Name = "Rojo",             Hex = "#E2262C" },
            new() { Name = "Violeta neón",     Hex = "#B026FF" },
            new() { Name = "Violeta",          Hex = "#7C5CFF" },
            new() { Name = "Verde neón",       Hex = "#39FF14" },
            new() { Name = "Verde",            Hex = "#21C55D" },
            new() { Name = "Amarillo neón",    Hex = "#FFE600" },
            new() { Name = "Amarillo",         Hex = "#FFB300" },
            new() { Name = "Naranja",          Hex = "#FF7A18" },
            new() { Name = "Cian neón",        Hex = "#22E8FF" },
            new() { Name = "Azul",             Hex = "#3B82F6" },
            new() { Name = "Rosa",             Hex = "#FF3DA5" },
            new() { Name = "Magenta",          Hex = "#FF00D4" },
            new() { Name = "Blanco",           Hex = "#F2F2F2" },
        };

        public static AccentSwatch FindSwatch(string hex)
        {
            string norm = Normalize(hex);
            return Palette.FirstOrDefault(x => Normalize(x.Hex) == norm) ?? Palette[0];
        }

        public static bool TryParseHex(string hex, out Color color)
        {
            color = Colors.Transparent;

            if (string.IsNullOrWhiteSpace(hex))
                return false;

            string value = hex.Trim().TrimStart('#');

            if (value.Length != 6)
                return false;

            try
            {
                byte r = Convert.ToByte(value.Substring(0, 2), 16);
                byte g = Convert.ToByte(value.Substring(2, 2), 16);
                byte b = Convert.ToByte(value.Substring(4, 2), 16);
                color = Color.FromRgb(r, g, b);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        public static string Normalize(string hex) => (hex ?? "").Trim().TrimStart('#').ToUpperInvariant();

        /// <summary>
        /// Pushes the stored accent into the WPF-UI color resources. Safe to call from any thread
        /// owner as long as it runs on the dispatcher of the calling window.
        /// </summary>
        public static void ApplyAccent()
        {
            string hex = App.Settings.Prop.AccentColor;

            try
            {
                if (string.IsNullOrEmpty(Normalize(hex)))
                {
                    Accent.ApplySystemAccent();
                    ApplyBrushes(Accent.SystemAccent, Wpf.Ui.Appearance.Theme.GetAppTheme());
                    return;
                }

                if (!TryParseHex(hex, out Color color))
                {
                    App.Logger.WriteLine("Branding::ApplyAccent", $"Ignoring invalid accent '{hex}', falling back to system");
                    Accent.ApplySystemAccent();
                    ApplyBrushes(Accent.SystemAccent, Wpf.Ui.Appearance.Theme.GetAppTheme());
                    return;
                }

                // WPF-UI sets the fill colours but leaves the SystemAccentColor*Brush family and the
                // disabled/elevation brushes pointing at the Windows accent, so controls that bind to
                // those keep the old colour. Rewrite the whole family from the chosen colour instead.
                Accent.Apply(color, Wpf.Ui.Appearance.Theme.GetAppTheme());
                ApplyBrushes(color, Wpf.Ui.Appearance.Theme.GetAppTheme());
            }
            catch (Exception ex)
            {
                // a broken accent must never take the window down with it
                App.Logger.WriteLine("Branding::ApplyAccent", $"Failed to apply accent: {ex.Message}");
            }
        }

        /// <summary>
        /// Writes every accent-derived resource key the theme styles resolve against.
        /// Must run on a thread that owns <see cref="Application.Current"/> resources.
        /// </summary>
        private static void ApplyBrushes(Color systemAccent, ThemeType theme)
        {
            ResourceDictionary res = Application.Current.Resources;

            bool darkTheme = theme == ThemeType.Dark;

            // dark surfaces read best with a slightly lifted accent, light surfaces with a deeper one
            Color primary = Shift(systemAccent, darkTheme ? 0.10f : -0.06f);
            Color secondary = Shift(systemAccent, darkTheme ? 0.04f : -0.12f);
            Color tertiary = Shift(systemAccent, darkTheme ? -0.04f : -0.20f);

            res["SystemAccentColor"] = systemAccent;
            res["SystemAccentColorPrimary"] = primary;
            res["SystemAccentColorSecondary"] = secondary;
            res["SystemAccentColorTertiary"] = tertiary;

            res["SystemAccentColorBrush"] = Solid(systemAccent);
            res["SystemAccentColorPrimaryBrush"] = Solid(primary);
            res["SystemAccentColorSecondaryBrush"] = Solid(secondary);
            res["SystemAccentColorTertiaryBrush"] = Solid(tertiary);
            res["SystemAccentBrush"] = Solid(systemAccent);
            res["SystemFillColorAttentionBrush"] = Solid(systemAccent);

            res["AccentFillColorDefaultBrush"] = Solid(systemAccent);
            res["AccentFillColorSecondaryBrush"] = Solid(systemAccent, 0.9);
            res["AccentFillColorTertiaryBrush"] = Solid(systemAccent, 0.8);
            res["AccentFillColorDisabledBrush"] = Solid(systemAccent, 0.32);
            res["AccentFillColorSelectedTextBackgroundBrush"] = Solid(systemAccent);
            res["AccentControlElevationBorderBrush"] = Solid(systemAccent);

            res["AccentTextFillColorPrimaryBrush"] = Solid(primary);
            res["AccentTextFillColorSecondaryBrush"] = Solid(secondary);
            res["AccentTextFillColorTertiaryBrush"] = Solid(tertiary);

            // text drawn on top of an accent fill: black on bright accents, white on dark ones
            Color onAccent = PerceivedLuminance(systemAccent) > 0.62 ? Color.FromRgb(0x11, 0x11, 0x11) : Colors.White;

            res["TextOnAccentFillColorPrimary"] = onAccent;
            res["TextOnAccentFillColorPrimaryBrush"] = Solid(onAccent);
            res["TextOnAccentFillColorSecondaryBrush"] = Solid(onAccent, 0.8);
            res["TextOnAccentFillColorDisabledBrush"] = Solid(onAccent, 0.5);
            res["TextOnAccentFillColorSelectedTextBrush"] = Solid(onAccent);
        }

        private static SolidColorBrush Solid(Color color, double opacity = 1.0)
        {
            var brush = new SolidColorBrush(color) { Opacity = opacity };
            brush.Freeze();
            return brush;
        }

        /// <summary>Moves a colour towards white (positive) or black (negative) by a fraction.</summary>
        private static Color Shift(Color color, float amount)
        {
            byte Channel(byte value) => (byte)Math.Clamp(
                amount >= 0
                    ? value + (255 - value) * amount
                    : value + value * amount,
                0, 255);

            return Color.FromRgb(Channel(color.R), Channel(color.G), Channel(color.B));
        }

        private static double PerceivedLuminance(Color color) =>
            (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;

        public static void NotifyChanged() => Changed?.Invoke(null, EventArgs.Empty);
    }
}
