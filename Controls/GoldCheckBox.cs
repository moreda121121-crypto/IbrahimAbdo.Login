using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Controls;

internal class GoldCheckBox : CheckBox
{
    public GoldCheckBox()
    {
        AutoSize = false;
        Font = AppTheme.LabelFont;
        ForeColor = AppTheme.Gray;
        BackColor = Color.Transparent;
        FlatStyle = FlatStyle.Flat;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        const int boxSize = 16;
        var boxRect = new Rectangle(0, (Height - boxSize) / 2, boxSize, boxSize);

        using (var pen = new Pen(Color.FromArgb(140, AppTheme.Gold), 1F))
        {
            e.Graphics.DrawRectangle(pen, boxRect.X, boxRect.Y, boxSize - 1, boxSize - 1);
        }

        if (Checked)
        {
            using var fill = new SolidBrush(Color.FromArgb(200, AppTheme.Gold));
            e.Graphics.FillRectangle(fill, boxRect.X + 3, boxRect.Y + 3, boxSize - 6, boxSize - 6);
        }

        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            new Point(boxRect.Right + 10, (Height - Font.Height) / 2),
            ForeColor);
    }
}
