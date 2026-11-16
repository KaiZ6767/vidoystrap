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

        /// <summary>The accent actually in force right now: the stored one, or the Windows accent.</summary>
        public static Color EffectiveAccent
        {
            get
            {
                if (TryParseHex(App.Settings.Prop.AccentColor, out Color color))
                    return color;

                return Accent.SystemAccent;
            }
        }

        /// <summary>
        /// The colour the window shell is painted with. Everything in the WPF-UI theme is
        /// translucent white/black layered over this one surface, so tinting it here is what
        /// carries the accent into the nav rail, cards, inputs and title bar at once.
        /// </summary>
        public static Color WindowBaseColor
        {
            get
            {
                // a fully saturated neon behind translucent white would read as grey,
                // so the base keeps most of the shell's original lightness
                return Wpf.Ui.Appearance.Theme.GetAppTheme() == ThemeType.Dark
                    ? Mix(Color.FromRgb(0x14, 0x14, 0x14), EffectiveAccent, 0.30)
                    : Mix(Color.FromRgb(0xF4, 0xF4, 0xF4), EffectiveAccent, 0.16);
            }
        }

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
        /// Pushes the stored accent into the application resources. Safe to call from any thread
        /// owner as long as it runs on the dispatcher of the calling window.
        /// </summary>
        public static void ApplyAccent()
        {
            string hex = App.Settings.Prop.AccentColor;

            try
            {
                Color accent;

                if (string.IsNullOrEmpty(Normalize(hex)))
                {
                    // "Sistema" means exactly what the app used to do: take the DWM colour
                    Accent.ApplySystemAccent();
                    accent = Accent.SystemAccent;
                }
                else if (TryParseHex(hex, out accent))
                {
                    Accent.Apply(accent, Wpf.Ui.Appearance.Theme.GetAppTheme());
                }
                else
                {
                    App.Logger.WriteLine("Branding::ApplyAccent", $"Ignoring invalid accent '{hex}', falling back to system");
                    Accent.ApplySystemAccent();
                    accent = Accent.SystemAccent;
                }

                ApplyBrushes(accent, Wpf.Ui.Appearance.Theme.GetAppTheme(), Application.Current.Resources);
            }
            catch (Exception ex)
            {
                // a broken accent must never take the window down with it
                App.Logger.WriteLine("Branding::ApplyAccent", $"Failed to apply accent: {ex.Message}");
            }
        }

        /// <summary>
        /// Same accent, written into a specific resource dictionary. Windows that shadow theme
        /// keys with their own merged dictionaries keep their own copy, so they need this too.
        /// </summary>
        public static void ApplyAccent(ResourceDictionary target)
        {
            try
            {
                ApplyBrushes(EffectiveAccent, Wpf.Ui.Appearance.Theme.GetAppTheme(), target);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("Branding::ApplyAccent", $"Failed to apply accent to window resources: {ex.Message}");
            }
        }

        /// <summary>
        /// Writes every accent-derived resource key the theme styles resolve against.
        /// Must run on a thread that owns the target dictionary.
        /// </summary>
        private static void ApplyBrushes(Color systemAccent, ThemeType theme, ResourceDictionary res)
        {
            bool darkTheme = theme == ThemeType.Dark;

            // dark surfaces read best with a lighter accent, light surfaces with a deeper one,
            // so links and accent text keep contrast against the accent-tinted shell
            Color primary = Shift(systemAccent, darkTheme ? 0.42f : -0.30f);
            Color secondary = Shift(systemAccent, darkTheme ? 0.08f : -0.10f);
            Color tertiary = Shift(systemAccent, darkTheme ? 0.18f : -0.18f);

            // text drawn on top of an accent fill: black on bright accents, white on dark ones
            Color onAccent = PerceivedLuminance(systemAccent) > 0.62 ? Color.FromRgb(0x11, 0x11, 0x11) : Colors.White;

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
            res["AccentFillColorDisabled"] = WithAlpha(systemAccent, 0x59);

            res["AccentTextFillColorPrimaryBrush"] = Solid(primary);
            res["AccentTextFillColorSecondaryBrush"] = Solid(secondary);
            res["AccentTextFillColorTertiaryBrush"] = Solid(tertiary);
            res["AccentTextFillColorDisabledBrush"] = Solid(onAccent, 0.5);
            res["AccentTextFillColorDisabled"] = WithAlpha(onAccent, 0x5D);

            res["TextOnAccentFillColorPrimary"] = onAccent;
            res["TextOnAccentFillColorSecondary"] = WithAlpha(onAccent, 0x80);
            res["TextOnAccentFillColorDisabled"] = WithAlpha(onAccent, 0x66);
            res["TextOnAccentFillColorSelectedText"] = onAccent;

            res["TextOnAccentFillColorPrimaryBrush"] = Solid(onAccent);
            res["TextOnAccentFillColorSecondaryBrush"] = Solid(onAccent, 0.8);
            res["TextOnAccentFillColorDisabledBrush"] = Solid(onAccent, 0.5);
            res["TextOnAccentFillColorSelectedTextBrush"] = Solid(onAccent);

            ApplySurfaces(systemAccent, darkTheme, res);
        }

        /// <summary>
        /// Tints the layer the whole shell is built on and the translucent panels that sit on
        /// top of it, so no window falls back to the stock grey when an accent is selected.
        /// </summary>
        private static void ApplySurfaces(Color accent, bool dark, ResourceDictionary res)
        {
            Color Base(byte value) => Color.FromRgb(value, value, value);

            Color baseFill = WindowBaseColor;
            Color secondaryFill = dark ? Mix(Base(0x12), accent, 0.26) : Mix(Base(0xEE), accent, 0x18 / 255.0);
            Color tertiaryFill = dark ? Mix(Base(0x1C), accent, 0.34) : Mix(Base(0xF8), accent, 0.24);
            Color quarternaryFill = dark ? Mix(Base(0x22), accent, 0.36) : Mix(Base(0xFF), accent, 0.26);
            Color baseAlt = dark ? Mix(Base(0x24), accent, 0.34) : Mix(Base(0xFB), accent, 0.24);

            res["ApplicationBackgroundColor"] = baseFill;
            res["ApplicationBackgroundBrush"] = Solid(baseFill);
            res["MainWindowBackgroundBrush"] = Solid(baseFill);

            res["SolidBackgroundFillColorBase"] = baseFill;
            res["SolidBackgroundFillColorBaseBrush"] = Solid(baseFill);
            res["SolidBackgroundFillColorBaseAlt"] = baseAlt;
            res["SolidBackgroundFillColorBaseAltBrush"] = Solid(baseAlt);
            res["SolidBackgroundFillColorSecondary"] = secondaryFill;
            res["SolidBackgroundFillColorSecondaryBrush"] = Solid(secondaryFill);
            res["SolidBackgroundFillColorTertiary"] = tertiaryFill;
            res["SolidBackgroundFillColorTertiaryBrush"] = Solid(tertiaryFill);
            res["SolidBackgroundFillColorQuarternary"] = quarternaryFill;
            res["SolidBackgroundFillColorQuarternaryBrush"] = Solid(quarternaryFill);

            // translucent layers: keep the original alpha feel but pull the hue to the accent
            Color liftedWhite = Mix(Colors.White, accent, dark ? 0.40 : 0.30);
            Color liftedBlack = Mix(Colors.Black, accent, dark ? 0.35 : 0.30);

            res["LayerFillColorDefault"] = WithAlpha(Mix(Base(0x3A), accent, 0.38), 0x4C);
            res["LayerFillColorDefaultBrush"] = Solid(Mix(Base(0x3A), accent, 0.38), 0.30);
            res["LayerFillColorAltBrush"] = Solid(liftedWhite, 0.07);
            res["CardBackgroundFillColorDefaultBrush"] = Solid(liftedWhite, 0.10);
            res["CardBackgroundFillColorSecondaryBrush"] = Solid(liftedWhite, 0.06);
            res["CardStrokeColorDefault"] = WithAlpha(accent, 0x40);
            res["CardStrokeColorDefaultBrush"] = Solid(accent, 0.25);
            res["CardStrokeColorDefaultSolidBrush"] = Solid(Mix(baseFill, accent, 0.5));
            res["ControlFillColorDefault"] = WithAlpha(liftedWhite, 0x16);
            res["ControlFillColorDefaultBrush"] = Solid(liftedWhite, 0.09);
            res["ControlFillColorSecondary"] = WithAlpha(liftedWhite, 0x22);
            res["ControlFillColorSecondaryBrush"] = Solid(liftedWhite, 0.14);
            res["ControlFillColorTertiary"] = WithAlpha(liftedWhite, 0x0E);
            res["ControlFillColorTertiaryBrush"] = Solid(liftedWhite, 0.06);
            res["ControlFillColorInputActive"] = WithAlpha(dark ? Mix(Base(0x1E), accent, 0.30) : Mix(Base(0xFF), accent, 0.18), 0xB3);
            res["ControlFillColorInputActiveBrush"] = Solid(dark ? Mix(Base(0x1E), accent, 0.30) : Mix(Base(0xFF), accent, 0.18), 0.70);
            res["SubtleFillColorSecondaryBrush"] = Solid(liftedWhite, 0.09);
            res["ControlAltFillColorSecondaryBrush"] = Solid(liftedBlack, 0.10);
            res["ControlAltFillColorTertiaryBrush"] = Solid(liftedWhite, 0.05);

            res["ControlStrokeColorDefault"] = WithAlpha(accent, 0x33);
            res["ControlStrokeColorDefaultBrush"] = Solid(accent, 0.20);
            res["ControlStrokeColorSecondary"] = WithAlpha(accent, 0x4D);
            res["ControlStrokeColorSecondaryBrush"] = Solid(accent, 0.30);
            res["DividerStrokeColorDefault"] = WithAlpha(accent, 0x55);
            res["DividerStrokeColorDefaultBrush"] = Solid(accent, 0.33);
            res["FocusStrokeColorOuter"] = accent;
            res["FocusStrokeColorOuterBrush"] = Solid(accent);
        }

        private static Color WithAlpha(Color color, byte alpha) => Color.FromArgb(alpha, color.R, color.G, color.B);

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

        /// <summary>Linear blend from <paramref name="a"/> to <paramref name="b"/>; 0 = a, 1 = b.</summary>
        private static Color Mix(Color a, Color b, double amount)
        {
            byte Channel(byte from, byte to) =>
                (byte)Math.Clamp(from + (to - from) * amount, 0, 255);

            return Color.FromRgb(Channel(a.R, b.R), Channel(a.G, b.G), Channel(a.B, b.B));
        }

        private static double PerceivedLuminance(Color color) =>
            (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;

        public static void NotifyChanged() => Changed?.Invoke(null, EventArgs.Empty);
    }
}
