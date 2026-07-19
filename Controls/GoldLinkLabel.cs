using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Controls;

internal class GoldLinkLabel : Label
{
    private bool _isHovered;

    public GoldLinkLabel()
    {
        AutoSize = true;
        Font = AppTheme.LabelFont;
        ForeColor = AppTheme.Gold;
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        TextAlign = ContentAlignment.MiddleRight;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        ForeColor = AppTheme.GoldLight;
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        ForeColor = AppTheme.Gold;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_isHovered)
        {
            var underlineY = Height - 2;
            using var pen = new Pen(AppTheme.GoldLight, 1F);
            e.Graphics.DrawLine(pen, 0, underlineY, Width, underlineY);
        }
    }
}
