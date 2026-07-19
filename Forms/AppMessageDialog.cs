using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

internal enum AppMessageKind
{
    Info,
    Success,
    Warning,
    Error
}

internal sealed class AppMessageDialog : Form
{
    private AppMessageDialog(string title, string message, AppMessageKind kind)
    {
        SuspendLayout();
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = InvoiceTheme.Card;
        ForeColor = InvoiceTheme.White;
        Font = InvoiceTheme.BodyFont;
        ClientSize = new Size(440, 250);

        var accent = kind switch
        {
            AppMessageKind.Success => Color.FromArgb(72, 180, 120),
            AppMessageKind.Warning => InvoiceTheme.Gold,
            AppMessageKind.Error => InvoiceTheme.Danger,
            _ => InvoiceTheme.Gold
        };

        var glyph = kind switch
        {
            AppMessageKind.Success => "\uE73E",
            AppMessageKind.Warning => "\uE7BA",
            AppMessageKind.Error => "\uE783",
            _ => "\uE946"
        };

        var shell = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.Card,
            BorderColor = accent,
            BorderThickness = 2,
            BorderRadius = 16,
            Padding = new Padding(24, 22, 24, 18),
            ShadowDecoration =
            {
                Enabled = true,
                Depth = 25,
                Color = Color.FromArgb(180, 0, 0, 0),
                BorderRadius = 16
            }
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var iconHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var iconBadge = new Guna2Panel
        {
            Size = new Size(52, 52),
            BorderRadius = 26,
            FillColor = Color.FromArgb(40, accent),
            Anchor = AnchorStyles.None
        };
        var icon = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.CenterImage,
            BackColor = Color.Transparent,
            Image = GlyphHelper.Create(glyph, accent, 28)
        };
        iconBadge.Controls.Add(icon);
        iconHost.Resize += (_, _) =>
        {
            iconBadge.Left = (iconHost.ClientSize.Width - iconBadge.Width) / 2;
            iconBadge.Top = Math.Max(0, (iconHost.ClientSize.Height - iconBadge.Height) / 2);
        };
        iconHost.Controls.Add(iconBadge);

        var titleLbl = new Label
        {
            Dock = DockStyle.Fill,
            Text = title,
            Font = InvoiceTheme.TitleFont,
            ForeColor = accent,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        var messageLbl = new Label
        {
            Dock = DockStyle.Fill,
            Text = message,
            Font = InvoiceTheme.MenuFont,
            ForeColor = Color.FromArgb(230, 230, 230),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        var footer = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var ok = new Guna2Button
        {
            Text = "موافق",
            Size = new Size(150, 42),
            BorderRadius = 10,
            Font = new Font(InvoiceTheme.Family.FontFamily, 11F, FontStyle.Bold, GraphicsUnit.Point),
            FillColor = accent,
            ForeColor = kind is AppMessageKind.Warning or AppMessageKind.Info ? Color.Black : Color.White,
            Cursor = Cursors.Hand,
            Animated = true,
            Anchor = AnchorStyles.None,
            HoverState =
            {
                FillColor = ControlPaint.Light(accent, 0.12F),
                ForeColor = kind is AppMessageKind.Warning or AppMessageKind.Info ? Color.Black : Color.White
            }
        };
        footer.Resize += (_, _) =>
        {
            ok.Left = (footer.ClientSize.Width - ok.Width) / 2;
            ok.Top = Math.Max(0, (footer.ClientSize.Height - ok.Height) / 2);
        };
        ok.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };
        footer.Controls.Add(ok);

        root.Controls.Add(iconHost, 0, 0);
        root.Controls.Add(titleLbl, 0, 1);
        root.Controls.Add(messageLbl, 0, 2);
        root.Controls.Add(footer, 0, 3);
        shell.Controls.Add(root);
        Controls.Add(shell);

        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode is Keys.Enter or Keys.Escape)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        };

        ResumeLayout(true);
    }

    public static void Show(IWin32Window? owner, string message, string title, AppMessageKind kind = AppMessageKind.Info)
    {
        using var dlg = new AppMessageDialog(title, message, kind);
        if (owner is not null)
        {
            dlg.ShowDialog(owner);
        }
        else
        {
            dlg.ShowDialog();
        }
    }

    public static void Success(IWin32Window? owner, string message, string title = "نجاح") =>
        Show(owner, message, title, AppMessageKind.Success);

    public static void Warning(IWin32Window? owner, string message, string title = "تنبيه") =>
        Show(owner, message, title, AppMessageKind.Warning);

    public static void Error(IWin32Window? owner, string message, string title = "خطأ") =>
        Show(owner, message, title, AppMessageKind.Error);

    public static void Info(IWin32Window? owner, string message, string title = "معلومة") =>
        Show(owner, message, title, AppMessageKind.Info);
}
