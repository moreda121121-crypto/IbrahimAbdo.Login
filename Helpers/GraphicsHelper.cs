namespace IbrahimAbdo.Login.Helpers;

internal static class GraphicsHelper
{
    public static GraphicsPath CreateRoundedRectangle(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2F;

        if (diameter >= bounds.Width || diameter >= bounds.Height)
        {
            path.AddRectangle(bounds);
            path.CloseFigure();
            return path;
        }

        var arc = new RectangleF(bounds.X, bounds.Y, diameter, diameter);
        path.AddArc(arc, 180, 90);

        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);

        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);

        arc.X = bounds.X;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }

    public static void DrawGlow(Graphics graphics, RectangleF bounds, Color color, float radius, int layers, float spread)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        for (var i = layers; i >= 1; i--)
        {
            var alpha = (int)(color.A * (0.08F + (0.12F * i / layers)));
            using var pen = new Pen(Color.FromArgb(alpha, color), spread * i);
            pen.LineJoin = LineJoin.Round;

            var inset = spread * i * 0.35F;
            using var path = CreateRoundedRectangle(
                RectangleF.Inflate(bounds, inset, inset),
                radius + inset);

            graphics.DrawPath(pen, path);
        }
    }

    public static void DrawDropShadow(Graphics graphics, RectangleF bounds, float radius, int depth)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        for (var i = depth; i >= 1; i--)
        {
            var alpha = Math.Min(48, 4 + i * 4);
            var offset = i * 0.9F;

            using var brush = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0));
            using var path = CreateRoundedRectangle(
                new RectangleF(bounds.X + 2, bounds.Y + offset, bounds.Width, bounds.Height),
                radius);

            graphics.FillPath(brush, path);
        }
    }

    public static void EnableHighQuality(Graphics graphics)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
    }
}
