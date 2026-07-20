using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

/// <summary>Pick a product from inventory for sale or purchase invoices.</summary>
internal sealed class SelectProductDialog : Form
{
    private readonly bool _forPurchase;
    private readonly Guna2TextBox _txtSearch;
    private readonly DataGridView _grid;
    private List<ProductRecord> _filtered = [];

    public ProductRecord? Selected { get; private set; }

    public SelectProductDialog(bool forPurchase = false)
    {
        _forPurchase = forPurchase;
        SuspendLayout();
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = forPurchase ? "اختيار صنف للشراء" : "اختيار صنف من المخزون";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = InvoiceTheme.Background;
        ForeColor = InvoiceTheme.White;
        Font = InvoiceTheme.BodyFont;
        ClientSize = new Size(720, 480);
        RightToLeft = RightToLeft.No;

        ProductStore.Load();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = InvoiceTheme.Background,
            Padding = new Padding(16)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        _txtSearch = new Guna2TextBox
        {
            Dock = DockStyle.Fill,
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(90, InvoiceTheme.Gold),
            FillColor = InvoiceTheme.InputFill,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.BodyFont,
            PlaceholderText = "بحث بالاسم / الكود / الباركود",
            PlaceholderForeColor = InvoiceTheme.Muted,
            IconLeft = GlyphHelper.Create("\uE721", InvoiceTheme.Gold, 14),
            IconLeftSize = new Size(16, 16),
            FocusedState = { BorderColor = InvoiceTheme.Gold }
        };
        _txtSearch.TextChanged += (_, _) => BindGrid();

        var card = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Padding = new Padding(8),
            Margin = new Padding(0, 8, 0, 8)
        };

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = InvoiceTheme.Card,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            EnableHeadersVisualStyles = false,
            GridColor = InvoiceTheme.CardBorder,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 34 },
            Font = InvoiceTheme.SmallFont,
            ForeColor = InvoiceTheme.White,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = InvoiceTheme.Card,
                ForeColor = InvoiceTheme.White,
                SelectionBackColor = Color.FromArgb(48, InvoiceTheme.Gold),
                SelectionForeColor = InvoiceTheme.White
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = InvoiceTheme.RowAlt,
                ForeColor = InvoiceTheme.White,
                SelectionBackColor = Color.FromArgb(48, InvoiceTheme.Gold),
                SelectionForeColor = InvoiceTheme.White
            },
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = InvoiceTheme.Gold,
                Font = InvoiceTheme.TableHeaderFont
            },
            ColumnHeadersHeight = 34
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "الكود", FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "اسم الصنف", FillWeight = 28 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCat", HeaderText = "التصنيف", FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty", HeaderText = "المتاح", FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colPrice",
            HeaderText = _forPurchase ? "سعر الشراء" : "سعر البيع",
            FillWeight = 14
        });
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                ConfirmSelection();
            }
        };
        card.Controls.Add(_grid);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 6, 0, 0)
        };
        var btnOk = new Guna2Button
        {
            Text = "إضافة للفاتورة",
            Font = InvoiceTheme.MenuFont,
            ForeColor = Color.Black,
            FillColor = InvoiceTheme.Gold,
            BorderRadius = 8,
            Size = new Size(150, 38),
            Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand,
            HoverState = { FillColor = InvoiceTheme.GoldDark, ForeColor = Color.Black }
        };
        btnOk.Click += (_, _) => ConfirmSelection();
        var btnCancel = new Guna2Button
        {
            Text = "إلغاء",
            Font = InvoiceTheme.MenuFont,
            ForeColor = InvoiceTheme.White,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.Gold,
            BorderThickness = 1,
            BorderRadius = 8,
            Size = new Size(100, 38),
            Cursor = Cursors.Hand
        };
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        footer.Controls.Add(btnOk);
        footer.Controls.Add(btnCancel);

        root.Controls.Add(_txtSearch, 0, 0);
        root.Controls.Add(card, 0, 1);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);

        BindGrid();
        ResumeLayout(true);
    }

    private void BindGrid()
    {
        var query = ProductStore.Search(_txtSearch.Text, "الكل", "الكل");
        _filtered = _forPurchase
            ? query.ToList()
            : query.Where(p => p.Quantity > 0).ToList();
        _grid.Rows.Clear();
        foreach (var p in _filtered)
        {
            var price = _forPurchase ? p.PurchasePrice : p.SellingPrice;
            var r = _grid.Rows.Add(
                p.Code,
                p.Name,
                p.Category,
                p.Quantity,
                $"{price:N2}");
            _grid.Rows[r].Tag = p;
            if (!_forPurchase && p.IsLowStock)
            {
                _grid.Rows[r].Cells["colQty"].Style.ForeColor = InvoiceTheme.Danger;
            }
        }
    }

    private void ConfirmSelection()
    {
        if (_grid.CurrentRow?.Tag is not ProductRecord product)
        {
            AppMessageDialog.Warning(this, "اختر صنفاً من القائمة أولاً");
            return;
        }

        if (!_forPurchase && product.Quantity <= 0)
        {
            AppMessageDialog.Warning(this, "لا توجد كمية متاحة لهذا الصنف");
            return;
        }

        Selected = product;
        DialogResult = DialogResult.OK;
        Close();
    }
}
