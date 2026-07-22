using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

/// <summary>Standalone dialog to add a brand-new inventory product (mirrors the products page form).</summary>
internal sealed class AddProductDialog : Form
{
    private readonly Guna2TextBox _txtName;
    private readonly Guna2TextBox _txtCode;
    private readonly Guna2TextBox _txtBarcode;
    private readonly Guna2ComboBox _cmbCategory;
    private readonly Guna2ComboBox _cmbSupplier;
    private readonly Guna2ComboBox _cmbUnit;
    private readonly Guna2TextBox _txtQty;
    private readonly Guna2TextBox _txtMinStock;
    private readonly Guna2TextBox _txtPurchase;
    private readonly Guna2TextBox _txtSelling;

    public ProductRecord? Result { get; private set; }

    public AddProductDialog()
    {
        SuspendLayout();
        WindowTheme.Attach(this);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "إضافة صنف جديد";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = InvoiceTheme.Background;
        ForeColor = InvoiceTheme.White;
        Font = InvoiceTheme.BodyFont;
        ClientSize = new Size(400, 640);
        RightToLeft = RightToLeft.No;

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 44,
            Text = "إضافة صنف جديد",
            Font = InvoiceTheme.TitleFont,
            ForeColor = InvoiceTheme.Gold,
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.Yes,
            Padding = new Padding(16, 0, 16, 0)
        };

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = InvoiceTheme.Background,
            Padding = new Padding(16, 10, 16, 12)
        };
        var save = new Guna2Button
        {
            Dock = DockStyle.Left,
            Width = 150,
            Text = "حفظ الصنف",
            Font = InvoiceTheme.MenuFont,
            ForeColor = Color.Black,
            FillColor = InvoiceTheme.Gold,
            BorderRadius = 8,
            Cursor = Cursors.Hand,
            HoverState = { FillColor = InvoiceTheme.GoldDark, ForeColor = Color.Black }
        };
        save.Click += (_, _) => Save();
        var cancel = new Guna2Button
        {
            Dock = DockStyle.Right,
            Width = 110,
            Text = "إلغاء",
            Font = InvoiceTheme.MenuFont,
            ForeColor = InvoiceTheme.White,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.Gold,
            BorderThickness = 1,
            BorderRadius = 8,
            Cursor = Cursors.Hand,
            HoverState = { FillColor = Color.FromArgb(30, InvoiceTheme.Gold), ForeColor = InvoiceTheme.White }
        };
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        footer.Controls.Add(save);
        footer.Controls.Add(cancel);

        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = InvoiceTheme.Background,
            Padding = new Padding(16, 4, 16, 8)
        };
        body.HandleCreated += (_, _) => TrySetDarkScrollbars(body);

        _txtName = AddField(body, "اسم الصنف", "مثال: زيت موتور");
        _txtCode = AddField(body, "كود الصنف", "مثال: OIL-5W30");
        _txtBarcode = AddField(body, "الباركود", "اختياري");
        _cmbCategory = AddCombo(body, "التصنيف", ProductStore.Categories);
        _cmbSupplier = AddCombo(body, "المورد", ProductStore.Suppliers);
        _cmbUnit = AddCombo(body, "الوحدة", ProductStore.Units);
        _txtQty = AddField(body, "الكمية الحالية", "0");
        _txtQty.Text = "0";
        _txtMinStock = AddField(body, "حد التنبيه الأدنى", "5");
        _txtMinStock.Text = "5";
        _txtPurchase = AddField(body, "سعر الشراء", "0.00");
        _txtPurchase.Text = "0.00";
        _txtSelling = AddField(body, "سعر البيع", "0.00");
        _txtSelling.Text = "0.00";

        if (_cmbCategory.Items.Count > 0) _cmbCategory.SelectedIndex = 0;
        if (_cmbSupplier.Items.Count > 0) _cmbSupplier.SelectedIndex = 0;
        if (_cmbUnit.Items.Count > 0) _cmbUnit.SelectedIndex = 0;

        Controls.Add(body);
        Controls.Add(footer);
        Controls.Add(title);

        AcceptButton = save;
        ResumeLayout(true);
    }

    private void Save()
    {
        var name = _txtName.Text.Trim();
        var code = _txtCode.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            AppMessageDialog.Warning(this, "برجاء إدخال اسم الصنف");
            return;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            AppMessageDialog.Warning(this, "برجاء إدخال كود الصنف");
            return;
        }

        if (ProductStore.CodeExists(code))
        {
            AppMessageDialog.Warning(this, "كود الصنف مستخدم بالفعل");
            return;
        }

        if (!int.TryParse(_txtQty.Text.Trim(), out var qty) || qty < 0)
        {
            AppMessageDialog.Warning(this, "كمية غير صحيحة");
            return;
        }

        if (!int.TryParse(_txtMinStock.Text.Trim(), out var min) || min < 0)
        {
            AppMessageDialog.Warning(this, "حد التنبيه غير صحيح");
            return;
        }

        if (!decimal.TryParse(_txtPurchase.Text.Trim(), out var buy) || buy < 0)
        {
            AppMessageDialog.Warning(this, "سعر الشراء غير صحيح");
            return;
        }

        if (!decimal.TryParse(_txtSelling.Text.Trim(), out var sell) || sell < 0)
        {
            AppMessageDialog.Warning(this, "سعر البيع غير صحيح");
            return;
        }

        var product = new ProductRecord
        {
            Name = name,
            Code = code,
            Barcode = _txtBarcode.Text.Trim(),
            Category = _cmbCategory.SelectedItem?.ToString() ?? "أخرى",
            Supplier = _cmbSupplier.SelectedItem?.ToString() ?? "",
            Unit = _cmbUnit.SelectedItem?.ToString() ?? "قطعة",
            Quantity = qty,
            MinStock = min,
            PurchasePrice = buy,
            SellingPrice = sell
        };
        ProductStore.Add(product);
        Result = product;

        DialogResult = DialogResult.OK;
        Close();
    }

    private static Guna2TextBox AddField(FlowLayoutPanel host, string label, string placeholder)
    {
        var block = new Panel
        {
            Width = host.ClientSize.Width - 40,
            Height = 60,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 6)
        };
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
        block.Controls.Add(txt);
        block.Controls.Add(lbl);
        host.Controls.Add(block);
        host.SizeChanged += (_, _) => block.Width = host.ClientSize.Width - 40;
        return txt;
    }

    private static Guna2ComboBox AddCombo(FlowLayoutPanel host, string label, string[] items)
    {
        var block = new Panel
        {
            Width = host.ClientSize.Width - 40,
            Height = 60,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 6)
        };
        var lbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = label,
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont
        };
        var cmb = new Guna2ComboBox
        {
            Dock = DockStyle.Fill,
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(90, InvoiceTheme.Gold),
            FillColor = InvoiceTheme.InputFill,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.BodyFont,
            ItemHeight = 28,
            FocusedState = { BorderColor = InvoiceTheme.Gold }
        };
        cmb.Items.AddRange(items);
        block.Controls.Add(cmb);
        block.Controls.Add(lbl);
        host.Controls.Add(block);
        host.SizeChanged += (_, _) => block.Width = host.ClientSize.Width - 40;
        return cmb;
    }

    [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string? pszSubIdList);

    private static void TrySetDarkScrollbars(Control control)
    {
        try
        {
            SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
        }
        catch
        {
            // theming not available; ignore
        }
    }
}
