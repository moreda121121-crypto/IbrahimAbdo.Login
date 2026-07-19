namespace IbrahimAbdo.Login.Theme;

internal static class AppLayout
{
    public static readonly Size WindowSize = new(1280, 854);

    private const float DesignWidth = 1920F;

    public static float ScaleFactor => WindowSize.Width / DesignWidth;

    public static int Scale(int value) => (int)Math.Round(value * ScaleFactor);

    public static float Scale(float value) => value * ScaleFactor;

    public static Size PanelSize => new(Scale(480), Scale(640));

    public static int ContentWidth => Scale(408);

    public static Font Font(float size, FontStyle style = FontStyle.Regular) =>
        new(AppTheme.FontFamily, Math.Max(7F, Scale(size)), style, GraphicsUnit.Point);
}
