using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Controls;

internal sealed class OrDivider : Control
{
    public OrDivider()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor,
            true);
        Height = AppLayout.Scale(28);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var centerY = Height / 2;
        var text = "OR";
        var textSize = TextRenderer.MeasureText(text, Font);
        var gap = AppLayout.Scale(12);
        var textX = (Width - textSize.Width) / 2;

        using var linePen = new Pen(Color.FromArgb(80, AppTheme.Gold), 1F);
        graphics.DrawLine(linePen, 0, centerY, textX - gap, centerY);
        graphics.DrawLine(linePen, textX + textSize.Width + gap, centerY, Width, centerY);

        TextRenderer.DrawText(
            graphics,
            text,
            Font,
            new Point(textX, (Height - textSize.Height) / 2),
            AppTheme.Gray);
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        Invalidate();
    }
}
