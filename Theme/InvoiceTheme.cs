namespace IbrahimAbdo.Login.Theme;

internal static class InvoiceTheme
{
    public static readonly Color Background = Color.FromArgb(15, 15, 16);
    public static readonly Color Card = Color.FromArgb(23, 23, 23);
    public static readonly Color CardBorder = Color.FromArgb(40, 40, 40);
    public static readonly Color Sidebar = Color.FromArgb(18, 18, 18);
    public static readonly Color Gold = Color.FromArgb(212, 164, 77);
    public static readonly Color GoldDark = Color.FromArgb(168, 128, 48);
    public static readonly Color White = Color.White;
    public static readonly Color Muted = Color.FromArgb(160, 160, 160);
    public static readonly Color InputFill = Color.FromArgb(28, 28, 28);
    public static readonly Color RowAlt = Color.FromArgb(20, 20, 20);
    public static readonly Color Danger = Color.FromArgb(220, 80, 80);

    public static readonly Font Family = ResolveFont();

    public static Font TitleFont => new(Family.FontFamily, 16F, FontStyle.Bold, GraphicsUnit.Point);
    public static Font SectionFont => new(Family.FontFamily, 11F, FontStyle.Bold, GraphicsUnit.Point);
    public static Font BodyFont => new(Family.FontFamily, 9.5F, FontStyle.Regular, GraphicsUnit.Point);
    public static Font SmallFont => new(Family.FontFamily, 8.5F, FontStyle.Regular, GraphicsUnit.Point);
    public static Font MenuFont => new(Family.FontFamily, 10F, FontStyle.Regular, GraphicsUnit.Point);
    public static Font TableHeaderFont => new(Family.FontFamily, 9.5F, FontStyle.Bold, GraphicsUnit.Point);
    public static Font TotalFont => new(Family.FontFamily, 16F, FontStyle.Bold, GraphicsUnit.Point);
    public static Font IconFont => new("Segoe MDL2 Assets", 12F, FontStyle.Regular, GraphicsUnit.Point);

    public const int Radius = 12;
    public const int SidebarWidth = 260;

    private static Font ResolveFont()
    {
        try
        {
            using var test = new Font("Cairo", 10F, FontStyle.Regular, GraphicsUnit.Point);
            if (test.Name.Equals("Cairo", StringComparison.OrdinalIgnoreCase))
            {
                return test;
            }
        }
        catch
        {
            // fall through
        }

        return new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    }
}
