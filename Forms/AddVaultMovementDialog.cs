using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

internal sealed class AddVaultMovementDialog : Form
{
    private readonly VaultMovementType _type;
    private readonly Guna2TextBox _txtAmount;
    private readonly Guna2TextBox _txtDescription;

    public decimal Amount { get; private set; }
    public string Description { get; private set; } = "";

    public AddVaultMovementDialog(VaultMovementType type)
    {
        _type = type;
        SuspendLayout();
        WindowTheme.Attach(this);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = DialogTitle(type);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = InvoiceTheme.Background;
        ForeColor = InvoiceTheme.White;
        Font = InvoiceTheme.BodyFont;
        ClientSize = new Size(460, 300);
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
            Text = DialogTitle(type),
            Font = InvoiceTheme.TitleFont,
            ForeColor = InvoiceTheme.Gold,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Padding = new Padding(8, 8, 8, 0)
        };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));

        _txtAmount = AddField(body, 0, "المبلغ *", "0.00");
        _txtDescription = AddField(body, 1, "الوصف *", "اكتب وصف الحركة");

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
        if (!decimal.TryParse(_txtAmount.Text.Trim().Replace(",", ""), out var amount) || amount <= 0)
        {
            AppMessageDialog.Warning(this, "برجاء إدخال مبلغ صحيح");
            return;
        }

        var desc = _txtDescription.Text.Trim();
        if (string.IsNullOrWhiteSpace(desc))
        {
            AppMessageDialog.Warning(this, "برجاء إدخال وصف الحركة");
            return;
        }

        Amount = amount;
        Description = desc;
        DialogResult = DialogResult.OK;
        Close();
    }

    private static string DialogTitle(VaultMovementType type) => type switch
    {
        VaultMovementType.Income => "إضافة إيراد",
        VaultMovementType.Expense => "إضافة مصروف",
        VaultMovementType.Transfer => "تحويل بين الحسابات",
        VaultMovementType.Withdraw => "سحب نقدي",
        VaultMovementType.Deposit => "إيداع نقدي",
        _ => "حركة خزنة"
    };

    private static Guna2TextBox AddField(TableLayoutPanel body, int row, string label, string placeholder)
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
        var txt = new Guna2TextBox
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
        host.Controls.Add(txt);
        host.Controls.Add(lbl);
        body.Controls.Add(host, 0, row);
        return txt;
    }

    private static Guna2Button CreateButton(string text, bool primary) =>
        new()
        {
            Text = text,
            Font = InvoiceTheme.MenuFont,
            Size = new Size(110, 38),
            BorderRadius = 8,
            Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand,
            ForeColor = primary ? Color.Black : InvoiceTheme.White,
            FillColor = primary ? InvoiceTheme.Gold : InvoiceTheme.Card,
            BorderColor = InvoiceTheme.Gold,
            BorderThickness = primary ? 0 : 1,
            HoverState =
            {
                FillColor = primary ? InvoiceTheme.GoldDark : Color.FromArgb(30, InvoiceTheme.Gold),
                ForeColor = primary ? Color.Black : InvoiceTheme.White
            }
        };
}
