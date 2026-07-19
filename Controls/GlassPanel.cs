using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Controls;

internal class GlassPanel : Panel
{
    private float _opacity = 1F;

    public float PanelOpacity
    {
        get => _opacity;
        set
        {
            _opacity = Math.Clamp(value, 0F, 1F);
            Invalidate();
        }
    }

    public GlassPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor,
            true);

        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        GraphicsHelper.EnableHighQuality(graphics);

        var bounds = new RectangleF(0.5F, 0.5F, Width - 1F, Height - 1F);
        GraphicsHelper.DrawDropShadow(graphics, bounds, AppTheme.CornerRadius, 8);

        using var path = GraphicsHelper.CreateRoundedRectangle(bounds, AppTheme.CornerRadius);
        var backgroundAlpha = (int)(AppTheme.PanelBackground.A * _opacity);
        using (var fill = new SolidBrush(Color.FromArgb(backgroundAlpha, 15, 15, 15)))
        {
            graphics.FillPath(fill, path);
        }

        using (var border = new Pen(Color.FromArgb((int)(255 * _opacity), AppTheme.Gold), AppTheme.BorderWidth))
        {
            border.Alignment = PenAlignment.Inset;
            graphics.DrawPath(border, path);
        }
    }
}
