using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Controls;

internal class GoldButton : Control
{
    private bool _isPressed;
    private float _hoverGlow;

    public event EventHandler? Clicked;

    public GoldButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, true);
        Height = AppTheme.ButtonHeight;
        Font = AppTheme.ButtonFont;
        ForeColor = AppTheme.White;
        Cursor = Cursors.Hand;
        Text = "LOGIN";
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

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            _isPressed = true;
            Invalidate();
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            _isPressed = false;
            Invalidate();
            OnClick(EventArgs.Empty);
        }
    }

    private void AnimateGlow(float target)
    {
        var animator = new AnimationHelper();
        var start = _hoverGlow;
        animator.Animate(start, target, 180, value =>
        {
            _hoverGlow = value;
            Invalidate();
        }, animator.Dispose);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        GraphicsHelper.EnableHighQuality(graphics);

        var scale = _isPressed ? 0.985F : 1F;
        var width = Width * scale;
        var height = Height * scale;
        var x = (Width - width) / 2F;
        var y = (Height - height) / 2F;
        var bounds = new RectangleF(x, y, width, height);

        if (_hoverGlow > 0F)
        {
            GraphicsHelper.DrawGlow(graphics, bounds, Color.FromArgb((int)(70 * _hoverGlow), AppTheme.Gold), AppTheme.ButtonRadius, 3, 1.2F);
        }

        using var path = GraphicsHelper.CreateRoundedRectangle(bounds, AppTheme.ButtonRadius);
        using var gradient = new LinearGradientBrush(
            bounds,
            _isPressed ? AppTheme.Gold : AppTheme.GoldLight,
            _isPressed ? AppTheme.GoldDark : AppTheme.Gold,
            LinearGradientMode.Vertical);

        graphics.FillPath(gradient, path);

        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            Rectangle.Round(bounds),
            ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
