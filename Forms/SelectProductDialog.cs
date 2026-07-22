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
        WindowTheme.Attach(this);
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
            FillColor = InvoiceTheme.Background,
            BorderColor = InvoiceTheme.Background,
            BorderThickness = 0,
            BorderRadius = InvoiceTheme.Radius,
            Padding = new Padding(8),
            Margin = new Padding(0, 8, 0, 8)
        };

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = InvoiceTheme.Background,
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
                BackColor = InvoiceTheme.Background,
                ForeColor = InvoiceTheme.White,
                SelectionBackColor = Color.FromArgb(40, 40, 40),
                SelectionForeColor = InvoiceTheme.White
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = InvoiceTheme.Background,
                ForeColor = InvoiceTheme.White,
                SelectionBackColor = Color.FromArgb(40, 40, 40),
                SelectionForeColor = InvoiceTheme.White
            },
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = InvoiceTheme.Background,
                ForeColor = InvoiceTheme.Gold,
                Font = InvoiceTheme.TableHeaderFont
            },
            ColumnHeadersHeight = 34
        };
        _grid.HandleCreated += (_, _) => TrySetDarkScrollbars(_grid);
        _grid.CellFormatting += OnGridCellFormatting;
        _grid.SelectionChanged += (_, _) => _grid.Invalidate();
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

        var footer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 6, 0, 0)
        };
        var rightGroup = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent
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
        rightGroup.Controls.Add(btnOk);
        rightGroup.Controls.Add(btnCancel);

        var btnNew = new Guna2Button
        {
            Dock = DockStyle.Left,
            Width = 175,
            Text = "+ إضافة صنف جديد",
            Font = InvoiceTheme.MenuFont,
            ForeColor = InvoiceTheme.White,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.Gold,
            BorderThickness = 1,
            BorderRadius = 8,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0),
            HoverState = { FillColor = InvoiceTheme.Gold, BorderColor = InvoiceTheme.Gold, ForeColor = Color.Black }
        };
        btnNew.Click += (_, _) => OpenAddProduct();

        footer.Controls.Add(rightGroup);
        footer.Controls.Add(btnNew);

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

    private void OnGridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count)
        {
            return;
        }

        e.CellStyle!.SelectionBackColor = Color.FromArgb(40, 40, 40);
        e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;

        if (_grid.Rows[e.RowIndex].Selected)
        {
            e.CellStyle.Font = new Font(e.CellStyle.Font ?? _grid.Font, FontStyle.Bold);
        }
    }

    private void OpenAddProduct()
    {
        using var dlg = new AddProductDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK || dlg.Result is null)
        {
            return;
        }

        BindGrid();
        for (var i = 0; i < _grid.Rows.Count; i++)
        {
            if (_grid.Rows[i].Tag is ProductRecord p && p.Id == dlg.Result.Id)
            {
                _grid.ClearSelection();
                _grid.Rows[i].Selected = true;
                _grid.CurrentCell = _grid.Rows[i].Cells[0];
                break;
            }
        }
    }

    [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string? pszSubIdList);

    private static void TrySetDarkScrollbars(Control control)
    {
        try
        {
            SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
            foreach (Control child in control.Controls)
            {
                if (child is ScrollBar)
                {
                    SetWindowTheme(child.Handle, "DarkMode_Explorer", null);
                    child.BackColor = InvoiceTheme.Background;
                }
            }
        }
        catch
        {
            // theming not available; ignore
        }
    }
}
