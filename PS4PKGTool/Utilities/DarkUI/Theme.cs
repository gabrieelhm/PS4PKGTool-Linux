using System.Drawing;

namespace DarkUI.Config
{
    public enum ThemeDepthStyle
    {
        Flat,
        Glossy3D,
        ClassicBevel3D,
        Soft3D,
        Glass3D,
        Neumorphic3D,
        RetroWindows3D,
        Industrial3D,
        Console3D,
        Clay3D,
        Crystal3D,
        Terminal3D,
        Paper3D,
        SciFi3D,
        AeroGlass,
        WindowsXpLuna,
        Windows98Classic
    }

    /// <summary>Defines the overall surface treatment used by theme-aware controls.</summary>
    public enum ThemeSurfaceStyle
    {
        Classic,
        Flat,
        Soft,
        Glass,
        NeoBrutalist
    }

    public class Theme
    {
        public string Name { get; }
        public ThemeDepthStyle DepthStyle { get; init; } = ThemeDepthStyle.Flat;
        /// <summary>Optional selector category for a built-in theme.</summary>
        public string Category { get; init; }

        // ── Geometry and surface tokens ──
        // Legacy themes retain their existing square 1px treatment by default.
        public ThemeSurfaceStyle SurfaceStyle { get; init; } = ThemeSurfaceStyle.Classic;
        public int CornerRadius { get; init; }
        public int BorderThickness { get; init; } = 1;
        public int ControlHeight { get; init; } = 28;
        public int ContentSpacing { get; init; } = 8;
        public int Elevation { get; init; }

        // ── Backgrounds ──
        public Color GreyBackground { get; init; }
        public Color HeaderBackground { get; init; }
        public Color MediumBackground { get; init; }
        public Color LightBackground { get; init; }
        public Color LighterBackground { get; init; }
        public Color LightestBackground { get; init; }
        public Color DarkBackground { get; init; }
        public Color BlueBackground { get; init; }
        public Color DarkBlueBackground { get; init; }

        // ── Borders ──
        public Color LightBorder { get; init; }
        public Color DarkBorder { get; init; }
        public Color DarkBlueBorder { get; init; }
        public Color LightBlueBorder { get; init; }

        // ── Text ──
        public Color LightText { get; init; }
        public Color DisabledText { get; init; }

        /// <summary>
        /// Optional foreground for content selected with <see cref="BlueSelection"/>.
        /// When omitted, the theme chooses whichever of black or white has the
        /// stronger WCAG contrast against the selection color.
        /// </summary>
        public Color? SelectionText { get; init; }

        // ── Accent / selection ──
        public Color BlueHighlight { get; init; }
        public Color BlueSelection { get; init; }
        public Color GreyHighlight { get; init; }
        public Color GreySelection { get; init; }
        public Color DarkGreySelection { get; init; }
        public Color ActiveControl { get; init; }

        // ── Menu ──
        public Color MenuItemToggledOnFill { get; init; }
        public Color MenuItemToggledOnBorder { get; init; }

        // ── Semantic status accents (used by DarkToast) ──
        // Mid-tone colors that stay visible on both dark and light themes;
        // themes may override them via init.
        public Color StatusSuccess { get; init; } = Color.FromArgb(120, 200, 90);
        public Color StatusWarning { get; init; } = Color.FromArgb(220, 180, 60);
        public Color StatusError { get; init; } = Color.FromArgb(220, 80, 80);

        public Theme(string name)
        {
            Name = name;
        }

        /// <summary>Gets the accessible foreground for the theme selection surface.</summary>
        public Color GetSelectionText()
        {
            if (SelectionText.HasValue)
                return SelectionText.Value;

            return ContrastRatio(Color.Black, BlueSelection) >= ContrastRatio(Color.White, BlueSelection)
                ? Color.Black
                : Color.White;
        }

        private static double ContrastRatio(Color first, Color second)
        {
            var firstLuminance = RelativeLuminance(first);
            var secondLuminance = RelativeLuminance(second);
            return (System.Math.Max(firstLuminance, secondLuminance) + 0.05) /
                   (System.Math.Min(firstLuminance, secondLuminance) + 0.05);
        }

        private static double RelativeLuminance(Color color)
        {
            static double Linearize(byte channel)
            {
                var value = channel / 255d;
                return value <= 0.04045d
                    ? value / 12.92d
                    : System.Math.Pow((value + 0.055d) / 1.055d, 2.4d);
            }

            return 0.2126d * Linearize(color.R) +
                   0.7152d * Linearize(color.G) +
                   0.0722d * Linearize(color.B);
        }
    }
}
