using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

internal sealed class AddUserDialog : Form
{
    private readonly Guna2TextBox _txtUsername;
    private readonly Guna2TextBox _txtPassword;
    private readonly Guna2TextBox _txtConfirm;

    public UserRecord? Result { get; private set; }

    public AddUserDialog()
    {
        SuspendLayout();
        WindowTheme.Attach(this);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "إضافة مستخدم جديد";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = InvoiceTheme.Background;
        ForeColor = InvoiceTheme.White;
        Font = InvoiceTheme.BodyFont;
        ClientSize = new Size(480, 340);
        RightToLeft = RightToLeft.No;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = InvoiceTheme.Background,
            Padding = new Padding(20)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "إضافة مستخدم جديد",
            Font = InvoiceTheme.TitleFont,
            ForeColor = InvoiceTheme.Gold,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Padding = new Padding(8, 8, 8, 0)
        };
        for (var i = 0; i < 3; i++)
        {
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        }

        _txtUsername = AddField(body, 0, "اسم الحساب (Username) *", "مثال: ahmed");
        _txtPassword = AddField(body, 1, "كلمة المرور *", "أدخل كلمة المرور", isPassword: true);
        _txtConfirm = AddField(body, 2, "تأكيد كلمة المرور *", "أعد إدخال كلمة المرور", isPassword: true);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0)
        };
        var btnSave = CreateGoldButton("حفظ المستخدم", "\uE74E");
        btnSave.Click += (_, _) => Save();
        var btnCancel = CreateOutlineButton("إلغاء");
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        footer.Controls.Add(btnSave);
        footer.Controls.Add(btnCancel);

        root.Controls.Add(title, 0, 0);
        root.Controls.Add(body, 0, 1);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);
        ResumeLayout(true);
    }

    private void Save()
    {
        var username = _txtUsername.Text.Trim();
        var password = _txtPassword.Text;
        var confirm = _txtConfirm.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            AppMessageDialog.Warning(this, "اسم الحساب وكلمة المرور مطلوبان.");
            return;
        }

        if (password.Length < 3)
        {
            AppMessageDialog.Warning(this, "كلمة المرور يجب ألا تقل عن 3 أحرف.");
            return;
        }

        if (password != confirm)
        {
            AppMessageDialog.Warning(this, "كلمة المرور وتأكيدها غير متطابقين.");
            return;
        }

        if (UserStore.UsernameExists(username))
        {
            AppMessageDialog.Warning(this, "اسم الحساب مستخدم بالفعل. اختر اسماً آخر.");
            return;
        }

        Result = new UserRecord
        {
            DisplayName = username,
            Username = username,
            Password = password,
            Role = "مدير",
            IsActive = true,
            CreatedAt = DateTime.Today
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    private static Guna2TextBox AddField(TableLayoutPanel body, int row, string label, string placeholder, bool isPassword = false)
    {
        var host = new Panel { Dock = DockStyle.Fill, Margin = new Padding(4), BackColor = Color.Transparent };
        var lbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = label,
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont
        };
        var input = new Guna2TextBox
        {
            Dock = DockStyle.Fill,
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(90, InvoiceTheme.Gold),
            FillColor = InvoiceTheme.InputFill,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.BodyFont,
            PlaceholderText = placeholder,
            PlaceholderForeColor = InvoiceTheme.Muted,
            Height = 34,
            FocusedState = { BorderColor = InvoiceTheme.Gold },
            HoverState = { BorderColor = InvoiceTheme.Gold },
            PasswordChar = isPassword ? '\u25CF' : '\0',
            UseSystemPasswordChar = isPassword
        };
        host.Controls.Add(input);
        host.Controls.Add(lbl);
        body.Controls.Add(host, 0, row);
        return input;
    }

    private static Guna2Button CreateGoldButton(string text, string glyph)
    {
        return new Guna2Button
        {
            Text = text,
            Font = InvoiceTheme.MenuFont,
            ForeColor = Color.Black,
            FillColor = InvoiceTheme.Gold,
            BorderRadius = InvoiceTheme.Radius,
            Size = new Size(160, 42),
            Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand,
            Image = GlyphHelper.Create(glyph, Color.Black, 16),
            ImageSize = new Size(16, 16),
            ImageAlign = HorizontalAlignment.Left,
            HoverState = { FillColor = InvoiceTheme.GoldDark, ForeColor = Color.Black }
        };
    }

    private static Guna2Button CreateOutlineButton(string text)
    {
        return new Guna2Button
        {
            Text = text,
            Font = InvoiceTheme.MenuFont,
            ForeColor = InvoiceTheme.White,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.Gold,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Size = new Size(120, 42),
            Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand,
            HoverState = { FillColor = Color.FromArgb(30, InvoiceTheme.Gold), ForeColor = InvoiceTheme.White }
        };
    }
}
