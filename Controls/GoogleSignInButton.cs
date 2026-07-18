using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Controls;

internal sealed class GoogleSignInButton : Control
{
    private bool _isPressed;
    private float _hoverGlow;

    public event EventHandler? Clicked;

    public GoogleSignInButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint |
            ControlStyles.StandardClick,
            true);

        Size = new Size(AppLayout.ContentWidth, AppLayout.Scale(44));
        Font = AppLayout.Font(9.5F, FontStyle.Bold);
        ForeColor = AppTheme.White;
        Cursor = Cursors.Hand;
        Text = "SIGN IN WITH GOOGLE";
        TabStop = true;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        AnimateGlow(1F);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isPressed = false;
        AnimateGlow(0F);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            _isPressed = true;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _isPressed = false;
        Invalidate();
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Clicked?.Invoke(this, EventArgs.Empty);
    }

    private void AnimateGlow(float target)
    {
        var animator = new AnimationHelper();
        var start = _hoverGlow;
        animator.Animate(start, target, 200, value =>
        {
            _hoverGlow = value;
            Invalidate();
        }, animator.Dispose);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        GraphicsHelper.EnableHighQuality(graphics);

        var scale = _isPressed ? 0.98F : 1F;
        var width = Width * scale;
        var height = Height * scale;
        var x = (Width - width) / 2F;
        var y = (Height - height) / 2F;
        var bounds = new RectangleF(x, y, width, height);
        var radius = AppLayout.Scale(AppTheme.ButtonRadius);

        if (_hoverGlow > 0F)
        {
            GraphicsHelper.DrawGlow(
                graphics,
                bounds,
                Color.FromArgb((int)(120 * _hoverGlow), AppTheme.Gold),
                radius,
                4,
                1.8F);
        }

        using var path = GraphicsHelper.CreateRoundedRectangle(bounds, radius);
        var fillColor = _isPressed ? AppTheme.Secondary : AppTheme.Dark;
        using (var fill = new SolidBrush(fillColor))
        {
            graphics.FillPath(fill, path);
        }

        using (var border = new Pen(AppTheme.Gold, 1.2F) { Alignment = PenAlignment.Inset })
        {
            graphics.DrawPath(border, path);
        }

        DrawGoogleMark(graphics, new RectangleF(bounds.X + AppLayout.Scale(18), bounds.Y + height / 2F - AppLayout.Scale(8), AppLayout.Scale(16), AppLayout.Scale(16)));

        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            Rectangle.Round(bounds),
            ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private static void DrawGoogleMark(Graphics graphics, RectangleF bounds)
    {
        using var font = AppLayout.Font(11F, FontStyle.Bold);
        TextRenderer.DrawText(
            graphics,
            "G",
            font,
            Rectangle.Round(bounds),
            Color.FromArgb(66, 133, 244),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
