using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

internal sealed class AddTechnicianDialog : Form
{
    private readonly Guna2TextBox _txtName;
    private readonly Guna2TextBox _txtPhone;
    private readonly Guna2TextBox _txtAddress;

    public TechnicianRecord? Result { get; private set; }

    public AddTechnicianDialog()
    {
        SuspendLayout();
        WindowTheme.Attach(this);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "إضافة فني";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = InvoiceTheme.Background;
        ForeColor = InvoiceTheme.White;
        Font = InvoiceTheme.BodyFont;
        ClientSize = new Size(460, 340);
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
            Text = "إضافة فني جديد",
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

        _txtName = AddField(body, 0, "اسم الفني *", "مثال: أحمد محمد");
        _txtPhone = AddField(body, 1, "رقم الهاتف *", "مثال: 01001234567");
        _txtAddress = AddField(body, 2, "العنوان", "مثال: القاهرة - مدينة نصر");

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0)
        };
        var btnSave = CreateButton("حفظ", true);
        btnSave.Click += (_, _) => Save();
        var btnCancel = CreateButton("إلغاء", false);
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
        var name = _txtName.Text.Trim();
        var phone = _txtPhone.Text.Trim();
        var address = _txtAddress.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            AppMessageDialog.Warning(this, "برجاء إدخال اسم الفني");
            return;
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            AppMessageDialog.Warning(this, "برجاء إدخال رقم الهاتف");
            return;
        }

        Result = new TechnicianRecord
        {
            Name = name,
            Phone = phone,
            Address = address,
            CreatedAt = DateTime.Today
        };
        DialogResult = DialogResult.OK;
        Close();
    }

    private static Guna2TextBox AddField(TableLayoutPanel body, int row, string label, string placeholder)
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
            FocusedState = { BorderColor = InvoiceTheme.Gold }
        };
        host.Controls.Add(input);
        host.Controls.Add(lbl);
        body.Controls.Add(host, 0, row);
        return input;
    }

    private static Guna2Button CreateButton(string text, bool primary) =>
        new()
        {
            Text = text,
            Font = InvoiceTheme.MenuFont,
            ForeColor = primary ? Color.Black : InvoiceTheme.White,
            FillColor = primary ? InvoiceTheme.Gold : InvoiceTheme.Card,
            BorderColor = InvoiceTheme.Gold,
            BorderThickness = primary ? 0 : 1,
            BorderRadius = 8,
            Size = new Size(120, 40),
            Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand,
            HoverState =
            {
                FillColor = primary ? InvoiceTheme.GoldDark : Color.FromArgb(30, InvoiceTheme.Gold),
                ForeColor = primary ? Color.Black : InvoiceTheme.White
            }
        };
}
