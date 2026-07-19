namespace IbrahimAbdo.Login.Helpers;

internal static class GlyphHelper
{
    public static Bitmap Create(string glyph, Color color, int size)
    {
        var bmp = new Bitmap(size + 6, size + 6);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using var font = new Font("Segoe MDL2 Assets", size * 0.7F, FontStyle.Regular, GraphicsUnit.Pixel);
        TextRenderer.DrawText(g, glyph, font, new Rectangle(0, 0, bmp.Width, bmp.Height), color,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        return bmp;
    }
}
