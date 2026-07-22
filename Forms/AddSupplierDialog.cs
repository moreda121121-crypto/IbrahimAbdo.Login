using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

internal sealed class AddSupplierDialog : Form
{
    private readonly Guna2TextBox _txtName;
    private readonly Guna2TextBox _txtPhone;

    public SupplierRecord? Result { get; private set; }

    public AddSupplierDialog()
    {
        SuspendLayout();
        WindowTheme.Attach(this);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "إضافة مورد جديد";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = InvoiceTheme.Background;
        ForeColor = InvoiceTheme.White;
        Font = InvoiceTheme.BodyFont;
        ClientSize = new Size(420, 260);
        RightToLeft = RightToLeft.No;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = InvoiceTheme.Background,
            Padding = new Padding(18)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "إضافة مورد جديد",
            Font = InvoiceTheme.TitleFont,
            ForeColor = InvoiceTheme.Gold,
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.Yes
        };

        _txtName = CreateInput("اسم المورد");
        _txtPhone = CreateInput("رقم الهاتف");

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0)
        };
        var save = new Guna2Button
        {
            Text = "حفظ",
            Font = InvoiceTheme.MenuFont,
            ForeColor = Color.Black,
            FillColor = InvoiceTheme.Gold,
            BorderRadius = InvoiceTheme.Radius,
            Size = new Size(130, 40),
            Cursor = Cursors.Hand,
            HoverState = { FillColor = InvoiceTheme.GoldDark, ForeColor = Color.Black }
        };
        save.Click += (_, _) => Save();
        var cancel = new Guna2Button
        {
            Text = "إلغاء",
            Font = InvoiceTheme.MenuFont,
            ForeColor = InvoiceTheme.White,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.Gold,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Size = new Size(110, 40),
            Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand,
            HoverState = { FillColor = Color.FromArgb(30, InvoiceTheme.Gold), ForeColor = InvoiceTheme.White }
        };
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        footer.Controls.Add(save);
        footer.Controls.Add(cancel);

        root.Controls.Add(title, 0, 0);
        root.Controls.Add(Labeled("اسم المورد *", _txtName), 0, 1);
        root.Controls.Add(Labeled("رقم الهاتف", _txtPhone), 0, 2);
        root.Controls.Add(footer, 0, 3);
        Controls.Add(root);

        AcceptButton = save;
        ResumeLayout(true);
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            AppMessageDialog.Warning(this, "اسم المورد مطلوب.");
            return;
        }

        Result = new SupplierRecord
        {
            Name = _txtName.Text.Trim(),
            Phone = _txtPhone.Text.Trim()
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    private static Control Labeled(string label, Control input)
    {
        var host = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var lbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = label,
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont
        };
        input.Dock = DockStyle.Fill;
        input.Height = 38;
        host.Controls.Add(input);
        host.Controls.Add(lbl);
        return host;
    }

    private static Guna2TextBox CreateInput(string placeholder) =>
        new()
        {
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(90, InvoiceTheme.Gold),
            FillColor = InvoiceTheme.InputFill,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.BodyFont,
            PlaceholderText = placeholder,
            PlaceholderForeColor = InvoiceTheme.Muted,
            Height = 38,
            MinimumSize = new Size(0, 38),
            FocusedState = { BorderColor = InvoiceTheme.Gold }
        };
}
