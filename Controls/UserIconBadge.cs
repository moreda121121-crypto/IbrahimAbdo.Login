using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Controls;

internal class UserIconBadge : Control
{
    public UserIconBadge()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
        Size = new Size(52, 52);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Keep fully transparent — no white/dark square behind the icon.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        GraphicsHelper.EnableHighQuality(graphics);

        var outer = new RectangleF(1.5F, 1.5F, Width - 3F, Height - 3F);
        using (var border = new Pen(AppTheme.Gold, 1F))
        {
            graphics.DrawEllipse(border, outer);
        }

        TextRenderer.DrawText(
            graphics,
            "\uE77B",
            AppTheme.UserIconFont,
            new Rectangle(0, 0, Width, Height),
            AppTheme.Gold,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
