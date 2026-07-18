namespace IbrahimAbdo.Login.Theme;

internal static class AppTheme
{
    public const string FontFamily = "Segoe UI";

    public static readonly Color Gold = Color.FromArgb(212, 175, 55);
    public static readonly Color GoldLight = Color.FromArgb(232, 205, 95);
    public static readonly Color GoldDark = Color.FromArgb(168, 134, 32);
    public static readonly Color Dark = Color.FromArgb(15, 15, 15);
    public static readonly Color Secondary = Color.FromArgb(26, 26, 26);
    public static readonly Color White = Color.FromArgb(255, 255, 255);
    public static readonly Color Gray = Color.FromArgb(160, 160, 160);
    public static readonly Color MutedIcon = Color.FromArgb(122, 122, 122);
    public static readonly Color InputBackground = Color.FromArgb(22, 22, 22);
    public static readonly Color InputBorder = Color.FromArgb(90, 90, 90);
    public static readonly Color PanelBackground = Color.FromArgb(210, 15, 15, 15);

    public static readonly Font TitleFont = new("Segoe UI Semibold", 16F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font SubtitleFont = new(FontFamily, 9F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font LabelFont = new(FontFamily, 9F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font InputFont = new(FontFamily, 10F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font ButtonFont = new(FontFamily, 10F, FontStyle.Bold, GraphicsUnit.Point);
    public static readonly Font FooterFont = new(FontFamily, 8F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font IconFont = new("Segoe MDL2 Assets", 12F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font UserIconFont = new("Segoe MDL2 Assets", 14F, FontStyle.Regular, GraphicsUnit.Point);

    // Wide rectangle card (~35% of 1280) sitting in the black center void
    public const int PanelWidth = 445;
    public const int PanelHeight = 381; // shortened ~0.5cm from bottom
    public const int ContentWidth = 360;
    public const int CornerRadius = 20;
    public const int InputRadius = 10;
    public const int ButtonRadius = 10;
    public const int InputHeight = 38;
    public const int ButtonHeight = 40;
    public const int ControlSpacing = 7;
    public const int SidePadding = (PanelWidth - ContentWidth) / 2;
    public const float BorderWidth = 1F;

    /// <summary>Clears IBRAHIM ABDO / AUTO SERVICE at the top.</summary>
    public const float LogoClearanceRatio = 0.26F;

    /// <summary>Extra downward shift into the black center (was 154; raised ~1.5cm / 57px).</summary>
    public const int PanelDownOffset = 97;

    /// <summary>Keeps EXPERT SERVICE / POWER IN EVERY PART (and left quality block) uncovered.</summary>
    public const int BottomArtworkClearance = 110;
}
