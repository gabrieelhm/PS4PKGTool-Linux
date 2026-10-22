using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace DarkUI.Config
{
    public static class ThemeManager
    {
        public const string AllThemesCategory = "All themes";
        public const string DeepDarkCategory = "Deep dark";
        public const string NeutralDarkCategory = "Neutral dark";
        public const string DeveloperCategory = "Developer palettes";
        public const string AccentDarkCategory = "Color-accent dark";
        public const string LightCategory = "Light and soft";
        public const string HighContrastCategory = "High contrast";
        public const string ThreeDCategory = "3D themes";
        public const string Glass3DCategory = "Glass 3D";
        public const string Neumorphic3DCategory = "Neumorphic 3D";
        public const string RetroWindows3DCategory = "Retro Windows 3D";
        public const string Industrial3DCategory = "Industrial Metal 3D";
        public const string Console3DCategory = "Console / Game 3D";
        public const string PlayStation4Category = "PlayStation 4 3D";
        public const string Clay3DCategory = "Clay 3D";
        public const string Crystal3DCategory = "Crystal / Gem 3D";
        public const string Terminal3DCategory = "Terminal Hardware 3D";
        public const string Paper3DCategory = "Paper Card 3D";
        public const string SciFi3DCategory = "Sci-fi HUD 3D";

        public static event EventHandler ThemeChanged;

        private static Theme _active = BuiltIn.Default;
        public static Theme Active
        {
            get => _active;
            set
            {
                if (value == null || ReferenceEquals(value, _active)) return;
                _active = value;
                ThemeChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static IReadOnlyList<Theme> Presets { get; } = new List<Theme>
        {
            BuiltIn.Default, BuiltIn.Obscura, BuiltIn.Emerald, BuiltIn.Crimson,
            BuiltIn.Arctic, BuiltIn.Solar, BuiltIn.Void, BuiltIn.Violet,
            BuiltIn.Dracula, BuiltIn.Nord, BuiltIn.OneDark, BuiltIn.TokyoNight,
            BuiltIn.Catppuccin, BuiltIn.Gruvbox, BuiltIn.Monokai,
            BuiltIn.BordeauxNoir, BuiltIn.MossCarbon, BuiltIn.CopperForge,
            BuiltIn.Gunmetal, BuiltIn.RoseAsh, BuiltIn.AcidOlive,
            BuiltIn.DeepCobalt, BuiltIn.SepiaNoir,
            BuiltIn.CarbonBlue, BuiltIn.GraphiteCyan, BuiltIn.SignalGreen,
            BuiltIn.RoyalViolet, BuiltIn.BlackCherry, BuiltIn.AmberNight,
            BuiltIn.IceSteel, BuiltIn.NeonAzure, BuiltIn.MutedRose,
            BuiltIn.LimeTerminal,
            BuiltIn.Ps4Midnight, BuiltIn.OledPurple, BuiltIn.GunmetalOrange,
            BuiltIn.ForestNight, BuiltIn.WarmPaper,
            BuiltIn.PitchBlack, BuiltIn.Carbon, BuiltIn.Graphite,
            BuiltIn.SlateGrey, BuiltIn.Steel, BuiltIn.Ash, BuiltIn.SilverSmoke,
            BuiltIn.PewterLight, BuiltIn.Frost, BuiltIn.Porcelain,
            BuiltIn.HighContrastBlack, BuiltIn.HighContrastWhite,
            BuiltIn.SoftCharcoal, BuiltIn.FogGrey, BuiltIn.Cloud, BuiltIn.Snow,
            BuiltIn.IvoryPaper, BuiltIn.Moonlight, BuiltIn.SageMist,
            BuiltIn.LavenderMist, BuiltIn.PowderBlue, BuiltIn.Sandstone,
            BuiltIn.Terracotta, BuiltIn.OceanDepth, BuiltIn.TurquoiseGlass,
            BuiltIn.Orchid, BuiltIn.Raspberry, BuiltIn.Honeycomb,
            BuiltIn.MintCream, BuiltIn.Blueprint,
            BuiltIn.CyberChrome3D, BuiltIn.VioletArcade3D, BuiltIn.EmeraldConsole3D,
            BuiltIn.CrimsonMachine3D, BuiltIn.AmberForge3D, BuiltIn.CobaltCabinet3D,
            BuiltIn.MonochromeBevel3D, BuiltIn.IvoryBevel3D, BuiltIn.SoftGraphite3D,
            BuiltIn.SoftLavender3D,
        }.Concat(BuiltIn.ConceptThemes).ToArray();

        public static IReadOnlyList<string> ThemeCategories { get; } = new[]
        {
            AllThemesCategory,
            DeepDarkCategory,
            NeutralDarkCategory,
            DeveloperCategory,
            AccentDarkCategory,
            LightCategory,
            HighContrastCategory,
            ThreeDCategory,
            Glass3DCategory,
            Neumorphic3DCategory,
            RetroWindows3DCategory,
            Industrial3DCategory,
            Console3DCategory,
            PlayStation4Category,
            Clay3DCategory,
            Crystal3DCategory,
            Terminal3DCategory,
            Paper3DCategory,
            SciFi3DCategory
        };

        public static IReadOnlyList<Theme> GetThemes(string category)
        {
            if (string.IsNullOrEmpty(category) || category == AllThemesCategory)
                return Presets;

            return Presets.Where(theme => GetThemeCategory(theme) == category).ToArray();
        }

        public static string GetThemeCategory(Theme theme)
        {
            if (theme == null)
                return AllThemesCategory;

            if (!string.IsNullOrWhiteSpace(theme.Category))
                return theme.Category;

            return theme.Name switch
            {
                "High Contrast Black" or "High Contrast White" => HighContrastCategory,
                "Cyber Chrome 3D" or "Violet Arcade 3D" or "Emerald Console 3D" or
                "Crimson Machine 3D" or "Amber Forge 3D" or "Cobalt Cabinet 3D" or
                "Monochrome Bevel 3D" or "Ivory Bevel 3D" or "Soft Graphite 3D" or
                "Soft Lavender 3D" => ThreeDCategory,

                "Dracula" or "Nord" or "One Dark" or "Tokyo Night" or
                "Catppuccin" or "Gruvbox" or "Monokai" => DeveloperCategory,

                "Obsidian" or "Void" or "Pitch Black" or "Carbon" or "OLED Purple" => DeepDarkCategory,

                "Default (Charcoal)" or "Gunmetal" or "Graphite" or "Slate Grey" or
                "Steel" or "Ash" or "Silver Smoke" or "Soft Charcoal" or "Fog Grey" or
                "Moonlight" => NeutralDarkCategory,

                "Pewter Light" or "Frost" or "Porcelain" or "Cloud" or "Snow" or
                "Ivory Paper" or "Sage Mist" or "Lavender Mist" or "Powder Blue" or
                "Sandstone" or "Terracotta" or "Ocean Depth" or "Turquoise Glass" or
                "Orchid" or "Raspberry" or "Honeycomb" or "Mint Cream" or "Blueprint" or
                "Warm Paper" => LightCategory,

                _ => AccentDarkCategory
            };
        }

        public static void Apply(Theme theme) => Active = theme;

        /// <summary>
        /// Re-fires ThemeChanged without changing the theme. Use after all controls
        /// have been created (e.g. form Shown) so late-created controls that missed
        /// the original Apply pick up the current theme.
        /// </summary>
        public static void Refresh() => ThemeChanged?.Invoke(null, EventArgs.Empty);

        public static class BuiltIn
        {
            public static Theme Default { get; } = new Theme("Default (Charcoal)")
            {
                GreyBackground = Color.FromArgb(60, 63, 65),
                HeaderBackground = Color.FromArgb(57, 60, 62),
                MediumBackground = Color.FromArgb(49, 51, 53),
                LightBackground = Color.FromArgb(69, 73, 74),
                LighterBackground = Color.FromArgb(95, 101, 102),
                LightestBackground = Color.FromArgb(178, 178, 178),
                DarkBackground = Color.FromArgb(43, 43, 43),
                BlueBackground = Color.FromArgb(66, 77, 95),
                DarkBlueBackground = Color.FromArgb(52, 57, 66),
                LightBorder = Color.FromArgb(81, 81, 81),
                DarkBorder = Color.FromArgb(51, 51, 51),
                DarkBlueBorder = Color.FromArgb(51, 61, 78),
                LightBlueBorder = Color.FromArgb(86, 97, 114),
                LightText = Color.FromArgb(220, 220, 220),
                DisabledText = Color.FromArgb(153, 153, 153),
                BlueHighlight = Color.FromArgb(104, 151, 187),
                BlueSelection = Color.FromArgb(75, 110, 175),
                GreyHighlight = Color.FromArgb(122, 128, 132),
                GreySelection = Color.FromArgb(92, 92, 92),
                DarkGreySelection = Color.FromArgb(82, 82, 82),
                ActiveControl = Color.FromArgb(159, 178, 196),
                MenuItemToggledOnFill = Color.FromArgb(105, 84, 69),
                MenuItemToggledOnBorder = Color.FromArgb(225, 128, 68),
            };

            public static Theme Obscura { get; } = new Theme("Obsidian")
            {
                GreyBackground = Color.FromArgb(18, 19, 22),
                HeaderBackground = Color.FromArgb(16, 17, 19),
                MediumBackground = Color.FromArgb(26, 28, 32),
                LightBackground = Color.FromArgb(34, 36, 42),
                LighterBackground = Color.FromArgb(44, 46, 54),
                LightestBackground = Color.FromArgb(120, 124, 135),
                DarkBackground = Color.FromArgb(10, 11, 14),
                BlueBackground = Color.FromArgb(28, 32, 38),
                DarkBlueBackground = Color.FromArgb(20, 22, 28),
                LightBorder = Color.FromArgb(42, 44, 50),
                DarkBorder = Color.FromArgb(28, 30, 34),
                DarkBlueBorder = Color.FromArgb(28, 32, 38),
                LightBlueBorder = Color.FromArgb(44, 48, 56),
                LightText = Color.FromArgb(205, 208, 212),
                DisabledText = Color.FromArgb(110, 113, 118),
                BlueHighlight = Color.FromArgb(195, 155, 65),
                BlueSelection = Color.FromArgb(200, 150, 55),
                GreyHighlight = Color.FromArgb(82, 88, 96),
                GreySelection = Color.FromArgb(42, 46, 54),
                DarkGreySelection = Color.FromArgb(32, 34, 40),
                ActiveControl = Color.FromArgb(185, 155, 100),
                MenuItemToggledOnFill = Color.FromArgb(55, 45, 32),
                MenuItemToggledOnBorder = Color.FromArgb(195, 160, 80),
            };

            public static Theme Emerald { get; } = new Theme("Midnight Emerald")
            {
                GreyBackground = Color.FromArgb(16, 22, 24),
                HeaderBackground = Color.FromArgb(14, 19, 21),
                MediumBackground = Color.FromArgb(22, 30, 33),
                LightBackground = Color.FromArgb(30, 38, 42),
                LighterBackground = Color.FromArgb(38, 48, 54),
                LightestBackground = Color.FromArgb(105, 130, 138),
                DarkBackground = Color.FromArgb(8, 14, 16),
                BlueBackground = Color.FromArgb(24, 34, 38),
                DarkBlueBackground = Color.FromArgb(16, 24, 28),
                LightBorder = Color.FromArgb(38, 48, 52),
                DarkBorder = Color.FromArgb(22, 30, 33),
                DarkBlueBorder = Color.FromArgb(22, 32, 36),
                LightBlueBorder = Color.FromArgb(40, 50, 56),
                LightText = Color.FromArgb(200, 210, 214),
                DisabledText = Color.FromArgb(105, 118, 122),
                BlueHighlight = Color.FromArgb(55, 165, 150),
                BlueSelection = Color.FromArgb(60, 170, 158),
                GreyHighlight = Color.FromArgb(72, 105, 108),
                GreySelection = Color.FromArgb(30, 45, 55),
                DarkGreySelection = Color.FromArgb(22, 33, 42),
                ActiveControl = Color.FromArgb(100, 180, 170),
                MenuItemToggledOnFill = Color.FromArgb(30, 55, 52),
                MenuItemToggledOnBorder = Color.FromArgb(80, 175, 160),
            };

            public static Theme Crimson { get; } = new Theme("Crimson Ash")
            {
                GreyBackground = Color.FromArgb(22, 20, 19),
                HeaderBackground = Color.FromArgb(20, 18, 17),
                MediumBackground = Color.FromArgb(30, 28, 26),
                LightBackground = Color.FromArgb(40, 36, 34),
                LighterBackground = Color.FromArgb(52, 48, 44),
                LightestBackground = Color.FromArgb(130, 122, 115),
                DarkBackground = Color.FromArgb(14, 13, 11),
                BlueBackground = Color.FromArgb(36, 28, 26),
                DarkBlueBackground = Color.FromArgb(26, 20, 18),
                LightBorder = Color.FromArgb(50, 46, 42),
                DarkBorder = Color.FromArgb(32, 30, 27),
                DarkBlueBorder = Color.FromArgb(32, 28, 25),
                LightBlueBorder = Color.FromArgb(54, 48, 44),
                LightText = Color.FromArgb(225, 218, 210),
                DisabledText = Color.FromArgb(118, 112, 106),
                BlueHighlight = Color.FromArgb(200, 90, 75),
                BlueSelection = Color.FromArgb(205, 95, 80),
                GreyHighlight = Color.FromArgb(125, 100, 92),
                GreySelection = Color.FromArgb(50, 35, 34),
                DarkGreySelection = Color.FromArgb(38, 26, 25),
                ActiveControl = Color.FromArgb(205, 140, 125),
                MenuItemToggledOnFill = Color.FromArgb(55, 38, 32),
                MenuItemToggledOnBorder = Color.FromArgb(200, 108, 85),
            };

            public static Theme Arctic { get; } = new Theme("Arctic Dusk")
            {
                GreyBackground = Color.FromArgb(17, 21, 28),
                HeaderBackground = Color.FromArgb(15, 19, 25),
                MediumBackground = Color.FromArgb(24, 28, 36),
                LightBackground = Color.FromArgb(32, 36, 46),
                LighterBackground = Color.FromArgb(40, 46, 58),
                LightestBackground = Color.FromArgb(115, 124, 140),
                DarkBackground = Color.FromArgb(10, 13, 18),
                BlueBackground = Color.FromArgb(26, 32, 42),
                DarkBlueBackground = Color.FromArgb(18, 24, 32),
                LightBorder = Color.FromArgb(36, 42, 52),
                DarkBorder = Color.FromArgb(22, 28, 35),
                DarkBlueBorder = Color.FromArgb(22, 30, 38),
                LightBlueBorder = Color.FromArgb(40, 48, 60),
                LightText = Color.FromArgb(195, 200, 210),
                DisabledText = Color.FromArgb(105, 112, 120),
                BlueHighlight = Color.FromArgb(115, 148, 178),
                BlueSelection = Color.FromArgb(130, 155, 185),
                GreyHighlight = Color.FromArgb(82, 98, 116),
                GreySelection = Color.FromArgb(34, 42, 55),
                DarkGreySelection = Color.FromArgb(26, 34, 45),
                ActiveControl = Color.FromArgb(150, 175, 200),
                MenuItemToggledOnFill = Color.FromArgb(40, 50, 62),
                MenuItemToggledOnBorder = Color.FromArgb(120, 150, 175),
            };

            public static Theme Solar { get; } = new Theme("Solar Ash")
            {
                GreyBackground = Color.FromArgb(26, 22, 18),
                HeaderBackground = Color.FromArgb(23, 19, 15),
                MediumBackground = Color.FromArgb(34, 28, 24),
                LightBackground = Color.FromArgb(44, 36, 30),
                LighterBackground = Color.FromArgb(56, 48, 38),
                LightestBackground = Color.FromArgb(135, 124, 108),
                DarkBackground = Color.FromArgb(16, 13, 10),
                BlueBackground = Color.FromArgb(38, 30, 24),
                DarkBlueBackground = Color.FromArgb(28, 22, 16),
                LightBorder = Color.FromArgb(56, 48, 40),
                DarkBorder = Color.FromArgb(34, 30, 24),
                DarkBlueBorder = Color.FromArgb(34, 28, 22),
                LightBlueBorder = Color.FromArgb(60, 52, 42),
                LightText = Color.FromArgb(224, 215, 202),
                DisabledText = Color.FromArgb(120, 112, 102),
                BlueHighlight = Color.FromArgb(218, 135, 48),
                BlueSelection = Color.FromArgb(225, 140, 52),
                GreyHighlight = Color.FromArgb(125, 100, 70),
                GreySelection = Color.FromArgb(50, 40, 30),
                DarkGreySelection = Color.FromArgb(38, 30, 22),
                ActiveControl = Color.FromArgb(220, 160, 98),
                MenuItemToggledOnFill = Color.FromArgb(58, 44, 28),
                MenuItemToggledOnBorder = Color.FromArgb(215, 145, 65),
            };

            public static Theme Void { get; } = new Theme("Void")
            {
                GreyBackground = Color.FromArgb(0, 0, 0),
                HeaderBackground = Color.FromArgb(4, 4, 4),
                MediumBackground = Color.FromArgb(8, 8, 8),
                LightBackground = Color.FromArgb(14, 14, 16),
                LighterBackground = Color.FromArgb(22, 22, 26),
                LightestBackground = Color.FromArgb(110, 110, 115),
                DarkBackground = Color.FromArgb(0, 0, 0),
                BlueBackground = Color.FromArgb(10, 10, 18),
                DarkBlueBackground = Color.FromArgb(4, 4, 10),
                LightBorder = Color.FromArgb(26, 26, 28),
                DarkBorder = Color.FromArgb(16, 16, 18),
                DarkBlueBorder = Color.FromArgb(16, 16, 24),
                LightBlueBorder = Color.FromArgb(30, 30, 38),
                LightText = Color.FromArgb(200, 200, 200),
                DisabledText = Color.FromArgb(90, 90, 95),
                BlueHighlight = Color.FromArgb(80, 200, 250),
                BlueSelection = Color.FromArgb(90, 210, 255),
                GreyHighlight = Color.FromArgb(52, 52, 52),
                GreySelection = Color.FromArgb(16, 16, 30),
                DarkGreySelection = Color.FromArgb(10, 10, 20),
                ActiveControl = Color.FromArgb(130, 210, 245),
                MenuItemToggledOnFill = Color.FromArgb(25, 35, 48),
                MenuItemToggledOnBorder = Color.FromArgb(100, 190, 235),
            };

            public static Theme Violet { get; } = new Theme("Violet Hour")
            {
                GreyBackground = Color.FromArgb(18, 15, 30),
                HeaderBackground = Color.FromArgb(16, 13, 27),
                MediumBackground = Color.FromArgb(24, 21, 38),
                LightBackground = Color.FromArgb(32, 28, 48),
                LighterBackground = Color.FromArgb(42, 36, 60),
                LightestBackground = Color.FromArgb(120, 112, 145),
                DarkBackground = Color.FromArgb(10, 8, 20),
                BlueBackground = Color.FromArgb(30, 24, 44),
                DarkBlueBackground = Color.FromArgb(22, 16, 34),
                LightBorder = Color.FromArgb(40, 35, 56),
                DarkBorder = Color.FromArgb(24, 22, 36),
                DarkBlueBorder = Color.FromArgb(26, 22, 38),
                LightBlueBorder = Color.FromArgb(44, 38, 60),
                LightText = Color.FromArgb(218, 212, 234),
                DisabledText = Color.FromArgb(112, 108, 130),
                BlueHighlight = Color.FromArgb(160, 125, 250),
                BlueSelection = Color.FromArgb(170, 140, 250),
                GreyHighlight = Color.FromArgb(110, 95, 160),
                GreySelection = Color.FromArgb(40, 32, 62),
                DarkGreySelection = Color.FromArgb(30, 24, 50),
                ActiveControl = Color.FromArgb(175, 155, 240),
                MenuItemToggledOnFill = Color.FromArgb(45, 35, 65),
                MenuItemToggledOnBorder = Color.FromArgb(155, 125, 225),
            };

            public static Theme Dracula { get; } = new Theme("Dracula")
            {
                GreyBackground = Color.FromArgb(26, 28, 38),
                HeaderBackground = Color.FromArgb(22, 23, 30),
                MediumBackground = Color.FromArgb(30, 32, 42),
                LightBackground = Color.FromArgb(38, 40, 52),
                LighterBackground = Color.FromArgb(48, 50, 64),
                LightestBackground = Color.FromArgb(120, 122, 140),
                DarkBackground = Color.FromArgb(16, 17, 24),
                BlueBackground = Color.FromArgb(34, 32, 48),
                DarkBlueBackground = Color.FromArgb(24, 23, 36),
                LightBorder = Color.FromArgb(46, 48, 62),
                DarkBorder = Color.FromArgb(30, 32, 42),
                DarkBlueBorder = Color.FromArgb(30, 32, 44),
                LightBlueBorder = Color.FromArgb(48, 50, 66),
                LightText = Color.FromArgb(245, 245, 240),
                DisabledText = Color.FromArgb(115, 115, 125),
                BlueHighlight = Color.FromArgb(180, 140, 245),
                BlueSelection = Color.FromArgb(250, 115, 190),
                GreyHighlight = Color.FromArgb(100, 100, 112),
                GreySelection = Color.FromArgb(75, 85, 125),
                DarkGreySelection = Color.FromArgb(46, 48, 62),
                ActiveControl = Color.FromArgb(130, 225, 250),
                MenuItemToggledOnFill = Color.FromArgb(52, 44, 62),
                MenuItemToggledOnBorder = Color.FromArgb(250, 115, 190),
            };

            public static Theme Nord { get; } = new Theme("Nord")
            {
                GreyBackground = Color.FromArgb(30, 34, 42),
                HeaderBackground = Color.FromArgb(26, 29, 36),
                MediumBackground = Color.FromArgb(40, 44, 56),
                LightBackground = Color.FromArgb(48, 53, 66),
                LighterBackground = Color.FromArgb(56, 62, 78),
                LightestBackground = Color.FromArgb(130, 138, 155),
                DarkBackground = Color.FromArgb(22, 25, 32),
                BlueBackground = Color.FromArgb(34, 38, 52),
                DarkBlueBackground = Color.FromArgb(26, 29, 38),
                LightBorder = Color.FromArgb(54, 62, 76),
                DarkBorder = Color.FromArgb(34, 38, 48),
                DarkBlueBorder = Color.FromArgb(32, 36, 48),
                LightBlueBorder = Color.FromArgb(56, 64, 80),
                LightText = Color.FromArgb(232, 236, 242),
                DisabledText = Color.FromArgb(115, 120, 130),
                BlueHighlight = Color.FromArgb(125, 155, 190),
                BlueSelection = Color.FromArgb(130, 185, 200),
                GreyHighlight = Color.FromArgb(92, 102, 118),
                GreySelection = Color.FromArgb(70, 90, 130),
                DarkGreySelection = Color.FromArgb(40, 44, 56),
                ActiveControl = Color.FromArgb(135, 180, 180),
                MenuItemToggledOnFill = Color.FromArgb(40, 50, 62),
                MenuItemToggledOnBorder = Color.FromArgb(130, 185, 200),
            };

            public static Theme OneDark { get; } = new Theme("One Dark")
            {
                GreyBackground = Color.FromArgb(26, 29, 35),
                HeaderBackground = Color.FromArgb(22, 24, 29),
                MediumBackground = Color.FromArgb(32, 36, 44),
                LightBackground = Color.FromArgb(38, 42, 52),
                LighterBackground = Color.FromArgb(48, 53, 66),
                LightestBackground = Color.FromArgb(118, 124, 138),
                DarkBackground = Color.FromArgb(18, 20, 25),
                BlueBackground = Color.FromArgb(32, 38, 48),
                DarkBlueBackground = Color.FromArgb(24, 28, 36),
                LightBorder = Color.FromArgb(44, 48, 58),
                DarkBorder = Color.FromArgb(32, 36, 44),
                DarkBlueBorder = Color.FromArgb(32, 36, 44),
                LightBlueBorder = Color.FromArgb(46, 52, 62),
                LightText = Color.FromArgb(168, 175, 188),
                DisabledText = Color.FromArgb(95, 100, 110),
                BlueHighlight = Color.FromArgb(80, 175, 188),
                BlueSelection = Color.FromArgb(75, 130, 245),
                GreyHighlight = Color.FromArgb(100, 108, 120),
                GreySelection = Color.FromArgb(44, 48, 58),
                DarkGreySelection = Color.FromArgb(34, 38, 46),
                ActiveControl = Color.FromArgb(220, 100, 110),
                MenuItemToggledOnFill = Color.FromArgb(48, 55, 65),
                MenuItemToggledOnBorder = Color.FromArgb(80, 175, 188),
            };

            public static Theme TokyoNight { get; } = new Theme("Tokyo Night")
            {
                GreyBackground = Color.FromArgb(20, 21, 30),
                HeaderBackground = Color.FromArgb(16, 16, 24),
                MediumBackground = Color.FromArgb(28, 32, 48),
                LightBackground = Color.FromArgb(36, 40, 58),
                LighterBackground = Color.FromArgb(46, 50, 70),
                LightestBackground = Color.FromArgb(130, 134, 155),
                DarkBackground = Color.FromArgb(12, 12, 18),
                BlueBackground = Color.FromArgb(30, 34, 52),
                DarkBlueBackground = Color.FromArgb(22, 24, 36),
                LightBorder = Color.FromArgb(42, 46, 68),
                DarkBorder = Color.FromArgb(26, 30, 42),
                DarkBlueBorder = Color.FromArgb(28, 32, 48),
                LightBlueBorder = Color.FromArgb(44, 50, 72),
                LightText = Color.FromArgb(188, 196, 240),
                DisabledText = Color.FromArgb(100, 105, 130),
                BlueHighlight = Color.FromArgb(180, 148, 242),
                BlueSelection = Color.FromArgb(115, 150, 240),
                GreyHighlight = Color.FromArgb(100, 108, 130),
                GreySelection = Color.FromArgb(42, 46, 68),
                DarkGreySelection = Color.FromArgb(28, 32, 48),
                ActiveControl = Color.FromArgb(220, 170, 95),
                MenuItemToggledOnFill = Color.FromArgb(48, 44, 68),
                MenuItemToggledOnBorder = Color.FromArgb(180, 148, 242),
            };

            public static Theme Catppuccin { get; } = new Theme("Catppuccin")
            {
                GreyBackground = Color.FromArgb(20, 20, 32),
                HeaderBackground = Color.FromArgb(16, 16, 26),
                MediumBackground = Color.FromArgb(34, 35, 50),
                LightBackground = Color.FromArgb(40, 42, 58),
                LighterBackground = Color.FromArgb(50, 52, 70),
                LightestBackground = Color.FromArgb(135, 138, 158),
                DarkBackground = Color.FromArgb(12, 12, 22),
                BlueBackground = Color.FromArgb(36, 38, 56),
                DarkBlueBackground = Color.FromArgb(26, 26, 40),
                LightBorder = Color.FromArgb(50, 52, 68),
                DarkBorder = Color.FromArgb(32, 34, 44),
                DarkBlueBorder = Color.FromArgb(34, 36, 52),
                LightBlueBorder = Color.FromArgb(52, 54, 72),
                LightText = Color.FromArgb(200, 208, 240),
                DisabledText = Color.FromArgb(105, 110, 132),
                BlueHighlight = Color.FromArgb(175, 185, 250),
                BlueSelection = Color.FromArgb(130, 172, 245),
                GreyHighlight = Color.FromArgb(108, 112, 140),
                GreySelection = Color.FromArgb(50, 52, 68),
                DarkGreySelection = Color.FromArgb(34, 35, 50),
                ActiveControl = Color.FromArgb(240, 130, 160),
                MenuItemToggledOnFill = Color.FromArgb(52, 45, 65),
                MenuItemToggledOnBorder = Color.FromArgb(175, 185, 250),
            };

            public static Theme Gruvbox { get; } = new Theme("Gruvbox")
            {
                GreyBackground = Color.FromArgb(28, 28, 26),
                HeaderBackground = Color.FromArgb(20, 22, 21),
                MediumBackground = Color.FromArgb(35, 34, 32),
                LightBackground = Color.FromArgb(42, 38, 35),
                LighterBackground = Color.FromArgb(56, 50, 45),
                LightestBackground = Color.FromArgb(135, 120, 105),
                DarkBackground = Color.FromArgb(18, 18, 16),
                BlueBackground = Color.FromArgb(38, 36, 33),
                DarkBlueBackground = Color.FromArgb(26, 24, 22),
                LightBorder = Color.FromArgb(56, 50, 45),
                DarkBorder = Color.FromArgb(35, 34, 32),
                DarkBlueBorder = Color.FromArgb(34, 32, 28),
                LightBlueBorder = Color.FromArgb(60, 54, 48),
                LightText = Color.FromArgb(228, 212, 170),
                DisabledText = Color.FromArgb(118, 108, 88),
                BlueHighlight = Color.FromArgb(248, 185, 40),
                BlueSelection = Color.FromArgb(60, 120, 125),
                GreyHighlight = Color.FromArgb(118, 100, 82),
                GreySelection = Color.FromArgb(56, 50, 45),
                DarkGreySelection = Color.FromArgb(35, 34, 32),
                ActiveControl = Color.FromArgb(168, 90, 125),
                MenuItemToggledOnFill = Color.FromArgb(55, 45, 34),
                MenuItemToggledOnBorder = Color.FromArgb(248, 185, 40),
            };

            public static Theme Monokai { get; } = new Theme("Monokai")
            {
                GreyBackground = Color.FromArgb(28, 29, 24),
                HeaderBackground = Color.FromArgb(22, 23, 18),
                MediumBackground = Color.FromArgb(34, 34, 30),
                LightBackground = Color.FromArgb(42, 42, 36),
                LighterBackground = Color.FromArgb(54, 54, 46),
                LightestBackground = Color.FromArgb(128, 128, 118),
                DarkBackground = Color.FromArgb(18, 19, 15),
                BlueBackground = Color.FromArgb(36, 36, 32),
                DarkBlueBackground = Color.FromArgb(26, 26, 22),
                LightBorder = Color.FromArgb(46, 45, 38),
                DarkBorder = Color.FromArgb(32, 32, 28),
                DarkBlueBorder = Color.FromArgb(32, 32, 28),
                LightBlueBorder = Color.FromArgb(50, 48, 40),
                LightText = Color.FromArgb(244, 244, 238),
                DisabledText = Color.FromArgb(112, 112, 105),
                BlueHighlight = Color.FromArgb(164, 225, 42),
                BlueSelection = Color.FromArgb(248, 35, 110),
                GreyHighlight = Color.FromArgb(108, 108, 100),
                GreySelection = Color.FromArgb(46, 45, 38),
                DarkGreySelection = Color.FromArgb(34, 34, 30),
                ActiveControl = Color.FromArgb(95, 210, 235),
                MenuItemToggledOnFill = Color.FromArgb(55, 50, 40),
                MenuItemToggledOnBorder = Color.FromArgb(164, 225, 42),
            };

            public static Theme BordeauxNoir { get; } = new Theme("Bordeaux Noir")
            {
                GreyBackground = Color.FromArgb(34, 27, 30),
                HeaderBackground = Color.FromArgb(30, 24, 27),
                MediumBackground = Color.FromArgb(43, 34, 38),
                LightBackground = Color.FromArgb(53, 42, 47),
                LighterBackground = Color.FromArgb(70, 55, 62),
                LightestBackground = Color.FromArgb(154, 137, 144),
                DarkBackground = Color.FromArgb(23, 18, 21),
                BlueBackground = Color.FromArgb(48, 34, 40),
                DarkBlueBackground = Color.FromArgb(38, 27, 32),
                LightBorder = Color.FromArgb(66, 52, 58),
                DarkBorder = Color.FromArgb(39, 31, 35),
                DarkBlueBorder = Color.FromArgb(55, 38, 45),
                LightBlueBorder = Color.FromArgb(83, 57, 68),
                LightText = Color.FromArgb(224, 216, 219),
                DisabledText = Color.FromArgb(137, 125, 130),
                BlueHighlight = Color.FromArgb(125, 68, 84),
                BlueSelection = Color.FromArgb(150, 76, 96),
                GreyHighlight = Color.FromArgb(96, 79, 85),
                GreySelection = Color.FromArgb(66, 45, 53),
                DarkGreySelection = Color.FromArgb(51, 35, 41),
                ActiveControl = Color.FromArgb(174, 106, 124),
                MenuItemToggledOnFill = Color.FromArgb(69, 42, 50),
                MenuItemToggledOnBorder = Color.FromArgb(158, 83, 103),
            };

            public static Theme MossCarbon { get; } = new Theme("Moss Carbon")
            {
                GreyBackground = Color.FromArgb(27, 32, 27),
                HeaderBackground = Color.FromArgb(24, 29, 24),
                MediumBackground = Color.FromArgb(35, 41, 35),
                LightBackground = Color.FromArgb(44, 51, 44),
                LighterBackground = Color.FromArgb(58, 67, 58),
                LightestBackground = Color.FromArgb(137, 150, 138),
                DarkBackground = Color.FromArgb(18, 22, 18),
                BlueBackground = Color.FromArgb(36, 47, 37),
                DarkBlueBackground = Color.FromArgb(29, 38, 30),
                LightBorder = Color.FromArgb(55, 65, 55),
                DarkBorder = Color.FromArgb(33, 39, 33),
                DarkBlueBorder = Color.FromArgb(40, 53, 41),
                LightBlueBorder = Color.FromArgb(63, 82, 65),
                LightText = Color.FromArgb(216, 222, 216),
                DisabledText = Color.FromArgb(128, 138, 129),
                BlueHighlight = Color.FromArgb(79, 111, 79),
                BlueSelection = Color.FromArgb(88, 127, 88),
                GreyHighlight = Color.FromArgb(81, 94, 81),
                GreySelection = Color.FromArgb(48, 61, 49),
                DarkGreySelection = Color.FromArgb(38, 49, 39),
                ActiveControl = Color.FromArgb(112, 147, 111),
                MenuItemToggledOnFill = Color.FromArgb(43, 60, 44),
                MenuItemToggledOnBorder = Color.FromArgb(92, 132, 91),
            };

            public static Theme CopperForge { get; } = new Theme("Copper Forge")
            {
                GreyBackground = Color.FromArgb(35, 30, 26),
                HeaderBackground = Color.FromArgb(31, 27, 23),
                MediumBackground = Color.FromArgb(45, 38, 32),
                LightBackground = Color.FromArgb(55, 47, 39),
                LighterBackground = Color.FromArgb(73, 62, 51),
                LightestBackground = Color.FromArgb(158, 143, 129),
                DarkBackground = Color.FromArgb(23, 20, 17),
                BlueBackground = Color.FromArgb(50, 39, 31),
                DarkBlueBackground = Color.FromArgb(40, 31, 25),
                LightBorder = Color.FromArgb(68, 57, 47),
                DarkBorder = Color.FromArgb(41, 35, 29),
                DarkBlueBorder = Color.FromArgb(56, 43, 33),
                LightBlueBorder = Color.FromArgb(87, 66, 49),
                LightText = Color.FromArgb(226, 219, 210),
                DisabledText = Color.FromArgb(140, 130, 119),
                BlueHighlight = Color.FromArgb(137, 89, 55),
                BlueSelection = Color.FromArgb(164, 105, 64),
                GreyHighlight = Color.FromArgb(101, 88, 75),
                GreySelection = Color.FromArgb(69, 53, 41),
                DarkGreySelection = Color.FromArgb(54, 42, 33),
                ActiveControl = Color.FromArgb(188, 133, 91),
                MenuItemToggledOnFill = Color.FromArgb(75, 52, 36),
                MenuItemToggledOnBorder = Color.FromArgb(172, 113, 70),
            };

            public static Theme Gunmetal { get; } = new Theme("Gunmetal")
            {
                GreyBackground = Color.FromArgb(28, 32, 35),
                HeaderBackground = Color.FromArgb(25, 29, 32),
                MediumBackground = Color.FromArgb(37, 42, 46),
                LightBackground = Color.FromArgb(46, 52, 57),
                LighterBackground = Color.FromArgb(61, 68, 74),
                LightestBackground = Color.FromArgb(143, 151, 158),
                DarkBackground = Color.FromArgb(19, 22, 24),
                BlueBackground = Color.FromArgb(39, 48, 55),
                DarkBlueBackground = Color.FromArgb(31, 39, 45),
                LightBorder = Color.FromArgb(58, 65, 71),
                DarkBorder = Color.FromArgb(35, 40, 44),
                DarkBlueBorder = Color.FromArgb(42, 53, 61),
                LightBlueBorder = Color.FromArgb(66, 81, 91),
                LightText = Color.FromArgb(218, 223, 227),
                DisabledText = Color.FromArgb(130, 138, 144),
                BlueHighlight = Color.FromArgb(83, 107, 126),
                BlueSelection = Color.FromArgb(96, 126, 152),
                GreyHighlight = Color.FromArgb(84, 94, 102),
                GreySelection = Color.FromArgb(49, 61, 69),
                DarkGreySelection = Color.FromArgb(39, 49, 56),
                ActiveControl = Color.FromArgb(126, 151, 171),
                MenuItemToggledOnFill = Color.FromArgb(43, 56, 65),
                MenuItemToggledOnBorder = Color.FromArgb(100, 133, 157),
            };

            public static Theme RoseAsh { get; } = new Theme("Rose Ash")
            {
                GreyBackground = Color.FromArgb(34, 29, 31),
                HeaderBackground = Color.FromArgb(30, 26, 28),
                MediumBackground = Color.FromArgb(43, 37, 40),
                LightBackground = Color.FromArgb(53, 45, 49),
                LighterBackground = Color.FromArgb(70, 59, 64),
                LightestBackground = Color.FromArgb(157, 142, 148),
                DarkBackground = Color.FromArgb(23, 20, 21),
                BlueBackground = Color.FromArgb(48, 38, 43),
                DarkBlueBackground = Color.FromArgb(38, 30, 34),
                LightBorder = Color.FromArgb(66, 56, 60),
                DarkBorder = Color.FromArgb(40, 34, 36),
                DarkBlueBorder = Color.FromArgb(54, 42, 48),
                LightBlueBorder = Color.FromArgb(83, 65, 73),
                LightText = Color.FromArgb(225, 217, 220),
                DisabledText = Color.FromArgb(139, 128, 132),
                BlueHighlight = Color.FromArgb(121, 81, 91),
                BlueSelection = Color.FromArgb(144, 96, 108),
                GreyHighlight = Color.FromArgb(99, 84, 89),
                GreySelection = Color.FromArgb(66, 50, 56),
                DarkGreySelection = Color.FromArgb(52, 40, 45),
                ActiveControl = Color.FromArgb(168, 120, 131),
                MenuItemToggledOnFill = Color.FromArgb(69, 47, 54),
                MenuItemToggledOnBorder = Color.FromArgb(151, 100, 113),
            };

            public static Theme AcidOlive { get; } = new Theme("Acid Olive")
            {
                GreyBackground = Color.FromArgb(30, 33, 25),
                HeaderBackground = Color.FromArgb(27, 29, 22),
                MediumBackground = Color.FromArgb(39, 43, 31),
                LightBackground = Color.FromArgb(48, 53, 38),
                LighterBackground = Color.FromArgb(64, 70, 50),
                LightestBackground = Color.FromArgb(145, 151, 126),
                DarkBackground = Color.FromArgb(20, 22, 16),
                BlueBackground = Color.FromArgb(42, 48, 31),
                DarkBlueBackground = Color.FromArgb(34, 39, 25),
                LightBorder = Color.FromArgb(60, 65, 47),
                DarkBorder = Color.FromArgb(36, 40, 29),
                DarkBlueBorder = Color.FromArgb(47, 55, 32),
                LightBlueBorder = Color.FromArgb(73, 84, 49),
                LightText = Color.FromArgb(220, 223, 207),
                DisabledText = Color.FromArgb(132, 137, 116),
                BlueHighlight = Color.FromArgb(100, 122, 62),
                BlueSelection = Color.FromArgb(118, 147, 74),
                GreyHighlight = Color.FromArgb(90, 96, 70),
                GreySelection = Color.FromArgb(55, 64, 39),
                DarkGreySelection = Color.FromArgb(44, 52, 31),
                ActiveControl = Color.FromArgb(144, 163, 102),
                MenuItemToggledOnFill = Color.FromArgb(57, 70, 37),
                MenuItemToggledOnBorder = Color.FromArgb(128, 153, 78),
            };

            public static Theme DeepCobalt { get; } = new Theme("Deep Cobalt")
            {
                GreyBackground = Color.FromArgb(24, 27, 34),
                HeaderBackground = Color.FromArgb(21, 24, 30),
                MediumBackground = Color.FromArgb(32, 36, 45),
                LightBackground = Color.FromArgb(40, 45, 56),
                LighterBackground = Color.FromArgb(53, 59, 73),
                LightestBackground = Color.FromArgb(136, 146, 164),
                DarkBackground = Color.FromArgb(16, 18, 23),
                BlueBackground = Color.FromArgb(30, 39, 58),
                DarkBlueBackground = Color.FromArgb(24, 31, 47),
                LightBorder = Color.FromArgb(51, 57, 69),
                DarkBorder = Color.FromArgb(30, 34, 42),
                DarkBlueBorder = Color.FromArgb(34, 45, 66),
                LightBlueBorder = Color.FromArgb(53, 69, 98),
                LightText = Color.FromArgb(215, 220, 230),
                DisabledText = Color.FromArgb(126, 134, 149),
                BlueHighlight = Color.FromArgb(64, 88, 143),
                BlueSelection = Color.FromArgb(70, 98, 164),
                GreyHighlight = Color.FromArgb(76, 84, 102),
                GreySelection = Color.FromArgb(42, 51, 72),
                DarkGreySelection = Color.FromArgb(33, 41, 58),
                ActiveControl = Color.FromArgb(103, 127, 184),
                MenuItemToggledOnFill = Color.FromArgb(34, 47, 76),
                MenuItemToggledOnBorder = Color.FromArgb(77, 105, 174),
            };

            public static Theme SepiaNoir { get; } = new Theme("Sepia Noir")
            {
                GreyBackground = Color.FromArgb(32, 29, 25),
                HeaderBackground = Color.FromArgb(28, 26, 22),
                MediumBackground = Color.FromArgb(41, 37, 32),
                LightBackground = Color.FromArgb(50, 45, 39),
                LighterBackground = Color.FromArgb(66, 59, 50),
                LightestBackground = Color.FromArgb(151, 141, 127),
                DarkBackground = Color.FromArgb(21, 19, 16),
                BlueBackground = Color.FromArgb(45, 39, 31),
                DarkBlueBackground = Color.FromArgb(36, 31, 25),
                LightBorder = Color.FromArgb(62, 55, 46),
                DarkBorder = Color.FromArgb(37, 34, 28),
                DarkBlueBorder = Color.FromArgb(51, 43, 34),
                LightBlueBorder = Color.FromArgb(78, 66, 51),
                LightText = Color.FromArgb(224, 218, 207),
                DisabledText = Color.FromArgb(136, 129, 118),
                BlueHighlight = Color.FromArgb(123, 100, 71),
                BlueSelection = Color.FromArgb(147, 116, 78),
                GreyHighlight = Color.FromArgb(96, 87, 74),
                GreySelection = Color.FromArgb(64, 53, 41),
                DarkGreySelection = Color.FromArgb(50, 42, 33),
                ActiveControl = Color.FromArgb(171, 143, 105),
                MenuItemToggledOnFill = Color.FromArgb(67, 52, 36),
                MenuItemToggledOnBorder = Color.FromArgb(154, 121, 82),
            };

            public static Theme CarbonBlue { get; } = new Theme("Carbon Blue")
            {
                GreyBackground = Color.FromArgb(28, 30, 32),
                HeaderBackground = Color.FromArgb(25, 27, 29),
                MediumBackground = Color.FromArgb(36, 38, 41),
                LightBackground = Color.FromArgb(46, 49, 52),
                LighterBackground = Color.FromArgb(61, 64, 68),
                LightestBackground = Color.FromArgb(145, 149, 154),
                DarkBackground = Color.FromArgb(20, 21, 23),
                BlueBackground = Color.FromArgb(37, 43, 51),
                DarkBlueBackground = Color.FromArgb(29, 34, 40),
                LightBorder = Color.FromArgb(57, 60, 64),
                DarkBorder = Color.FromArgb(34, 36, 39),
                DarkBlueBorder = Color.FromArgb(40, 47, 56),
                LightBlueBorder = Color.FromArgb(62, 73, 86),
                LightText = Color.FromArgb(220, 222, 225),
                DisabledText = Color.FromArgb(132, 136, 141),
                BlueHighlight = Color.FromArgb(76, 108, 145),
                BlueSelection = Color.FromArgb(72, 105, 153),
                GreyHighlight = Color.FromArgb(84, 89, 95),
                GreySelection = Color.FromArgb(49, 55, 62),
                DarkGreySelection = Color.FromArgb(40, 45, 50),
                ActiveControl = Color.FromArgb(111, 139, 171),
                MenuItemToggledOnFill = Color.FromArgb(39, 49, 61),
                MenuItemToggledOnBorder = Color.FromArgb(83, 117, 157),
            };

            public static Theme GraphiteCyan { get; } = new Theme("Graphite Cyan")
            {
                GreyBackground = Color.FromArgb(27, 30, 31),
                HeaderBackground = Color.FromArgb(24, 27, 28),
                MediumBackground = Color.FromArgb(35, 39, 40),
                LightBackground = Color.FromArgb(45, 49, 50),
                LighterBackground = Color.FromArgb(59, 64, 65),
                LightestBackground = Color.FromArgb(143, 150, 151),
                DarkBackground = Color.FromArgb(19, 21, 22),
                BlueBackground = Color.FromArgb(34, 45, 47),
                DarkBlueBackground = Color.FromArgb(27, 36, 38),
                LightBorder = Color.FromArgb(55, 61, 62),
                DarkBorder = Color.FromArgb(33, 37, 38),
                DarkBlueBorder = Color.FromArgb(37, 51, 53),
                LightBlueBorder = Color.FromArgb(58, 79, 81),
                LightText = Color.FromArgb(216, 224, 224),
                DisabledText = Color.FromArgb(128, 138, 138),
                BlueHighlight = Color.FromArgb(56, 128, 132),
                BlueSelection = Color.FromArgb(55, 142, 148),
                GreyHighlight = Color.FromArgb(80, 95, 96),
                GreySelection = Color.FromArgb(46, 61, 62),
                DarkGreySelection = Color.FromArgb(37, 49, 50),
                ActiveControl = Color.FromArgb(89, 161, 165),
                MenuItemToggledOnFill = Color.FromArgb(34, 61, 63),
                MenuItemToggledOnBorder = Color.FromArgb(64, 145, 149),
            };

            public static Theme SignalGreen { get; } = new Theme("Signal Green")
            {
                GreyBackground = Color.FromArgb(28, 30, 29),
                HeaderBackground = Color.FromArgb(25, 27, 26),
                MediumBackground = Color.FromArgb(36, 39, 37),
                LightBackground = Color.FromArgb(46, 50, 47),
                LighterBackground = Color.FromArgb(61, 65, 62),
                LightestBackground = Color.FromArgb(144, 151, 146),
                DarkBackground = Color.FromArgb(20, 22, 21),
                BlueBackground = Color.FromArgb(36, 46, 39),
                DarkBlueBackground = Color.FromArgb(29, 37, 32),
                LightBorder = Color.FromArgb(56, 62, 58),
                DarkBorder = Color.FromArgb(34, 38, 35),
                DarkBlueBorder = Color.FromArgb(40, 51, 43),
                LightBlueBorder = Color.FromArgb(63, 79, 67),
                LightText = Color.FromArgb(218, 223, 219),
                DisabledText = Color.FromArgb(130, 138, 132),
                BlueHighlight = Color.FromArgb(71, 119, 80),
                BlueSelection = Color.FromArgb(76, 137, 87),
                GreyHighlight = Color.FromArgb(82, 94, 84),
                GreySelection = Color.FromArgb(48, 60, 51),
                DarkGreySelection = Color.FromArgb(39, 48, 41),
                ActiveControl = Color.FromArgb(105, 156, 113),
                MenuItemToggledOnFill = Color.FromArgb(39, 61, 43),
                MenuItemToggledOnBorder = Color.FromArgb(84, 143, 94),
            };

            public static Theme RoyalViolet { get; } = new Theme("Royal Violet")
            {
                GreyBackground = Color.FromArgb(29, 28, 31),
                HeaderBackground = Color.FromArgb(26, 25, 28),
                MediumBackground = Color.FromArgb(38, 36, 40),
                LightBackground = Color.FromArgb(48, 46, 51),
                LighterBackground = Color.FromArgb(63, 60, 67),
                LightestBackground = Color.FromArgb(149, 144, 155),
                DarkBackground = Color.FromArgb(20, 20, 22),
                BlueBackground = Color.FromArgb(40, 35, 48),
                DarkBlueBackground = Color.FromArgb(32, 29, 39),
                LightBorder = Color.FromArgb(59, 56, 63),
                DarkBorder = Color.FromArgb(36, 34, 38),
                DarkBlueBorder = Color.FromArgb(45, 39, 54),
                LightBlueBorder = Color.FromArgb(69, 60, 83),
                LightText = Color.FromArgb(222, 219, 225),
                DisabledText = Color.FromArgb(136, 131, 140),
                BlueHighlight = Color.FromArgb(101, 78, 145),
                BlueSelection = Color.FromArgb(112, 85, 165),
                GreyHighlight = Color.FromArgb(91, 84, 97),
                GreySelection = Color.FromArgb(57, 49, 66),
                DarkGreySelection = Color.FromArgb(45, 40, 53),
                ActiveControl = Color.FromArgb(143, 117, 184),
                MenuItemToggledOnFill = Color.FromArgb(55, 44, 70),
                MenuItemToggledOnBorder = Color.FromArgb(122, 94, 172),
            };

            public static Theme BlackCherry { get; } = new Theme("Black Cherry")
            {
                GreyBackground = Color.FromArgb(30, 28, 29),
                HeaderBackground = Color.FromArgb(27, 25, 26),
                MediumBackground = Color.FromArgb(39, 36, 38),
                LightBackground = Color.FromArgb(49, 46, 48),
                LighterBackground = Color.FromArgb(65, 60, 63),
                LightestBackground = Color.FromArgb(151, 144, 147),
                DarkBackground = Color.FromArgb(21, 19, 20),
                BlueBackground = Color.FromArgb(43, 34, 38),
                DarkBlueBackground = Color.FromArgb(34, 27, 31),
                LightBorder = Color.FromArgb(61, 56, 59),
                DarkBorder = Color.FromArgb(37, 34, 36),
                DarkBlueBorder = Color.FromArgb(48, 38, 43),
                LightBlueBorder = Color.FromArgb(75, 57, 66),
                LightText = Color.FromArgb(225, 219, 221),
                DisabledText = Color.FromArgb(138, 130, 133),
                BlueHighlight = Color.FromArgb(128, 66, 82),
                BlueSelection = Color.FromArgb(151, 72, 93),
                GreyHighlight = Color.FromArgb(95, 83, 87),
                GreySelection = Color.FromArgb(62, 47, 53),
                DarkGreySelection = Color.FromArgb(49, 38, 43),
                ActiveControl = Color.FromArgb(177, 104, 122),
                MenuItemToggledOnFill = Color.FromArgb(67, 40, 48),
                MenuItemToggledOnBorder = Color.FromArgb(158, 79, 99),
            };

            public static Theme AmberNight { get; } = new Theme("Amber Night")
            {
                GreyBackground = Color.FromArgb(30, 29, 27),
                HeaderBackground = Color.FromArgb(27, 26, 24),
                MediumBackground = Color.FromArgb(39, 38, 35),
                LightBackground = Color.FromArgb(49, 48, 44),
                LighterBackground = Color.FromArgb(65, 63, 58),
                LightestBackground = Color.FromArgb(151, 147, 138),
                DarkBackground = Color.FromArgb(21, 20, 19),
                BlueBackground = Color.FromArgb(43, 39, 32),
                DarkBlueBackground = Color.FromArgb(34, 31, 26),
                LightBorder = Color.FromArgb(61, 59, 54),
                DarkBorder = Color.FromArgb(37, 36, 33),
                DarkBlueBorder = Color.FromArgb(49, 43, 34),
                LightBlueBorder = Color.FromArgb(76, 66, 49),
                LightText = Color.FromArgb(225, 222, 214),
                DisabledText = Color.FromArgb(138, 134, 125),
                BlueHighlight = Color.FromArgb(147, 110, 55),
                BlueSelection = Color.FromArgb(174, 127, 59),
                GreyHighlight = Color.FromArgb(96, 89, 76),
                GreySelection = Color.FromArgb(63, 55, 42),
                DarkGreySelection = Color.FromArgb(50, 44, 35),
                ActiveControl = Color.FromArgb(194, 151, 86),
                MenuItemToggledOnFill = Color.FromArgb(70, 54, 31),
                MenuItemToggledOnBorder = Color.FromArgb(181, 135, 65),
            };

            public static Theme IceSteel { get; } = new Theme("Ice Steel")
            {
                GreyBackground = Color.FromArgb(28, 31, 34),
                HeaderBackground = Color.FromArgb(25, 28, 31),
                MediumBackground = Color.FromArgb(36, 40, 44),
                LightBackground = Color.FromArgb(46, 51, 56),
                LighterBackground = Color.FromArgb(61, 67, 73),
                LightestBackground = Color.FromArgb(148, 157, 165),
                DarkBackground = Color.FromArgb(19, 22, 25),
                BlueBackground = Color.FromArgb(38, 46, 54),
                DarkBlueBackground = Color.FromArgb(30, 37, 44),
                LightBorder = Color.FromArgb(57, 64, 70),
                DarkBorder = Color.FromArgb(34, 39, 43),
                DarkBlueBorder = Color.FromArgb(41, 50, 60),
                LightBlueBorder = Color.FromArgb(65, 78, 91),
                LightText = Color.FromArgb(220, 226, 231),
                DisabledText = Color.FromArgb(131, 140, 147),
                BlueHighlight = Color.FromArgb(91, 119, 143),
                BlueSelection = Color.FromArgb(103, 137, 165),
                GreyHighlight = Color.FromArgb(84, 96, 106),
                GreySelection = Color.FromArgb(50, 62, 72),
                DarkGreySelection = Color.FromArgb(40, 50, 59),
                ActiveControl = Color.FromArgb(132, 158, 180),
                MenuItemToggledOnFill = Color.FromArgb(43, 56, 67),
                MenuItemToggledOnBorder = Color.FromArgb(106, 138, 165),
            };

            public static Theme NeonAzure { get; } = new Theme("Neon Azure")
            {
                GreyBackground = Color.FromArgb(25, 27, 30),
                HeaderBackground = Color.FromArgb(22, 24, 27),
                MediumBackground = Color.FromArgb(33, 36, 40),
                LightBackground = Color.FromArgb(43, 47, 52),
                LighterBackground = Color.FromArgb(57, 62, 68),
                LightestBackground = Color.FromArgb(140, 147, 155),
                DarkBackground = Color.FromArgb(17, 19, 22),
                BlueBackground = Color.FromArgb(31, 42, 55),
                DarkBlueBackground = Color.FromArgb(25, 34, 45),
                LightBorder = Color.FromArgb(53, 58, 64),
                DarkBorder = Color.FromArgb(31, 35, 39),
                DarkBlueBorder = Color.FromArgb(34, 48, 64),
                LightBlueBorder = Color.FromArgb(52, 76, 99),
                LightText = Color.FromArgb(220, 224, 229),
                DisabledText = Color.FromArgb(128, 134, 141),
                BlueHighlight = Color.FromArgb(43, 119, 186),
                BlueSelection = Color.FromArgb(45, 129, 213),
                GreyHighlight = Color.FromArgb(78, 89, 100),
                GreySelection = Color.FromArgb(42, 57, 72),
                DarkGreySelection = Color.FromArgb(34, 46, 59),
                ActiveControl = Color.FromArgb(83, 153, 215),
                MenuItemToggledOnFill = Color.FromArgb(31, 55, 78),
                MenuItemToggledOnBorder = Color.FromArgb(51, 132, 204),
            };

            public static Theme MutedRose { get; } = new Theme("Muted Rose")
            {
                GreyBackground = Color.FromArgb(30, 29, 30),
                HeaderBackground = Color.FromArgb(27, 26, 27),
                MediumBackground = Color.FromArgb(39, 37, 39),
                LightBackground = Color.FromArgb(49, 47, 49),
                LighterBackground = Color.FromArgb(65, 62, 65),
                LightestBackground = Color.FromArgb(151, 146, 150),
                DarkBackground = Color.FromArgb(21, 20, 21),
                BlueBackground = Color.FromArgb(42, 36, 39),
                DarkBlueBackground = Color.FromArgb(34, 29, 32),
                LightBorder = Color.FromArgb(61, 58, 60),
                DarkBorder = Color.FromArgb(37, 35, 36),
                DarkBlueBorder = Color.FromArgb(47, 41, 44),
                LightBlueBorder = Color.FromArgb(73, 63, 68),
                LightText = Color.FromArgb(224, 220, 222),
                DisabledText = Color.FromArgb(137, 132, 135),
                BlueHighlight = Color.FromArgb(117, 83, 93),
                BlueSelection = Color.FromArgb(137, 91, 105),
                GreyHighlight = Color.FromArgb(94, 87, 90),
                GreySelection = Color.FromArgb(61, 52, 56),
                DarkGreySelection = Color.FromArgb(49, 42, 45),
                ActiveControl = Color.FromArgb(162, 119, 131),
                MenuItemToggledOnFill = Color.FromArgb(63, 46, 52),
                MenuItemToggledOnBorder = Color.FromArgb(144, 96, 110),
            };

            public static Theme LimeTerminal { get; } = new Theme("Lime Terminal")
            {
                GreyBackground = Color.FromArgb(29, 30, 27),
                HeaderBackground = Color.FromArgb(26, 27, 24),
                MediumBackground = Color.FromArgb(37, 39, 34),
                LightBackground = Color.FromArgb(47, 50, 43),
                LighterBackground = Color.FromArgb(62, 65, 56),
                LightestBackground = Color.FromArgb(148, 152, 135),
                DarkBackground = Color.FromArgb(20, 21, 18),
                BlueBackground = Color.FromArgb(39, 45, 31),
                DarkBlueBackground = Color.FromArgb(31, 36, 25),
                LightBorder = Color.FromArgb(59, 62, 53),
                DarkBorder = Color.FromArgb(35, 38, 32),
                DarkBlueBorder = Color.FromArgb(43, 50, 33),
                LightBlueBorder = Color.FromArgb(67, 77, 50),
                LightText = Color.FromArgb(221, 224, 211),
                DisabledText = Color.FromArgb(133, 137, 122),
                BlueHighlight = Color.FromArgb(105, 128, 64),
                BlueSelection = Color.FromArgb(123, 151, 72),
                GreyHighlight = Color.FromArgb(88, 94, 75),
                GreySelection = Color.FromArgb(55, 63, 42),
                DarkGreySelection = Color.FromArgb(44, 51, 34),
                ActiveControl = Color.FromArgb(146, 166, 101),
                MenuItemToggledOnFill = Color.FromArgb(56, 68, 36),
                MenuItemToggledOnBorder = Color.FromArgb(130, 154, 76),
            };

            // ── Grayscale family ─────────────────────────────────
            // Slots not specified in the original proposal (blue-tinted
            // backgrounds/borders, menu toggles) are derived from each theme's
            // grayscale ramp.

            public static Theme PitchBlack { get; } = new Theme("Pitch Black")
            {
                GreyBackground = Color.FromArgb(8, 8, 8),
                HeaderBackground = Color.FromArgb(6, 6, 6),
                MediumBackground = Color.FromArgb(16, 16, 16),
                LightBackground = Color.FromArgb(24, 24, 24),
                LighterBackground = Color.FromArgb(36, 36, 36),
                LightestBackground = Color.FromArgb(119, 119, 119),
                DarkBackground = Color.FromArgb(3, 3, 3),
                BlueBackground = Color.FromArgb(14, 14, 14),
                DarkBlueBackground = Color.FromArgb(9, 9, 9),
                LightBorder = Color.FromArgb(48, 48, 48),
                DarkBorder = Color.FromArgb(22, 22, 22),
                DarkBlueBorder = Color.FromArgb(30, 30, 30),
                LightBlueBorder = Color.FromArgb(60, 60, 60),
                LightText = Color.FromArgb(216, 216, 216),
                DisabledText = Color.FromArgb(116, 116, 116),
                BlueHighlight = Color.FromArgb(102, 102, 102),
                BlueSelection = Color.FromArgb(85, 85, 85),
                GreyHighlight = Color.FromArgb(72, 72, 72),
                GreySelection = Color.FromArgb(56, 56, 56),
                DarkGreySelection = Color.FromArgb(41, 41, 41),
                ActiveControl = Color.FromArgb(146, 146, 146),
                MenuItemToggledOnFill = Color.FromArgb(42, 42, 42),
                MenuItemToggledOnBorder = Color.FromArgb(102, 102, 102),
            };

            public static Theme Carbon { get; } = new Theme("Carbon")
            {
                GreyBackground = Color.FromArgb(20, 20, 20),
                HeaderBackground = Color.FromArgb(16, 16, 16),
                MediumBackground = Color.FromArgb(28, 28, 28),
                LightBackground = Color.FromArgb(38, 38, 38),
                LighterBackground = Color.FromArgb(52, 52, 52),
                LightestBackground = Color.FromArgb(130, 130, 130),
                DarkBackground = Color.FromArgb(12, 12, 12),
                BlueBackground = Color.FromArgb(24, 24, 24),
                DarkBlueBackground = Color.FromArgb(16, 16, 16),
                LightBorder = Color.FromArgb(60, 60, 60),
                DarkBorder = Color.FromArgb(34, 34, 34),
                DarkBlueBorder = Color.FromArgb(42, 42, 42),
                LightBlueBorder = Color.FromArgb(72, 72, 72),
                LightText = Color.FromArgb(218, 218, 218),
                DisabledText = Color.FromArgb(128, 128, 128),
                BlueHighlight = Color.FromArgb(112, 112, 112),
                BlueSelection = Color.FromArgb(98, 98, 98),
                GreyHighlight = Color.FromArgb(85, 85, 85),
                GreySelection = Color.FromArgb(68, 68, 68),
                DarkGreySelection = Color.FromArgb(54, 54, 54),
                ActiveControl = Color.FromArgb(156, 156, 156),
                MenuItemToggledOnFill = Color.FromArgb(48, 48, 48),
                MenuItemToggledOnBorder = Color.FromArgb(112, 112, 112),
            };

            public static Theme Graphite { get; } = new Theme("Graphite")
            {
                GreyBackground = Color.FromArgb(32, 32, 32),
                HeaderBackground = Color.FromArgb(27, 27, 27),
                MediumBackground = Color.FromArgb(41, 41, 41),
                LightBackground = Color.FromArgb(52, 52, 52),
                LighterBackground = Color.FromArgb(68, 68, 68),
                LightestBackground = Color.FromArgb(146, 146, 146),
                DarkBackground = Color.FromArgb(22, 22, 22),
                BlueBackground = Color.FromArgb(37, 37, 37),
                DarkBlueBackground = Color.FromArgb(28, 28, 28),
                LightBorder = Color.FromArgb(75, 75, 75),
                DarkBorder = Color.FromArgb(41, 41, 41),
                DarkBlueBorder = Color.FromArgb(50, 50, 50),
                LightBlueBorder = Color.FromArgb(88, 88, 88),
                LightText = Color.FromArgb(222, 222, 222),
                DisabledText = Color.FromArgb(137, 137, 137),
                BlueHighlight = Color.FromArgb(124, 124, 124),
                BlueSelection = Color.FromArgb(112, 112, 112),
                GreyHighlight = Color.FromArgb(98, 98, 98),
                GreySelection = Color.FromArgb(80, 80, 80),
                DarkGreySelection = Color.FromArgb(66, 66, 66),
                ActiveControl = Color.FromArgb(170, 170, 170),
                MenuItemToggledOnFill = Color.FromArgb(58, 58, 58),
                MenuItemToggledOnBorder = Color.FromArgb(124, 124, 124),
            };

            public static Theme SlateGrey { get; } = new Theme("Slate Grey")
            {
                GreyBackground = Color.FromArgb(44, 44, 44),
                HeaderBackground = Color.FromArgb(38, 38, 38),
                MediumBackground = Color.FromArgb(53, 53, 53),
                LightBackground = Color.FromArgb(66, 66, 66),
                LighterBackground = Color.FromArgb(82, 82, 82),
                LightestBackground = Color.FromArgb(160, 160, 160),
                DarkBackground = Color.FromArgb(32, 32, 32),
                BlueBackground = Color.FromArgb(49, 49, 49),
                DarkBlueBackground = Color.FromArgb(38, 38, 38),
                LightBorder = Color.FromArgb(91, 91, 91),
                DarkBorder = Color.FromArgb(51, 51, 51),
                DarkBlueBorder = Color.FromArgb(61, 61, 61),
                LightBlueBorder = Color.FromArgb(104, 104, 104),
                LightText = Color.FromArgb(226, 226, 226),
                DisabledText = Color.FromArgb(150, 150, 150),
                BlueHighlight = Color.FromArgb(137, 137, 137),
                BlueSelection = Color.FromArgb(128, 128, 128),
                GreyHighlight = Color.FromArgb(112, 112, 112),
                GreySelection = Color.FromArgb(93, 93, 93),
                DarkGreySelection = Color.FromArgb(77, 77, 77),
                ActiveControl = Color.FromArgb(181, 181, 181),
                MenuItemToggledOnFill = Color.FromArgb(70, 70, 70),
                MenuItemToggledOnBorder = Color.FromArgb(137, 137, 137),
            };

            public static Theme Steel { get; } = new Theme("Steel")
            {
                GreyBackground = Color.FromArgb(56, 56, 56),
                HeaderBackground = Color.FromArgb(48, 48, 48),
                MediumBackground = Color.FromArgb(65, 65, 65),
                LightBackground = Color.FromArgb(80, 80, 80),
                LighterBackground = Color.FromArgb(96, 96, 96),
                LightestBackground = Color.FromArgb(172, 172, 172),
                DarkBackground = Color.FromArgb(42, 42, 42),
                BlueBackground = Color.FromArgb(61, 61, 61),
                DarkBlueBackground = Color.FromArgb(50, 50, 50),
                LightBorder = Color.FromArgb(105, 105, 105),
                DarkBorder = Color.FromArgb(64, 64, 64),
                DarkBlueBorder = Color.FromArgb(74, 74, 74),
                LightBlueBorder = Color.FromArgb(118, 118, 118),
                LightText = Color.FromArgb(232, 232, 232),
                DisabledText = Color.FromArgb(162, 162, 162),
                BlueHighlight = Color.FromArgb(156, 156, 156),
                BlueSelection = Color.FromArgb(146, 146, 146),
                GreyHighlight = Color.FromArgb(128, 128, 128),
                GreySelection = Color.FromArgb(107, 107, 107),
                DarkGreySelection = Color.FromArgb(91, 91, 91),
                ActiveControl = Color.FromArgb(192, 192, 192),
                MenuItemToggledOnFill = Color.FromArgb(82, 82, 82),
                MenuItemToggledOnBorder = Color.FromArgb(156, 156, 156),
            };

            public static Theme Ash { get; } = new Theme("Ash")
            {
                GreyBackground = Color.FromArgb(72, 72, 72),
                HeaderBackground = Color.FromArgb(62, 62, 62),
                MediumBackground = Color.FromArgb(83, 83, 83),
                LightBackground = Color.FromArgb(96, 96, 96),
                LighterBackground = Color.FromArgb(112, 112, 112),
                LightestBackground = Color.FromArgb(186, 186, 186),
                DarkBackground = Color.FromArgb(54, 54, 54),
                BlueBackground = Color.FromArgb(79, 79, 79),
                DarkBlueBackground = Color.FromArgb(66, 66, 66),
                LightBorder = Color.FromArgb(120, 120, 120),
                DarkBorder = Color.FromArgb(76, 76, 76),
                DarkBlueBorder = Color.FromArgb(90, 90, 90),
                LightBlueBorder = Color.FromArgb(136, 136, 136),
                LightText = Color.FromArgb(238, 238, 238),
                DisabledText = Color.FromArgb(176, 176, 176),
                BlueHighlight = Color.FromArgb(174, 174, 174),
                BlueSelection = Color.FromArgb(164, 164, 164),
                GreyHighlight = Color.FromArgb(146, 146, 146),
                GreySelection = Color.FromArgb(122, 122, 122),
                DarkGreySelection = Color.FromArgb(106, 106, 106),
                ActiveControl = Color.FromArgb(204, 204, 204),
                MenuItemToggledOnFill = Color.FromArgb(100, 100, 100),
                MenuItemToggledOnBorder = Color.FromArgb(174, 174, 174),
            };

            public static Theme SilverSmoke { get; } = new Theme("Silver Smoke")
            {
                GreyBackground = Color.FromArgb(96, 96, 96),
                HeaderBackground = Color.FromArgb(85, 85, 85),
                MediumBackground = Color.FromArgb(107, 107, 107),
                LightBackground = Color.FromArgb(120, 120, 120),
                LighterBackground = Color.FromArgb(137, 137, 137),
                LightestBackground = Color.FromArgb(204, 204, 204),
                DarkBackground = Color.FromArgb(75, 75, 75),
                BlueBackground = Color.FromArgb(103, 103, 103),
                DarkBlueBackground = Color.FromArgb(88, 88, 88),
                LightBorder = Color.FromArgb(145, 145, 145),
                DarkBorder = Color.FromArgb(94, 94, 94),
                DarkBlueBorder = Color.FromArgb(108, 108, 108),
                LightBlueBorder = Color.FromArgb(160, 160, 160),
                LightText = Color.FromArgb(244, 244, 244),
                DisabledText = Color.FromArgb(190, 190, 190),
                BlueHighlight = Color.FromArgb(194, 194, 194),
                BlueSelection = Color.FromArgb(184, 184, 184),
                GreyHighlight = Color.FromArgb(166, 166, 166),
                GreySelection = Color.FromArgb(140, 140, 140),
                DarkGreySelection = Color.FromArgb(124, 124, 124),
                ActiveControl = Color.FromArgb(222, 222, 222),
                MenuItemToggledOnFill = Color.FromArgb(120, 120, 120),
                MenuItemToggledOnBorder = Color.FromArgb(194, 194, 194),
            };

            // ── Light family (inverted text polarity) ────────────

            public static Theme PewterLight { get; } = new Theme("Pewter Light")
            {
                GreyBackground = Color.FromArgb(136, 136, 136),
                HeaderBackground = Color.FromArgb(123, 123, 123),
                MediumBackground = Color.FromArgb(148, 148, 148),
                LightBackground = Color.FromArgb(160, 160, 160),
                LighterBackground = Color.FromArgb(176, 176, 176),
                LightestBackground = Color.FromArgb(224, 224, 224),
                DarkBackground = Color.FromArgb(112, 112, 112),
                BlueBackground = Color.FromArgb(144, 144, 144),
                DarkBlueBackground = Color.FromArgb(128, 128, 128),
                LightBorder = Color.FromArgb(168, 168, 168),
                DarkBorder = Color.FromArgb(124, 124, 124),
                DarkBlueBorder = Color.FromArgb(136, 136, 136),
                LightBlueBorder = Color.FromArgb(180, 180, 180),
                LightText = Color.FromArgb(24, 24, 24),
                DisabledText = Color.FromArgb(85, 85, 85),
                BlueHighlight = Color.FromArgb(104, 104, 104),
                BlueSelection = Color.FromArgb(85, 85, 85),
                GreyHighlight = Color.FromArgb(126, 126, 126),
                GreySelection = Color.FromArgb(110, 110, 110),
                DarkGreySelection = Color.FromArgb(96, 96, 96),
                ActiveControl = Color.FromArgb(64, 64, 64),
                MenuItemToggledOnFill = Color.FromArgb(158, 158, 158),
                MenuItemToggledOnBorder = Color.FromArgb(104, 104, 104),
            };

            public static Theme Frost { get; } = new Theme("Frost")
            {
                GreyBackground = Color.FromArgb(196, 196, 196),
                HeaderBackground = Color.FromArgb(182, 182, 182),
                MediumBackground = Color.FromArgb(204, 204, 204),
                LightBackground = Color.FromArgb(214, 214, 214),
                LighterBackground = Color.FromArgb(226, 226, 226),
                LightestBackground = Color.FromArgb(244, 244, 244),
                DarkBackground = Color.FromArgb(168, 168, 168),
                BlueBackground = Color.FromArgb(202, 202, 202),
                DarkBlueBackground = Color.FromArgb(188, 188, 188),
                LightBorder = Color.FromArgb(220, 220, 220),
                DarkBorder = Color.FromArgb(182, 182, 182),
                DarkBlueBorder = Color.FromArgb(194, 194, 194),
                LightBlueBorder = Color.FromArgb(226, 226, 226),
                LightText = Color.FromArgb(24, 24, 24),
                DisabledText = Color.FromArgb(104, 104, 104),
                BlueHighlight = Color.FromArgb(119, 119, 119),
                BlueSelection = Color.FromArgb(104, 104, 104),
                GreyHighlight = Color.FromArgb(162, 162, 162),
                GreySelection = Color.FromArgb(148, 148, 148),
                DarkGreySelection = Color.FromArgb(136, 136, 136),
                ActiveControl = Color.FromArgb(68, 68, 68),
                MenuItemToggledOnFill = Color.FromArgb(208, 208, 208),
                MenuItemToggledOnBorder = Color.FromArgb(119, 119, 119),
            };

            public static Theme Porcelain { get; } = new Theme("Porcelain")
            {
                GreyBackground = Color.FromArgb(238, 238, 238),
                HeaderBackground = Color.FromArgb(226, 226, 226),
                MediumBackground = Color.FromArgb(242, 242, 242),
                LightBackground = Color.FromArgb(250, 250, 250),
                LighterBackground = Color.FromArgb(255, 255, 255),
                LightestBackground = Color.FromArgb(255, 255, 255),
                DarkBackground = Color.FromArgb(212, 212, 212),
                BlueBackground = Color.FromArgb(238, 238, 238),
                DarkBlueBackground = Color.FromArgb(230, 230, 230),
                LightBorder = Color.FromArgb(232, 232, 232),
                DarkBorder = Color.FromArgb(214, 214, 214),
                DarkBlueBorder = Color.FromArgb(222, 222, 222),
                LightBlueBorder = Color.FromArgb(244, 244, 244),
                LightText = Color.FromArgb(22, 22, 22),
                DisabledText = Color.FromArgb(119, 119, 119),
                BlueHighlight = Color.FromArgb(104, 104, 104),
                BlueSelection = Color.FromArgb(85, 85, 85),
                GreyHighlight = Color.FromArgb(208, 208, 208),
                GreySelection = Color.FromArgb(184, 184, 184),
                DarkGreySelection = Color.FromArgb(165, 165, 165),
                ActiveControl = Color.FromArgb(56, 56, 56),
                MenuItemToggledOnFill = Color.FromArgb(226, 226, 226),
                MenuItemToggledOnBorder = Color.FromArgb(104, 104, 104),
            };

            // ── Curated additions ─────────────────────────────────

            public static Theme Ps4Midnight { get; } = Create3D(
                "PS4 Midnight", ThemeDepthStyle.Flat, Color.FromArgb(12, 24, 50),
                Color.FromArgb(0, 112, 209), AccentDarkCategory);

            public static Theme OledPurple { get; } = Create3D(
                "OLED Purple", ThemeDepthStyle.Flat, Color.FromArgb(12, 8, 20),
                Color.FromArgb(151, 91, 255), DeepDarkCategory);

            public static Theme GunmetalOrange { get; } = Create3D(
                "Gunmetal Orange", ThemeDepthStyle.Flat, Color.FromArgb(38, 39, 41),
                Color.FromArgb(218, 112, 32), AccentDarkCategory);

            public static Theme ForestNight { get; } = Create3D(
                "Forest Night", ThemeDepthStyle.Flat, Color.FromArgb(13, 29, 24),
                Color.FromArgb(32, 165, 120), AccentDarkCategory);

            public static Theme WarmPaper { get; } = Create3D(
                "Warm Paper", ThemeDepthStyle.Flat, Color.FromArgb(244, 238, 225),
                Color.FromArgb(154, 91, 53), LightCategory);

            // ── High contrast pair ───────────────────────────────

            public static Theme HighContrastBlack { get; } = new Theme("High Contrast Black")
            {
                GreyBackground = Color.FromArgb(9, 9, 9),
                HeaderBackground = Color.FromArgb(5, 5, 5),
                MediumBackground = Color.FromArgb(17, 17, 17),
                LightBackground = Color.FromArgb(24, 24, 24),
                LighterBackground = Color.FromArgb(36, 36, 36),
                LightestBackground = Color.FromArgb(255, 255, 255),
                DarkBackground = Color.FromArgb(0, 0, 0),
                BlueBackground = Color.FromArgb(14, 14, 14),
                DarkBlueBackground = Color.FromArgb(9, 9, 9),
                LightBorder = Color.FromArgb(51, 51, 51),
                DarkBorder = Color.FromArgb(30, 30, 30),
                DarkBlueBorder = Color.FromArgb(38, 38, 38),
                LightBlueBorder = Color.FromArgb(74, 74, 74),
                LightText = Color.FromArgb(255, 255, 255),
                DisabledText = Color.FromArgb(160, 160, 160),
                BlueHighlight = Color.FromArgb(216, 216, 216),
                // Used by grids, grouped lists, combo boxes and sidebar navigation.
                // Keep it dark enough for the theme's white selection text.
                BlueSelection = Color.FromArgb(0, 95, 184),
                GreyHighlight = Color.FromArgb(90, 90, 90),
                GreySelection = Color.FromArgb(72, 72, 72),
                DarkGreySelection = Color.FromArgb(54, 54, 54),
                ActiveControl = Color.FromArgb(255, 255, 255),
                MenuItemToggledOnFill = Color.FromArgb(46, 46, 46),
                MenuItemToggledOnBorder = Color.FromArgb(255, 255, 255),
            };

            public static Theme HighContrastWhite { get; } = new Theme("High Contrast White")
            {
                GreyBackground = Color.FromArgb(248, 248, 248),
                HeaderBackground = Color.FromArgb(225, 225, 225),
                MediumBackground = Color.FromArgb(238, 238, 238),
                LightBackground = Color.FromArgb(255, 255, 255),
                LighterBackground = Color.FromArgb(255, 255, 255),
                LightestBackground = Color.FromArgb(16, 16, 16),
                DarkBackground = Color.FromArgb(212, 212, 212),
                BlueBackground = Color.FromArgb(240, 240, 240),
                DarkBlueBackground = Color.FromArgb(228, 228, 228),
                LightBorder = Color.FromArgb(232, 232, 232),
                DarkBorder = Color.FromArgb(208, 208, 208),
                DarkBlueBorder = Color.FromArgb(220, 220, 220),
                LightBlueBorder = Color.FromArgb(244, 244, 244),
                LightText = Color.FromArgb(16, 16, 16),
                DisabledText = Color.FromArgb(102, 102, 102),
                BlueHighlight = Color.FromArgb(51, 51, 51),
                // Used by grids, grouped lists, combo boxes and sidebar navigation.
                // Keep it light enough for the theme's black selection text.
                BlueSelection = Color.FromArgb(255, 235, 59),
                GreyHighlight = Color.FromArgb(212, 212, 212),
                GreySelection = Color.FromArgb(200, 200, 200),
                DarkGreySelection = Color.FromArgb(184, 184, 184),
                ActiveControl = Color.FromArgb(0, 0, 0),
                MenuItemToggledOnFill = Color.FromArgb(224, 224, 224),
                MenuItemToggledOnBorder = Color.FromArgb(17, 17, 17),
            };

            // ── Low-contrast / mid-light family ──────────────────

            public static Theme SoftCharcoal { get; } = new Theme("Soft Charcoal")
            {
                GreyBackground = Color.FromArgb(41, 42, 43),
                HeaderBackground = Color.FromArgb(45, 46, 47),
                MediumBackground = Color.FromArgb(48, 49, 50),
                LightBackground = Color.FromArgb(50, 51, 52),
                LighterBackground = Color.FromArgb(56, 57, 58),
                LightestBackground = Color.FromArgb(124, 126, 128),
                DarkBackground = Color.FromArgb(36, 37, 38),
                BlueBackground = Color.FromArgb(47, 48, 50),
                DarkBlueBackground = Color.FromArgb(42, 43, 45),
                LightBorder = Color.FromArgb(62, 64, 66),
                DarkBorder = Color.FromArgb(43, 44, 46),
                DarkBlueBorder = Color.FromArgb(52, 54, 56),
                LightBlueBorder = Color.FromArgb(70, 72, 74),
                LightText = Color.FromArgb(202, 203, 205),
                DisabledText = Color.FromArgb(133, 135, 137),
                BlueHighlight = Color.FromArgb(110, 113, 116),
                BlueSelection = Color.FromArgb(95, 98, 101),
                GreyHighlight = Color.FromArgb(74, 75, 78),
                GreySelection = Color.FromArgb(64, 66, 68),
                DarkGreySelection = Color.FromArgb(56, 58, 60),
                ActiveControl = Color.FromArgb(126, 128, 131),
                MenuItemToggledOnFill = Color.FromArgb(58, 60, 62),
                MenuItemToggledOnBorder = Color.FromArgb(110, 113, 116),
            };

            public static Theme FogGrey { get; } = new Theme("Fog Grey")
            {
                GreyBackground = Color.FromArgb(74, 76, 78),
                HeaderBackground = Color.FromArgb(69, 71, 73),
                MediumBackground = Color.FromArgb(80, 82, 84),
                LightBackground = Color.FromArgb(86, 88, 90),
                LighterBackground = Color.FromArgb(98, 100, 102),
                LightestBackground = Color.FromArgb(153, 155, 157),
                DarkBackground = Color.FromArgb(60, 62, 64),
                BlueBackground = Color.FromArgb(79, 81, 83),
                DarkBlueBackground = Color.FromArgb(70, 72, 74),
                LightBorder = Color.FromArgb(94, 96, 98),
                DarkBorder = Color.FromArgb(70, 72, 74),
                DarkBlueBorder = Color.FromArgb(82, 84, 86),
                LightBlueBorder = Color.FromArgb(106, 108, 110),
                LightText = Color.FromArgb(215, 216, 217),
                DisabledText = Color.FromArgb(156, 158, 159),
                BlueHighlight = Color.FromArgb(137, 140, 143),
                BlueSelection = Color.FromArgb(123, 126, 129),
                GreyHighlight = Color.FromArgb(112, 114, 116),
                GreySelection = Color.FromArgb(90, 92, 95),
                DarkGreySelection = Color.FromArgb(78, 80, 82),
                ActiveControl = Color.FromArgb(154, 156, 158),
                MenuItemToggledOnFill = Color.FromArgb(86, 88, 90),
                MenuItemToggledOnBorder = Color.FromArgb(137, 140, 143),
            };

            public static Theme Cloud { get; } = new Theme("Cloud")
            {
                GreyBackground = Color.FromArgb(180, 183, 186),
                HeaderBackground = Color.FromArgb(168, 171, 175),
                MediumBackground = Color.FromArgb(190, 193, 196),
                LightBackground = Color.FromArgb(197, 200, 203),
                LighterBackground = Color.FromArgb(208, 210, 212),
                LightestBackground = Color.FromArgb(226, 228, 230),
                DarkBackground = Color.FromArgb(150, 154, 158),
                BlueBackground = Color.FromArgb(185, 188, 192),
                DarkBlueBackground = Color.FromArgb(169, 172, 176),
                LightBorder = Color.FromArgb(201, 203, 206),
                DarkBorder = Color.FromArgb(166, 169, 172),
                DarkBlueBorder = Color.FromArgb(180, 183, 186),
                LightBlueBorder = Color.FromArgb(210, 212, 214),
                LightText = Color.FromArgb(32, 35, 38),
                DisabledText = Color.FromArgb(138, 142, 146),
                BlueHighlight = Color.FromArgb(108, 115, 122),
                BlueSelection = Color.FromArgb(95, 102, 109),
                GreyHighlight = Color.FromArgb(176, 179, 182),
                GreySelection = Color.FromArgb(158, 161, 164),
                DarkGreySelection = Color.FromArgb(142, 145, 148),
                ActiveControl = Color.FromArgb(116, 123, 130),
                MenuItemToggledOnFill = Color.FromArgb(192, 195, 198),
                MenuItemToggledOnBorder = Color.FromArgb(108, 115, 122),
            };

            public static Theme Snow { get; } = new Theme("Snow")
            {
                GreyBackground = Color.FromArgb(244, 245, 246),
                HeaderBackground = Color.FromArgb(232, 234, 236),
                MediumBackground = Color.FromArgb(236, 238, 240),
                LightBackground = Color.FromArgb(255, 255, 255),
                LighterBackground = Color.FromArgb(255, 255, 255),
                LightestBackground = Color.FromArgb(255, 255, 255),
                DarkBackground = Color.FromArgb(218, 221, 224),
                BlueBackground = Color.FromArgb(240, 242, 243),
                DarkBlueBackground = Color.FromArgb(228, 231, 233),
                LightBorder = Color.FromArgb(224, 227, 229),
                DarkBorder = Color.FromArgb(213, 216, 218),
                DarkBlueBorder = Color.FromArgb(226, 229, 231),
                LightBlueBorder = Color.FromArgb(237, 239, 241),
                LightText = Color.FromArgb(21, 23, 25),
                DisabledText = Color.FromArgb(115, 119, 123),
                BlueHighlight = Color.FromArgb(96, 102, 108),
                BlueSelection = Color.FromArgb(69, 74, 80),
                GreyHighlight = Color.FromArgb(210, 213, 216),
                GreySelection = Color.FromArgb(194, 198, 201),
                DarkGreySelection = Color.FromArgb(178, 182, 185),
                ActiveControl = Color.FromArgb(110, 116, 123),
                MenuItemToggledOnFill = Color.FromArgb(232, 234, 236),
                MenuItemToggledOnBorder = Color.FromArgb(96, 102, 108),
            };

            public static Theme IvoryPaper { get; } = new Theme("Ivory Paper")
            {
                GreyBackground = Color.FromArgb(238, 234, 224),
                HeaderBackground = Color.FromArgb(225, 220, 209),
                MediumBackground = Color.FromArgb(244, 241, 232),
                LightBackground = Color.FromArgb(250, 247, 239),
                LighterBackground = Color.FromArgb(255, 253, 247),
                LightestBackground = Color.FromArgb(255, 253, 247),
                DarkBackground = Color.FromArgb(213, 208, 197),
                BlueBackground = Color.FromArgb(245, 241, 232),
                DarkBlueBackground = Color.FromArgb(232, 227, 216),
                LightBorder = Color.FromArgb(232, 227, 216),
                DarkBorder = Color.FromArgb(208, 203, 191),
                DarkBlueBorder = Color.FromArgb(222, 217, 204),
                LightBlueBorder = Color.FromArgb(242, 238, 229),
                LightText = Color.FromArgb(41, 39, 32),
                DisabledText = Color.FromArgb(129, 125, 114),
                BlueHighlight = Color.FromArgb(139, 130, 113),
                BlueSelection = Color.FromArgb(115, 107, 92),
                GreyHighlight = Color.FromArgb(216, 211, 200),
                GreySelection = Color.FromArgb(198, 192, 180),
                DarkGreySelection = Color.FromArgb(180, 174, 162),
                ActiveControl = Color.FromArgb(139, 130, 113),
                MenuItemToggledOnFill = Color.FromArgb(233, 228, 217),
                MenuItemToggledOnBorder = Color.FromArgb(115, 107, 92),
            };

            public static Theme Moonlight { get; } = new Theme("Moonlight")
            {
                GreyBackground = Color.FromArgb(229, 233, 239),
                HeaderBackground = Color.FromArgb(221, 226, 232),
                MediumBackground = Color.FromArgb(233, 237, 242),
                LightBackground = Color.FromArgb(242, 245, 248),
                LighterBackground = Color.FromArgb(250, 252, 253),
                LightestBackground = Color.FromArgb(255, 255, 255),
                DarkBackground = Color.FromArgb(212, 217, 224),
                BlueBackground = Color.FromArgb(232, 236, 241),
                DarkBlueBackground = Color.FromArgb(217, 222, 229),
                LightBorder = Color.FromArgb(216, 221, 228),
                DarkBorder = Color.FromArgb(201, 207, 215),
                DarkBlueBorder = Color.FromArgb(213, 219, 226),
                LightBlueBorder = Color.FromArgb(232, 237, 242),
                LightText = Color.FromArgb(32, 37, 44),
                DisabledText = Color.FromArgb(140, 147, 156),
                BlueHighlight = Color.FromArgb(114, 131, 151),
                BlueSelection = Color.FromArgb(87, 103, 121),
                GreyHighlight = Color.FromArgb(201, 207, 214),
                GreySelection = Color.FromArgb(186, 193, 202),
                DarkGreySelection = Color.FromArgb(172, 180, 189),
                ActiveControl = Color.FromArgb(114, 131, 151),
                MenuItemToggledOnFill = Color.FromArgb(226, 231, 236),
                MenuItemToggledOnBorder = Color.FromArgb(87, 103, 121),
            };

            public static Theme SageMist { get; } = new Theme("Sage Mist")
            {
                GreyBackground = Color.FromArgb(216, 222, 215),
                HeaderBackground = Color.FromArgb(209, 216, 209),
                MediumBackground = Color.FromArgb(222, 227, 221),
                LightBackground = Color.FromArgb(232, 236, 231),
                LighterBackground = Color.FromArgb(242, 245, 241),
                LightestBackground = Color.FromArgb(248, 250, 247),
                DarkBackground = Color.FromArgb(200, 207, 200),
                BlueBackground = Color.FromArgb(226, 231, 225),
                DarkBlueBackground = Color.FromArgb(211, 217, 211),
                LightBorder = Color.FromArgb(203, 210, 203),
                DarkBorder = Color.FromArgb(187, 195, 187),
                DarkBlueBorder = Color.FromArgb(200, 208, 201),
                LightBlueBorder = Color.FromArgb(220, 227, 220),
                LightText = Color.FromArgb(37, 43, 38),
                DisabledText = Color.FromArgb(138, 147, 140),
                BlueHighlight = Color.FromArgb(120, 138, 123),
                BlueSelection = Color.FromArgb(97, 117, 102),
                GreyHighlight = Color.FromArgb(194, 202, 194),
                GreySelection = Color.FromArgb(178, 187, 179),
                DarkGreySelection = Color.FromArgb(164, 174, 165),
                ActiveControl = Color.FromArgb(120, 138, 123),
                MenuItemToggledOnFill = Color.FromArgb(224, 230, 224),
                MenuItemToggledOnBorder = Color.FromArgb(97, 117, 102),
            };

            public static Theme LavenderMist { get; } = new Theme("Lavender Mist")
            {
                GreyBackground = Color.FromArgb(221, 217, 229),
                HeaderBackground = Color.FromArgb(214, 210, 222),
                MediumBackground = Color.FromArgb(227, 224, 233),
                LightBackground = Color.FromArgb(236, 233, 241),
                LighterBackground = Color.FromArgb(245, 243, 248),
                LightestBackground = Color.FromArgb(251, 250, 252),
                DarkBackground = Color.FromArgb(205, 200, 214),
                BlueBackground = Color.FromArgb(231, 228, 236),
                DarkBlueBackground = Color.FromArgb(216, 212, 223),
                LightBorder = Color.FromArgb(212, 208, 218),
                DarkBorder = Color.FromArgb(196, 191, 204),
                DarkBlueBorder = Color.FromArgb(208, 203, 216),
                LightBlueBorder = Color.FromArgb(226, 223, 232),
                LightText = Color.FromArgb(41, 38, 46),
                DisabledText = Color.FromArgb(142, 136, 150),
                BlueHighlight = Color.FromArgb(141, 126, 155),
                BlueSelection = Color.FromArgb(116, 103, 130),
                GreyHighlight = Color.FromArgb(200, 195, 209),
                GreySelection = Color.FromArgb(184, 178, 194),
                DarkGreySelection = Color.FromArgb(169, 163, 179),
                ActiveControl = Color.FromArgb(141, 126, 155),
                MenuItemToggledOnFill = Color.FromArgb(229, 226, 235),
                MenuItemToggledOnBorder = Color.FromArgb(116, 103, 130),
            };

            public static Theme PowderBlue { get; } = new Theme("Powder Blue")
            {
                GreyBackground = Color.FromArgb(213, 222, 229),
                HeaderBackground = Color.FromArgb(204, 214, 222),
                MediumBackground = Color.FromArgb(219, 226, 233),
                LightBackground = Color.FromArgb(230, 237, 242),
                LighterBackground = Color.FromArgb(241, 245, 248),
                LightestBackground = Color.FromArgb(248, 250, 252),
                DarkBackground = Color.FromArgb(195, 205, 213),
                BlueBackground = Color.FromArgb(226, 233, 239),
                DarkBlueBackground = Color.FromArgb(211, 220, 228),
                LightBorder = Color.FromArgb(201, 211, 218),
                DarkBorder = Color.FromArgb(183, 194, 203),
                DarkBlueBorder = Color.FromArgb(195, 206, 214),
                LightBlueBorder = Color.FromArgb(220, 228, 234),
                LightText = Color.FromArgb(34, 41, 46),
                DisabledText = Color.FromArgb(138, 149, 158),
                BlueHighlight = Color.FromArgb(117, 144, 164),
                BlueSelection = Color.FromArgb(92, 117, 137),
                GreyHighlight = Color.FromArgb(193, 203, 211),
                GreySelection = Color.FromArgb(176, 188, 197),
                DarkGreySelection = Color.FromArgb(161, 174, 184),
                ActiveControl = Color.FromArgb(117, 144, 164),
                MenuItemToggledOnFill = Color.FromArgb(224, 231, 237),
                MenuItemToggledOnBorder = Color.FromArgb(92, 117, 137),
            };

            public static Theme Sandstone { get; } = new Theme("Sandstone")
            {
                GreyBackground = Color.FromArgb(199, 187, 167),
                HeaderBackground = Color.FromArgb(185, 172, 150),
                MediumBackground = Color.FromArgb(208, 197, 179),
                LightBackground = Color.FromArgb(217, 207, 190),
                LighterBackground = Color.FromArgb(224, 215, 200),
                LightestBackground = Color.FromArgb(239, 233, 222),
                DarkBackground = Color.FromArgb(169, 154, 130),
                BlueBackground = Color.FromArgb(206, 196, 178),
                DarkBlueBackground = Color.FromArgb(189, 177, 160),
                LightBorder = Color.FromArgb(195, 183, 164),
                DarkBorder = Color.FromArgb(174, 161, 145),
                DarkBlueBorder = Color.FromArgb(185, 173, 160),
                LightBlueBorder = Color.FromArgb(212, 202, 187),
                LightText = Color.FromArgb(48, 42, 34),
                DisabledText = Color.FromArgb(143, 133, 118),
                BlueHighlight = Color.FromArgb(146, 122, 91),
                BlueSelection = Color.FromArgb(119, 98, 71),
                GreyHighlight = Color.FromArgb(181, 169, 152),
                GreySelection = Color.FromArgb(166, 154, 138),
                DarkGreySelection = Color.FromArgb(152, 140, 125),
                ActiveControl = Color.FromArgb(146, 122, 91),
                MenuItemToggledOnFill = Color.FromArgb(207, 196, 178),
                MenuItemToggledOnBorder = Color.FromArgb(119, 98, 71),
            };

            // ── Colorful deep family ─────────────────────────────

            public static Theme Terracotta { get; } = new Theme("Terracotta")
            {
                GreyBackground = Color.FromArgb(90, 64, 54),
                HeaderBackground = Color.FromArgb(74, 52, 44),
                MediumBackground = Color.FromArgb(100, 70, 59),
                LightBackground = Color.FromArgb(112, 78, 64),
                LighterBackground = Color.FromArgb(128, 91, 74),
                LightestBackground = Color.FromArgb(169, 138, 118),
                DarkBackground = Color.FromArgb(63, 45, 39),
                BlueBackground = Color.FromArgb(101, 73, 60),
                DarkBlueBackground = Color.FromArgb(83, 58, 48),
                LightBorder = Color.FromArgb(111, 82, 67),
                DarkBorder = Color.FromArgb(80, 58, 48),
                DarkBlueBorder = Color.FromArgb(92, 66, 54),
                LightBlueBorder = Color.FromArgb(125, 92, 74),
                LightText = Color.FromArgb(240, 226, 219),
                DisabledText = Color.FromArgb(181, 160, 153),
                BlueHighlight = Color.FromArgb(171, 107, 83),
                BlueSelection = Color.FromArgb(198, 129, 100),
                GreyHighlight = Color.FromArgb(110, 87, 75),
                GreySelection = Color.FromArgb(94, 74, 64),
                DarkGreySelection = Color.FromArgb(81, 64, 55),
                ActiveControl = Color.FromArgb(198, 129, 100),
                MenuItemToggledOnFill = Color.FromArgb(107, 75, 59),
                MenuItemToggledOnBorder = Color.FromArgb(198, 129, 100),
            };

            public static Theme OceanDepth { get; } = new Theme("Ocean Depth")
            {
                GreyBackground = Color.FromArgb(19, 37, 46),
                HeaderBackground = Color.FromArgb(15, 33, 42),
                MediumBackground = Color.FromArgb(23, 45, 55),
                LightBackground = Color.FromArgb(28, 53, 64),
                LighterBackground = Color.FromArgb(36, 68, 79),
                LightestBackground = Color.FromArgb(94, 126, 137),
                DarkBackground = Color.FromArgb(12, 27, 34),
                BlueBackground = Color.FromArgb(26, 48, 57),
                DarkBlueBackground = Color.FromArgb(19, 38, 48),
                LightBorder = Color.FromArgb(35, 64, 75),
                DarkBorder = Color.FromArgb(22, 48, 58),
                DarkBlueBorder = Color.FromArgb(28, 57, 68),
                LightBlueBorder = Color.FromArgb(46, 78, 90),
                LightText = Color.FromArgb(209, 225, 231),
                DisabledText = Color.FromArgb(126, 149, 158),
                BlueHighlight = Color.FromArgb(57, 117, 141),
                BlueSelection = Color.FromArgb(62, 130, 157),
                GreyHighlight = Color.FromArgb(47, 76, 86),
                GreySelection = Color.FromArgb(36, 63, 72),
                DarkGreySelection = Color.FromArgb(28, 52, 61),
                ActiveControl = Color.FromArgb(78, 147, 172),
                MenuItemToggledOnFill = Color.FromArgb(30, 58, 69),
                MenuItemToggledOnBorder = Color.FromArgb(62, 130, 157),
            };

            public static Theme TurquoiseGlass { get; } = new Theme("Turquoise Glass")
            {
                GreyBackground = Color.FromArgb(23, 51, 54),
                HeaderBackground = Color.FromArgb(19, 43, 46),
                MediumBackground = Color.FromArgb(27, 60, 63),
                LightBackground = Color.FromArgb(33, 72, 75),
                LighterBackground = Color.FromArgb(41, 88, 90),
                LightestBackground = Color.FromArgb(94, 139, 139),
                DarkBackground = Color.FromArgb(16, 36, 38),
                BlueBackground = Color.FromArgb(30, 65, 68),
                DarkBlueBackground = Color.FromArgb(23, 53, 56),
                LightBorder = Color.FromArgb(38, 82, 85),
                DarkBorder = Color.FromArgb(24, 60, 63),
                DarkBlueBorder = Color.FromArgb(31, 70, 73),
                LightBlueBorder = Color.FromArgb(49, 96, 99),
                LightText = Color.FromArgb(209, 235, 233),
                DisabledText = Color.FromArgb(134, 168, 166),
                BlueHighlight = Color.FromArgb(50, 144, 140),
                BlueSelection = Color.FromArgb(54, 166, 160),
                GreyHighlight = Color.FromArgb(51, 98, 97),
                GreySelection = Color.FromArgb(40, 78, 78),
                DarkGreySelection = Color.FromArgb(32, 64, 64),
                ActiveControl = Color.FromArgb(78, 185, 179),
                MenuItemToggledOnFill = Color.FromArgb(34, 74, 76),
                MenuItemToggledOnBorder = Color.FromArgb(54, 166, 160),
            };

            public static Theme Orchid { get; } = new Theme("Orchid")
            {
                GreyBackground = Color.FromArgb(53, 41, 56),
                HeaderBackground = Color.FromArgb(46, 35, 48),
                MediumBackground = Color.FromArgb(61, 47, 64),
                LightBackground = Color.FromArgb(71, 53, 74),
                LighterBackground = Color.FromArgb(85, 64, 90),
                LightestBackground = Color.FromArgb(150, 122, 152),
                DarkBackground = Color.FromArgb(40, 30, 42),
                BlueBackground = Color.FromArgb(65, 50, 72),
                DarkBlueBackground = Color.FromArgb(52, 42, 58),
                LightBorder = Color.FromArgb(75, 58, 79),
                DarkBorder = Color.FromArgb(58, 45, 62),
                DarkBlueBorder = Color.FromArgb(66, 51, 73),
                LightBlueBorder = Color.FromArgb(87, 68, 92),
                LightText = Color.FromArgb(234, 223, 234),
                DisabledText = Color.FromArgb(165, 140, 168),
                BlueHighlight = Color.FromArgb(144, 87, 145),
                BlueSelection = Color.FromArgb(167, 101, 168),
                GreyHighlight = Color.FromArgb(92, 75, 96),
                GreySelection = Color.FromArgb(77, 62, 82),
                DarkGreySelection = Color.FromArgb(65, 53, 70),
                ActiveControl = Color.FromArgb(176, 124, 177),
                MenuItemToggledOnFill = Color.FromArgb(73, 56, 77),
                MenuItemToggledOnBorder = Color.FromArgb(167, 101, 168),
            };

            public static Theme Raspberry { get; } = new Theme("Raspberry")
            {
                GreyBackground = Color.FromArgb(58, 39, 46),
                HeaderBackground = Color.FromArgb(50, 32, 40),
                MediumBackground = Color.FromArgb(67, 44, 52),
                LightBackground = Color.FromArgb(77, 51, 60),
                LighterBackground = Color.FromArgb(91, 60, 71),
                LightestBackground = Color.FromArgb(158, 119, 132),
                DarkBackground = Color.FromArgb(43, 28, 34),
                BlueBackground = Color.FromArgb(70, 48, 58),
                DarkBlueBackground = Color.FromArgb(57, 37, 48),
                LightBorder = Color.FromArgb(82, 57, 68),
                DarkBorder = Color.FromArgb(62, 42, 51),
                DarkBlueBorder = Color.FromArgb(70, 48, 58),
                LightBlueBorder = Color.FromArgb(94, 67, 80),
                LightText = Color.FromArgb(236, 220, 226),
                DisabledText = Color.FromArgb(175, 150, 160),
                BlueHighlight = Color.FromArgb(157, 78, 105),
                BlueSelection = Color.FromArgb(182, 87, 120),
                GreyHighlight = Color.FromArgb(102, 74, 85),
                GreySelection = Color.FromArgb(85, 61, 72),
                DarkGreySelection = Color.FromArgb(73, 52, 62),
                ActiveControl = Color.FromArgb(195, 108, 137),
                MenuItemToggledOnFill = Color.FromArgb(76, 51, 64),
                MenuItemToggledOnBorder = Color.FromArgb(182, 87, 120),
            };

            public static Theme Honeycomb { get; } = new Theme("Honeycomb")
            {
                GreyBackground = Color.FromArgb(57, 52, 38),
                HeaderBackground = Color.FromArgb(48, 43, 30),
                MediumBackground = Color.FromArgb(66, 60, 42),
                LightBackground = Color.FromArgb(75, 68, 48),
                LighterBackground = Color.FromArgb(90, 81, 56),
                LightestBackground = Color.FromArgb(156, 142, 102),
                DarkBackground = Color.FromArgb(41, 37, 26),
                BlueBackground = Color.FromArgb(68, 62, 42),
                DarkBlueBackground = Color.FromArgb(54, 49, 37),
                LightBorder = Color.FromArgb(80, 72, 48),
                DarkBorder = Color.FromArgb(61, 56, 42),
                DarkBlueBorder = Color.FromArgb(70, 62, 44),
                LightBlueBorder = Color.FromArgb(94, 83, 52),
                LightText = Color.FromArgb(238, 232, 214),
                DisabledText = Color.FromArgb(179, 169, 136),
                BlueHighlight = Color.FromArgb(161, 135, 62),
                BlueSelection = Color.FromArgb(185, 154, 70),
                GreyHighlight = Color.FromArgb(101, 90, 62),
                GreySelection = Color.FromArgb(84, 75, 52),
                DarkGreySelection = Color.FromArgb(72, 64, 44),
                ActiveControl = Color.FromArgb(199, 168, 79),
                MenuItemToggledOnFill = Color.FromArgb(78, 68, 46),
                MenuItemToggledOnBorder = Color.FromArgb(185, 154, 70),
            };

            // ── Light pastel / special-purpose ───────────────────

            public static Theme MintCream { get; } = new Theme("Mint Cream")
            {
                GreyBackground = Color.FromArgb(228, 236, 231),
                HeaderBackground = Color.FromArgb(216, 226, 220),
                MediumBackground = Color.FromArgb(234, 241, 237),
                LightBackground = Color.FromArgb(242, 247, 244),
                LighterBackground = Color.FromArgb(251, 253, 252),
                LightestBackground = Color.FromArgb(255, 255, 255),
                DarkBackground = Color.FromArgb(203, 215, 207),
                BlueBackground = Color.FromArgb(236, 243, 239),
                DarkBlueBackground = Color.FromArgb(221, 231, 225),
                LightBorder = Color.FromArgb(210, 221, 215),
                DarkBorder = Color.FromArgb(194, 207, 199),
                DarkBlueBorder = Color.FromArgb(207, 219, 212),
                LightBlueBorder = Color.FromArgb(227, 236, 231),
                LightText = Color.FromArgb(28, 41, 34),
                DisabledText = Color.FromArgb(126, 143, 133),
                BlueHighlight = Color.FromArgb(109, 144, 126),
                BlueSelection = Color.FromArgb(80, 120, 100),
                GreyHighlight = Color.FromArgb(197, 210, 203),
                GreySelection = Color.FromArgb(182, 197, 189),
                DarkGreySelection = Color.FromArgb(169, 185, 176),
                ActiveControl = Color.FromArgb(109, 144, 126),
                MenuItemToggledOnFill = Color.FromArgb(226, 235, 230),
                MenuItemToggledOnBorder = Color.FromArgb(80, 120, 100),
            };

            public static Theme Blueprint { get; } = new Theme("Blueprint")
            {
                GreyBackground = Color.FromArgb(21, 42, 64),
                HeaderBackground = Color.FromArgb(17, 36, 56),
                MediumBackground = Color.FromArgb(24, 49, 73),
                LightBackground = Color.FromArgb(28, 56, 85),
                LighterBackground = Color.FromArgb(36, 70, 102),
                LightestBackground = Color.FromArgb(74, 107, 138),
                DarkBackground = Color.FromArgb(14, 30, 45),
                BlueBackground = Color.FromArgb(22, 41, 63),
                DarkBlueBackground = Color.FromArgb(17, 32, 47),
                LightBorder = Color.FromArgb(34, 64, 93),
                DarkBorder = Color.FromArgb(22, 48, 74),
                DarkBlueBorder = Color.FromArgb(29, 58, 87),
                LightBlueBorder = Color.FromArgb(44, 78, 111),
                LightText = Color.FromArgb(217, 233, 247),
                DisabledText = Color.FromArgb(131, 152, 170),
                BlueHighlight = Color.FromArgb(67, 127, 184),
                BlueSelection = Color.FromArgb(79, 145, 207),
                GreyHighlight = Color.FromArgb(53, 96, 132),
                GreySelection = Color.FromArgb(41, 79, 115),
                DarkGreySelection = Color.FromArgb(32, 62, 92),
                ActiveControl = Color.FromArgb(121, 175, 224),
                MenuItemToggledOnFill = Color.FromArgb(26, 53, 80),
                MenuItemToggledOnBorder = Color.FromArgb(79, 145, 207),
            };
            public static Theme CyberChrome3D { get; } = Create3D("Cyber Chrome 3D", ThemeDepthStyle.Glossy3D, Color.FromArgb(24, 30, 38), Color.FromArgb(70, 210, 255));
            public static Theme VioletArcade3D { get; } = Create3D("Violet Arcade 3D", ThemeDepthStyle.Glossy3D, Color.FromArgb(29, 22, 45), Color.FromArgb(170, 110, 255));
            public static Theme EmeraldConsole3D { get; } = Create3D("Emerald Console 3D", ThemeDepthStyle.Glossy3D, Color.FromArgb(18, 34, 31), Color.FromArgb(65, 220, 165));
            public static Theme CrimsonMachine3D { get; } = Create3D("Crimson Machine 3D", ThemeDepthStyle.Glossy3D, Color.FromArgb(40, 23, 27), Color.FromArgb(245, 100, 115));
            public static Theme AmberForge3D { get; } = Create3D("Amber Forge 3D", ThemeDepthStyle.Glossy3D, Color.FromArgb(42, 31, 18), Color.FromArgb(255, 175, 55));
            public static Theme CobaltCabinet3D { get; } = Create3D("Cobalt Cabinet 3D", ThemeDepthStyle.ClassicBevel3D, Color.FromArgb(22, 32, 55), Color.FromArgb(92, 145, 255));
            public static Theme MonochromeBevel3D { get; } = Create3D("Monochrome Bevel 3D", ThemeDepthStyle.ClassicBevel3D, Color.FromArgb(42, 42, 42), Color.FromArgb(190, 190, 190));
            public static Theme IvoryBevel3D { get; } = Create3D("Ivory Bevel 3D", ThemeDepthStyle.ClassicBevel3D, Color.FromArgb(225, 220, 205), Color.FromArgb(112, 92, 70));
            public static Theme SoftGraphite3D { get; } = Create3D("Soft Graphite 3D", ThemeDepthStyle.Soft3D, Color.FromArgb(52, 57, 66), Color.FromArgb(124, 151, 181));
            public static Theme SoftLavender3D { get; } = Create3D("Soft Lavender 3D", ThemeDepthStyle.Soft3D, Color.FromArgb(67, 60, 82), Color.FromArgb(184, 160, 235));

            /// <summary>
            /// Ten hand-picked palette variants for each 3D visual concept. Keeping
            /// these as series makes the selector easy to grow without a wall of
            /// individually copied Theme declarations.
            /// </summary>
            public static IReadOnlyList<Theme> ConceptThemes { get; } = new[]
            {
                CreateConceptSeries(Glass3DCategory, "Glass", ThemeDepthStyle.Glass3D, new[]
                {
                    Pair(20, 43, 57, 65, 210, 235), Pair(22, 36, 56, 90, 150, 255), Pair(28, 34, 50, 172, 112, 255), Pair(25, 47, 43, 69, 226, 180), Pair(45, 35, 56, 242, 119, 214),
                    Pair(38, 48, 58, 141, 216, 255), Pair(48, 38, 32, 255, 176, 96), Pair(31, 55, 52, 89, 240, 212), Pair(40, 36, 48, 196, 162, 255), Pair(25, 52, 70, 80, 222, 255)
                }),
                CreateConceptSeries(Neumorphic3DCategory, "Neumorphic", ThemeDepthStyle.Neumorphic3D, new[]
                {
                    Pair(57, 63, 71, 132, 157, 183), Pair(64, 68, 76, 166, 142, 198), Pair(67, 70, 69, 125, 175, 151), Pair(75, 69, 65, 193, 154, 126), Pair(215, 220, 226, 102, 128, 164),
                    Pair(224, 217, 224, 148, 112, 166), Pair(204, 220, 218, 75, 142, 132), Pair(218, 214, 202, 169, 130, 76), Pair(53, 58, 65, 104, 166, 210), Pair(76, 78, 84, 186, 153, 208)
                }),
                CreateConceptSeries(RetroWindows3DCategory, "Retro Windows", ThemeDepthStyle.RetroWindows3D, new[]
                {
                    Pair(192, 192, 192, 0, 0, 128), Pair(0, 128, 128, 255, 255, 255), Pair(0, 0, 128, 255, 255, 0), Pair(128, 0, 128, 255, 0, 255), Pair(0, 100, 0, 0, 255, 0),
                    Pair(128, 0, 0, 255, 255, 255), Pair(128, 128, 0, 255, 255, 0), Pair(72, 72, 72, 80, 160, 255), Pair(240, 240, 240, 0, 120, 215), Pair(48, 48, 48, 0, 255, 255)
                }),
                CreateConceptSeries(Industrial3DCategory, "Industrial", ThemeDepthStyle.Industrial3D, new[]
                {
                    Pair(42, 45, 48, 136, 144, 151), Pair(48, 49, 48, 205, 143, 52), Pair(39, 44, 45, 99, 179, 171), Pair(52, 46, 41, 188, 104, 63), Pair(35, 40, 46, 95, 139, 191),
                    Pair(58, 58, 55, 176, 179, 178), Pair(45, 48, 51, 169, 180, 191), Pair(55, 51, 46, 183, 143, 98), Pair(31, 34, 36, 107, 216, 190), Pair(62, 63, 64, 238, 176, 52)
                }),
                CreateConceptSeries(Console3DCategory, "Console", ThemeDepthStyle.Console3D, new[]
                {
                    Pair(20, 35, 45, 0, 200, 255), Pair(35, 19, 47, 174, 90, 255), Pair(18, 42, 30, 40, 232, 126), Pair(46, 19, 29, 255, 65, 105), Pair(47, 32, 13, 255, 187, 0),
                    Pair(15, 25, 50, 61, 119, 255), Pair(45, 15, 46, 255, 70, 215), Pair(14, 45, 46, 0, 243, 224), Pair(48, 48, 48, 255, 255, 255), Pair(29, 32, 34, 190, 255, 54)
                }),
                CreateConceptSeries(PlayStation4Category, "PS4", ThemeDepthStyle.Console3D, new[]
                {
                    Pair(16, 30, 59, 0, 112, 209), Pair(11, 22, 45, 35, 141, 255), Pair(19, 38, 74, 75, 164, 255), Pair(12, 27, 55, 0, 198, 255), Pair(24, 31, 62, 110, 130, 255),
                    Pair(16, 37, 66, 64, 189, 238), Pair(29, 42, 74, 104, 169, 255), Pair(9, 20, 40, 45, 99, 210), Pair(22, 33, 54, 92, 183, 255), Pair(13, 29, 52, 0, 145, 255)
                }),
                CreateConceptSeries(Clay3DCategory, "Clay", ThemeDepthStyle.Clay3D, new[]
                {
                    Pair(98, 125, 158, 158, 208, 255), Pair(156, 112, 142, 250, 178, 218), Pair(126, 153, 122, 184, 238, 165), Pair(173, 135, 105, 255, 207, 139), Pair(136, 119, 173, 205, 181, 255),
                    Pair(185, 126, 111, 255, 177, 144), Pair(104, 151, 148, 142, 232, 219), Pair(174, 154, 103, 255, 226, 136), Pair(117, 132, 167, 177, 206, 255), Pair(154, 137, 160, 230, 195, 232)
                }),
                CreateConceptSeries(Crystal3DCategory, "Crystal", ThemeDepthStyle.Crystal3D, new[]
                {
                    Pair(20, 33, 58, 89, 190, 255), Pair(42, 18, 63, 205, 110, 255), Pair(14, 51, 45, 60, 255, 209), Pair(62, 16, 35, 255, 77, 150), Pair(58, 37, 10, 255, 196, 54),
                    Pair(18, 47, 73, 71, 229, 255), Pair(49, 16, 78, 244, 122, 255), Pair(7, 58, 37, 107, 255, 146), Pair(71, 25, 16, 255, 125, 71), Pair(18, 18, 35, 210, 225, 255)
                }),
                CreateConceptSeries(Terminal3DCategory, "Terminal", ThemeDepthStyle.Terminal3D, new[]
                {
                    Pair(13, 20, 15, 80, 255, 115), Pair(13, 19, 23, 70, 210, 255), Pair(22, 18, 12, 255, 176, 48), Pair(22, 12, 16, 255, 70, 100), Pair(17, 13, 25, 183, 102, 255),
                    Pair(18, 22, 22, 190, 255, 70), Pair(16, 24, 21, 72, 237, 183), Pair(23, 23, 23, 232, 232, 232), Pair(20, 26, 35, 115, 172, 255), Pair(25, 22, 16, 255, 212, 97)
                }),
                CreateConceptSeries(Paper3DCategory, "Paper Card", ThemeDepthStyle.Paper3D, new[]
                {
                    Pair(238, 238, 236, 60, 105, 180), Pair(245, 240, 231, 159, 104, 54), Pair(235, 243, 237, 63, 132, 91), Pair(243, 235, 240, 166, 77, 129), Pair(231, 238, 247, 76, 118, 179),
                    Pair(249, 244, 231, 192, 145, 57), Pair(237, 235, 246, 122, 97, 185), Pair(233, 242, 244, 58, 140, 150), Pair(244, 237, 229, 189, 96, 61), Pair(241, 241, 241, 89, 89, 89)
                }),
                CreateConceptSeries(SciFi3DCategory, "Sci-fi HUD", ThemeDepthStyle.SciFi3D, new[]
                {
                    Pair(10, 21, 31, 0, 220, 255), Pair(20, 13, 37, 176, 83, 255), Pair(9, 35, 33, 0, 255, 190), Pair(37, 11, 25, 255, 48, 135), Pair(37, 27, 7, 255, 200, 0),
                    Pair(15, 23, 47, 71, 137, 255), Pair(25, 10, 43, 255, 75, 230), Pair(6, 35, 46, 38, 239, 255), Pair(29, 30, 32, 198, 255, 51), Pair(23, 17, 12, 255, 153, 50)
                })
            }.SelectMany(series => series).ToArray();

            private static Theme[] CreateConceptSeries(string category, string concept, ThemeDepthStyle depthStyle, (Color Background, Color Accent)[] variants)
            {
                return variants.Select((variant, index) =>
                    Create3D($"{concept} {index + 1:00}", depthStyle, variant.Background, variant.Accent, category)).ToArray();
            }

            private static (Color Background, Color Accent) Pair(int backgroundR, int backgroundG, int backgroundB, int accentR, int accentG, int accentB)
            {
                return (Color.FromArgb(backgroundR, backgroundG, backgroundB), Color.FromArgb(accentR, accentG, accentB));
            }

            private static Theme Create3D(string name, ThemeDepthStyle depthStyle, Color background, Color accent, string category = null)
            {
                Color Shift(Color color, int amount) => Color.FromArgb(
                    Math.Clamp(color.R + amount, 0, 255), Math.Clamp(color.G + amount, 0, 255), Math.Clamp(color.B + amount, 0, 255));
                Color Blend(Color first, Color second, float secondAmount) => Color.FromArgb(
                    (int)Math.Round(first.R + ((second.R - first.R) * secondAmount)),
                    (int)Math.Round(first.G + ((second.G - first.G) * secondAmount)),
                    (int)Math.Round(first.B + ((second.B - first.B) * secondAmount)));
                double Linear(byte channel)
                {
                    double value = channel / 255d;
                    return value <= 0.04045d ? value / 12.92d : Math.Pow((value + 0.055d) / 1.055d, 2.4d);
                }
                double Luminance(Color color) => (0.2126d * Linear(color.R)) + (0.7152d * Linear(color.G)) + (0.0722d * Linear(color.B));
                double Contrast(Color first, Color second)
                {
                    double firstLuminance = Luminance(first);
                    double secondLuminance = Luminance(second);
                    return (Math.Max(firstLuminance, secondLuminance) + 0.05d) / (Math.Min(firstLuminance, secondLuminance) + 0.05d);
                }
                Color EnsureSelectionContrast(Color color, Color foreground)
                {
                    Color target = foreground.GetBrightness() > 0.5f ? Color.Black : Color.White;
                    for (int step = 0; step < 12 && Contrast(color, foreground) < 4.5d; step++)
                        color = Blend(color, target, 0.18f);
                    return color;
                }

                bool light = background.GetBrightness() > 0.55f;
                Color text = light ? Color.FromArgb(35, 35, 35) : Color.FromArgb(235, 238, 242);
                // DataGridView, ListView and TreeView all use LightText for selected
                // items. A bright neon accent therefore needs a darker selected fill
                // on a dark theme (and the inverse on a light theme).
                Color selection = EnsureSelectionContrast(Blend(background, accent, light ? 0.45f : 0.40f), text);
                return new Theme(name)
                {
                    DepthStyle = depthStyle,
                    Category = category,
                    GreyBackground = background,
                    HeaderBackground = Shift(background, light ? -12 : -10),
                    MediumBackground = Shift(background, light ? -18 : 8),
                    LightBackground = Shift(background, light ? -28 : 16),
                    LighterBackground = Shift(background, light ? -38 : 28),
                    LightestBackground = Shift(background, light ? -75 : 65),
                    DarkBackground = Shift(background, light ? -45 : -18),
                    BlueBackground = Shift(accent, -70),
                    DarkBlueBackground = Shift(accent, -100),
                    LightBorder = Shift(background, light ? -48 : 42),
                    DarkBorder = Shift(background, light ? -82 : -28),
                    DarkBlueBorder = Shift(accent, -105),
                    LightBlueBorder = Shift(accent, -25),
                    LightText = text,
                    DisabledText = Shift(text, light ? 80 : -95),
                    BlueHighlight = Shift(accent, 25),
                    BlueSelection = selection,
                    GreyHighlight = Shift(background, light ? -55 : 55),
                    GreySelection = Shift(background, light ? -38 : 30),
                    DarkGreySelection = Shift(background, light ? -58 : 12),
                    ActiveControl = Shift(accent, 45),
                    MenuItemToggledOnFill = Shift(accent, -95),
                    MenuItemToggledOnBorder = accent
                };
            }
        }
    }
}
