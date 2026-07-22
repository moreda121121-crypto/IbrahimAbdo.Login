namespace IbrahimAbdo.Login.Helpers;

internal static class GlyphHelper
{
    public static Bitmap Create(string glyph, Color color, int size)
    {
        if (glyph is "SAFE" or "VAULT")
        {
            return CreateSafeIcon(color, size);
        }

        var bmp = new Bitmap(size + 6, size + 6);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using var font = new Font("Segoe MDL2 Assets", size * 0.7F, FontStyle.Regular, GraphicsUnit.Pixel);
        TextRenderer.DrawText(g, glyph, font, new Rectangle(0, 0, bmp.Width, bmp.Height), color,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        return bmp;
    }

    /// <summary>Professional safe/vault dial icon matching the الخزنة menu.</summary>
    public static Bitmap CreateSafeIcon(Color color, int size)
    {
        var bmp = new Bitmap(size + 6, size + 6);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        var penW = Math.Max(1.6f, size / 13f);
        using var pen = new Pen(color, penW);
        pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;

        var box = new RectangleF(3.5f, 4.5f, size - 1f, size - 3f);
        g.DrawRectangle(pen, box.X, box.Y, box.Width, box.Height);

        // Combination dial
        var dialSize = size * 0.42f;
        var dial = new RectangleF(
            box.X + (box.Width - dialSize) / 2f - 1f,
            box.Y + (box.Height - dialSize) / 2f,
            dialSize,
            dialSize);
        g.DrawEllipse(pen, dial);
        var hub = dialSize * 0.28f;
        g.DrawEllipse(pen,
            dial.X + (dial.Width - hub) / 2f,
            dial.Y + (dial.Height - hub) / 2f,
            hub,
            hub);

        // Handle on the right edge of the safe door
        var hx = box.Right - size * 0.18f;
        g.DrawLine(pen, hx, box.Y + size * 0.22f, hx, box.Bottom - size * 0.22f);
        g.DrawEllipse(pen, hx - size * 0.08f, box.Y + box.Height / 2f - size * 0.08f, size * 0.16f, size * 0.16f);

        return bmp;
    }
}
