using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

internal sealed class ProductsForm : Form
{
    private static readonly Color ProfitGreen = Color.FromArgb(80, 200, 120);

    private readonly System.Windows.Forms.Timer _clockTimer = new() { Interval = 1000 };
    private readonly bool _embedded;
    private Label _lblTime = null!;
    private Label _lblDate = null!;
    private Label _lblNetProfit = null!;
    private Label _lblInventoryValue = null!;
    private Label _lblProductCount = null!;
    private Label _lblLowStock = null!;
    private Label _lblTotalSales = null!;
    private Label _lblPage = null!;
    private Label _formTitle = null!;
    private Guna2TextBox _txtSearch = null!;
    private Guna2ComboBox _cmbCategoryFilter = null!;
    private Guna2ComboBox _cmbSupplierFilter = null!;
    private DataGridView _grid = null!;
    private DataGridView _lowStockGrid = null!;
    private DataGridView _movementsGrid = null!;
    private Bitmap _editIcon = null!;
    private Bitmap _deleteIcon = null!;

    private Guna2TextBox _txtName = null!;
    private Guna2TextBox _txtCode = null!;
    private Guna2TextBox _txtBarcode = null!;
    private Guna2ComboBox _cmbCategory = null!;
    private Guna2ComboBox _cmbSupplier = null!;
    private Guna2ComboBox _cmbUnit = null!;
    private Guna2TextBox _txtQty = null!;
    private Guna2TextBox _txtMinStock = null!;
    private Guna2TextBox _txtPurchase = null!;
    private Guna2TextBox _txtSelling = null!;
    private PictureBox _imgPreview = null!;
    private string? _pendingImagePath;
    private string? _editingId;
    private int _editingPreviousQty;

    private List<ProductRecord> _filtered = [];
    private int _page;
    private const int PageSize = 8;

    public ProductsForm(bool embedded = false)
    {
        _embedded = embedded;
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = InvoiceTheme.Background;
        ClientSize = new Size(1280, 720);
        DoubleBuffered = true;
        Font = InvoiceTheme.BodyFont;
        ForeColor = InvoiceTheme.White;
        FormBorderStyle = _embedded ? FormBorderStyle.None : FormBorderStyle.Sizable;
        MaximizeBox = !_embedded;
        MinimizeBox = !_embedded;
        MinimumSize = _embedded ? Size.Empty : new Size(1100, 650);
        Name = "ProductsForm";
        RightToLeft = RightToLeft.No;
        ShowIcon = !_embedded;
        ShowInTaskbar = !_embedded;
        StartPosition = FormStartPosition.Manual;
        Text = _embedded ? string.Empty : "الأصناف - Ibrahim Abdo Auto Service";
        WindowState = FormWindowState.Normal;

        ProductStore.Load();
        _editIcon = GlyphHelper.Create("\uE70F", InvoiceTheme.Gold, 14);
        _deleteIcon = GlyphHelper.Create("\uE74D", InvoiceTheme.Danger, 14);

        BuildUi();
        RefreshData();

        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();
        ResumeLayout(true);
    }

    private void BuildUi()
    {
        var main = BuildMain();
        main.Dock = DockStyle.Fill;
        Controls.Add(main);
    }

    private Control BuildMain()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = InvoiceTheme.Background,
            Padding = new Padding(14, 8, 14, 10)
        };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        main.Controls.Add(BuildTopBar(), 0, 0);

        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
        split.Controls.Add(BuildLeftPane(), 0, 0);
        split.Controls.Add(BuildRightPane(), 1, 0);
        main.Controls.Add(split, 0, 1);
        return main;
    }

    private Control BuildTopBar()
    {
        var bar = new Panel { Dock = DockStyle.Fill, BackColor = InvoiceTheme.Background };
        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "الأصناف",
            Font = new Font(InvoiceTheme.Family.FontFamily, 20F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = InvoiceTheme.White,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var right = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 10, 0, 0)
        };
        _lblDate = new Label { AutoSize = true, ForeColor = InvoiceTheme.Muted, Font = InvoiceTheme.SmallFont, Margin = new Padding(8, 8, 12, 0) };
        _lblTime = new Label { AutoSize = true, ForeColor = InvoiceTheme.Muted, Font = InvoiceTheme.SmallFont, Margin = new Padding(8, 8, 8, 0) };
        right.Controls.Add(CreateChromeIcon("\uE7E7"));
        right.Controls.Add(_lblDate);
        right.Controls.Add(CreateChromeIcon("\uE121"));
        right.Controls.Add(_lblTime);

        bar.Controls.Add(title);
        bar.Controls.Add(right);
        return bar;
    }

    private Control BuildLeftPane()
    {
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Margin = new Padding(0, 0, 8, 0),
            BackColor = Color.Transparent
        };
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));

        host.Controls.Add(BuildToolbar(), 0, 0);
        host.Controls.Add(BuildStats(), 0, 1);
        host.Controls.Add(BuildProductsGrid(), 0, 2);
        host.Controls.Add(BuildPagination(), 0, 3);
        host.Controls.Add(BuildBottomSections(), 0, 4);
        return host;
    }

    private Control BuildToolbar()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var filters = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 0)
        };

        _txtSearch = new Guna2TextBox
        {
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(90, InvoiceTheme.Gold),
            FillColor = InvoiceTheme.InputFill,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.BodyFont,
            PlaceholderText = "بحث بالاسم / الكود / الباركود",
            PlaceholderForeColor = InvoiceTheme.Muted,
            Width = 240,
            Height = 36,
            Margin = new Padding(0, 0, 8, 0),
            IconLeft = GlyphHelper.Create("\uE721", InvoiceTheme.Gold, 14),
            IconLeftSize = new Size(16, 16),
            FocusedState = { BorderColor = InvoiceTheme.Gold }
        };
        _txtSearch.TextChanged += (_, _) => { _page = 0; RefreshData(); };

        _cmbCategoryFilter = CreateFilterCombo(140, "كل التصنيفات");
        _cmbCategoryFilter.Items.Add("الكل");
        foreach (var c in ProductStore.Categories)
        {
            _cmbCategoryFilter.Items.Add(c);
        }

        _cmbCategoryFilter.SelectedIndex = 0;
        _cmbCategoryFilter.SelectedIndexChanged += (_, _) => { _page = 0; RefreshData(); };

        _cmbSupplierFilter = CreateFilterCombo(160, "كل الموردين");
        _cmbSupplierFilter.Items.Add("الكل");
        foreach (var s in ProductStore.Suppliers)
        {
            _cmbSupplierFilter.Items.Add(s);
        }

        _cmbSupplierFilter.SelectedIndex = 0;
        _cmbSupplierFilter.SelectedIndexChanged += (_, _) => { _page = 0; RefreshData(); };

        filters.Controls.Add(_txtSearch);
        filters.Controls.Add(_cmbCategoryFilter);
        filters.Controls.Add(_cmbSupplierFilter);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 0)
        };
        buttons.Controls.Add(CreateToolbarButton("+ إضافة صنف", true, (_, _) => BeginAddNew()));
        buttons.Controls.Add(CreateToolbarButton("تقرير المخزون", false, (_, _) => ExportReport()));

        bar.Controls.Add(filters, 0, 0);
        bar.Controls.Add(buttons, 1, 0);
        return bar;
    }

    private Control BuildStats()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 4)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));

        _lblNetProfit = new Label();
        _lblInventoryValue = new Label();
        _lblProductCount = new Label();
        _lblLowStock = new Label();
        _lblTotalSales = new Label();

        row.Controls.Add(CreateStatCard("صافي الربح", _lblNetProfit, "\uE9D2", highlight: true), 0, 0);
        row.Controls.Add(CreateStatCard("قيمة المخزون", _lblInventoryValue, "\uE8F1"), 1, 0);
        row.Controls.Add(CreateStatCard("عدد الأصناف", _lblProductCount, "\uE81E"), 2, 0);
        row.Controls.Add(CreateStatCard("أصناف منخفضة", _lblLowStock, "\uE7BA"), 3, 0);
        row.Controls.Add(CreateStatCard("إجمالي المبيعات", _lblTotalSales, "\uE8A5"), 4, 0);
        return row;
    }

    private Control BuildProductsGrid()
    {
        var card = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Margin = new Padding(4),
            Padding = new Padding(8),
            ShadowDecoration = { Enabled = true, Depth = 10, Color = Color.Black, BorderRadius = InvoiceTheme.Radius }
        };

        _grid = CreateGrid();
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIndex", HeaderText = "#", FillWeight = 4 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "كود الصنف", FillWeight = 9 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "اسم الصنف", FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCategory", HeaderText = "التصنيف", FillWeight = 8 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty", HeaderText = "الكمية", FillWeight = 6 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBuy", HeaderText = "سعر الشراء", FillWeight = 8 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSell", HeaderText = "سعر البيع", FillWeight = 8 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colProfitItem", HeaderText = "ربح القطعة", FillWeight = 8 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colProfitTotal", HeaderText = "إجمالي الربح", FillWeight = 8 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSupplier", HeaderText = "المورد", FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUpdated", HeaderText = "آخر تحديث", FillWeight = 8 });
        _grid.Columns.Add(new DataGridViewImageColumn { Name = "colEdit", HeaderText = "إجراء", FillWeight = 5, Image = _editIcon, ImageLayout = DataGridViewImageCellLayout.Zoom });
        _grid.Columns.Add(new DataGridViewImageColumn { Name = "colDelete", HeaderText = "", FillWeight = 5, Image = _deleteIcon, ImageLayout = DataGridViewImageCellLayout.Zoom });
        _grid.CellFormatting += OnGridCellFormatting;
        _grid.CellClick += OnGridCellClick;
        EnableDoubleBuffering(_grid);

        card.Controls.Add(_grid);
        return card;
    }

    private Control BuildPagination()
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            Padding = new Padding(4, 2, 0, 0)
        };
        var prev = CreateToolbarButton("السابق", false, (_, _) =>
        {
            if (_page > 0) { _page--; BindGrid(); }
        });
        var next = CreateToolbarButton("التالي", false, (_, _) =>
        {
            if ((_page + 1) * PageSize < _filtered.Count) { _page++; BindGrid(); }
        });
        _lblPage = new Label
        {
            AutoSize = true,
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont,
            Margin = new Padding(12, 10, 12, 0),
            Text = "صفحة 1"
        };
        bar.Controls.Add(prev);
        bar.Controls.Add(_lblPage);
        bar.Controls.Add(next);
        return bar;
    }

    private Control BuildBottomSections()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 0)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        row.Controls.Add(BuildMiniCard("تنبيهات المخزون المنخفض", out _lowStockGrid, BuildLowStockColumns), 0, 0);
        row.Controls.Add(BuildMiniCard("حركة المخزون الأخيرة", out _movementsGrid, BuildMovementColumns), 1, 0);
        return row;
    }

    private Control BuildMiniCard(string title, out DataGridView grid, Action<DataGridView> addColumns)
    {
        var card = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Margin = new Padding(4, 0, 4, 0),
            Padding = new Padding(8, 4, 8, 8),
            ShadowDecoration = { Enabled = true, Depth = 8, Color = Color.Black, BorderRadius = InvoiceTheme.Radius }
        };

        var header = new Label
        {
            Dock = DockStyle.Top,
            Height = 26,
            Text = title,
            ForeColor = InvoiceTheme.Gold,
            Font = InvoiceTheme.SectionFont,
            TextAlign = ContentAlignment.MiddleLeft
        };

        grid = CreateGrid();
        grid.Dock = DockStyle.Fill;
        grid.RowTemplate.Height = 26;
        addColumns(grid);
        EnableDoubleBuffering(grid);

        card.Controls.Add(grid);
        card.Controls.Add(header);
        return card;
    }

    private static void BuildLowStockColumns(DataGridView g)
    {
        g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الصنف", FillWeight = 45 });
        g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الكمية", FillWeight = 20 });
        g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الحد الأدنى", FillWeight = 35 });
    }

    private static void BuildMovementColumns(DataGridView g)
    {
        g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الصنف", FillWeight = 35 });
        g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "النوع", FillWeight = 20 });
        g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الكمية", FillWeight = 15 });
        g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "التاريخ", FillWeight = 30 });
    }

    private Control BuildRightPane()
    {
        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Margin = new Padding(6, 0, 0, 0),
            Padding = new Padding(4, 8, 8, 8)
        };

        var card = new Guna2Panel
        {
            Width = 312,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(312, 620),
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(12, 8, 12, 12),
            ShadowDecoration = { Enabled = true, Depth = 10, Color = Color.Black, BorderRadius = InvoiceTheme.Radius }
        };

        _formTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            Text = "إضافة صنف جديد",
            ForeColor = InvoiceTheme.Gold,
            Font = InvoiceTheme.SectionFont,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 0,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0)
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        _txtName = AddFormField(body, "اسم الصنف", "مثال: زيت موتور");
        _txtCode = AddFormField(body, "كود الصنف", "مثال: OIL-5W30");
        _txtBarcode = AddFormField(body, "الباركود", "اختياري");
        _cmbCategory = AddFormCombo(body, "التصنيف", ProductStore.Categories);
        _cmbSupplier = AddFormCombo(body, "المورد", ProductStore.Suppliers);
        _cmbUnit = AddFormCombo(body, "الوحدة", ProductStore.Units);
        _txtQty = AddFormField(body, "الكمية الحالية", "0");
        _txtMinStock = AddFormField(body, "حد التنبيه الأدنى", "5");
        _txtPurchase = AddFormField(body, "سعر الشراء", "0.00");
        _txtSelling = AddFormField(body, "سعر البيع", "0.00");

        var imgHost = new Panel { Dock = DockStyle.Top, Height = 118, Margin = new Padding(0, 6, 0, 6), BackColor = Color.Transparent };
        var imgLbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = "صورة الصنف",
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont
        };
        var imgRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0)
        };
        _imgPreview = new PictureBox
        {
            Width = 72,
            Height = 72,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = InvoiceTheme.InputFill,
            SizeMode = PictureBoxSizeMode.Zoom,
            Margin = new Padding(0, 0, 10, 0)
        };
        var uploadBtn = CreateToolbarButton("رفع صورة", false, (_, _) => PickImage());
        uploadBtn.Margin = new Padding(0, 18, 0, 0);
        imgRow.Controls.Add(_imgPreview);
        imgRow.Controls.Add(uploadBtn);
        imgHost.Controls.Add(imgRow);
        imgHost.Controls.Add(imgLbl);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 8, 0, 0),
            BackColor = Color.Transparent
        };
        actions.Controls.Add(CreateToolbarButton("حفظ الصنف", true, (_, _) => SaveProduct()));
        actions.Controls.Add(CreateToolbarButton("إلغاء", false, (_, _) => BeginAddNew()));

        card.Controls.Add(actions);
        card.Controls.Add(imgHost);
        card.Controls.Add(body);
        card.Controls.Add(_formTitle);
        scroll.Controls.Add(card);
        return scroll;
    }

    private void RefreshData()
    {
        var cat = _cmbCategoryFilter.SelectedItem?.ToString() ?? "الكل";
        var sup = _cmbSupplierFilter.SelectedItem?.ToString() ?? "الكل";
        _filtered = ProductStore.Search(_txtSearch.Text, cat, sup).ToList();
        if (_page * PageSize >= Math.Max(1, _filtered.Count))
        {
            _page = 0;
        }

        _lblNetProfit.Text = FormatMoney(ProductStore.NetProfit);
        _lblInventoryValue.Text = FormatMoney(ProductStore.InventoryValue);
        _lblProductCount.Text = ProductStore.ProductCount.ToString();
        _lblLowStock.Text = ProductStore.LowStockCount.ToString();
        _lblTotalSales.Text = FormatMoney(ProductStore.TotalProductSales);

        BindGrid();
        BindLowStock();
        BindMovements();
    }

    private void BindGrid()
    {
        _grid.Rows.Clear();
        var pageItems = _filtered.Skip(_page * PageSize).Take(PageSize).ToList();
        for (var i = 0; i < pageItems.Count; i++)
        {
            var p = pageItems[i];
            var idx = _page * PageSize + i + 1;
            var row = _grid.Rows.Add(
                idx,
                p.Code,
                p.Name,
                p.Category,
                p.Quantity,
                FormatMoney(p.PurchasePrice),
                FormatMoney(p.SellingPrice),
                FormatMoney(p.ProfitPerItem),
                FormatMoney(p.TotalProfit),
                p.Supplier,
                p.UpdatedAt.ToString("dd/MM/yyyy"),
                _editIcon,
                _deleteIcon);
            _grid.Rows[row].Tag = p.Id;
        }

        var pages = Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)PageSize));
        _lblPage.Text = $"صفحة {_page + 1} من {pages}  •  {_filtered.Count} صنف";
    }

    private void BindLowStock()
    {
        _lowStockGrid.Rows.Clear();
        foreach (var p in ProductStore.LowStockProducts().Take(8))
        {
            var r = _lowStockGrid.Rows.Add(p.Name, p.Quantity, p.MinStock);
            _lowStockGrid.Rows[r].DefaultCellStyle.ForeColor = InvoiceTheme.Danger;
        }
    }

    private void BindMovements()
    {
        _movementsGrid.Rows.Clear();
        foreach (var m in ProductStore.RecentMovements(10))
        {
            var r = _movementsGrid.Rows.Add(
                m.ProductName,
                m.Type == "Stock In" ? "دخول" : "خروج",
                m.Quantity,
                m.At.ToString("dd/MM HH:mm"));
            _movementsGrid.Rows[r].DefaultCellStyle.ForeColor =
                m.Type == "Stock In" ? ProfitGreen : InvoiceTheme.Danger;
        }
    }

    private void OnGridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        var col = _grid.Columns[e.ColumnIndex].Name;
        if (col is "colProfitItem" or "colProfitTotal")
        {
            e.CellStyle!.ForeColor = ProfitGreen;
        }
        else if (col == "colQty" && e.Value is int or string)
        {
            if (_grid.Rows[e.RowIndex].Tag is string id && ProductStore.Find(id) is { IsLowStock: true })
            {
                e.CellStyle!.ForeColor = InvoiceTheme.Danger;
                e.CellStyle.Font = new Font(InvoiceTheme.BodyFont, FontStyle.Bold);
            }
        }
    }

    private void OnGridCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        if (_grid.Rows[e.RowIndex].Tag is not string id)
        {
            return;
        }

        var col = _grid.Columns[e.ColumnIndex].Name;
        if (col == "colEdit")
        {
            LoadForEdit(id);
        }
        else if (col == "colDelete")
        {
            DeleteProduct(id);
        }
    }

    private void BeginAddNew()
    {
        _editingId = null;
        _editingPreviousQty = 0;
        _pendingImagePath = null;
        _formTitle.Text = "إضافة صنف جديد";
        _txtName.Text = "";
        _txtCode.Text = "";
        _txtBarcode.Text = "";
        if (_cmbCategory.Items.Count > 0) _cmbCategory.SelectedIndex = 0;
        if (_cmbSupplier.Items.Count > 0) _cmbSupplier.SelectedIndex = 0;
        if (_cmbUnit.Items.Count > 0) _cmbUnit.SelectedIndex = 0;
        _txtQty.Text = "0";
        _txtMinStock.Text = "5";
        _txtPurchase.Text = "";
        _txtSelling.Text = "";
        _imgPreview.Image = null;
    }

    private void LoadForEdit(string id)
    {
        var p = ProductStore.Find(id);
        if (p is null)
        {
            return;
        }

        _editingId = p.Id;
        _editingPreviousQty = p.Quantity;
        _pendingImagePath = p.ImagePath;
        _formTitle.Text = "تعديل الصنف";
        _txtName.Text = p.Name;
        _txtCode.Text = p.Code;
        _txtBarcode.Text = p.Barcode;
        SelectCombo(_cmbCategory, p.Category);
        SelectCombo(_cmbSupplier, p.Supplier);
        SelectCombo(_cmbUnit, p.Unit);
        _txtQty.Text = p.Quantity.ToString();
        _txtMinStock.Text = p.MinStock.ToString();
        _txtPurchase.Text = p.PurchasePrice.ToString("0.##");
        _txtSelling.Text = p.SellingPrice.ToString("0.##");
        LoadPreview(p.ImagePath);
    }

    private void SaveProduct()
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

        if (ProductStore.CodeExists(code, _editingId))
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

        if (_editingId is not null && ProductStore.Find(_editingId) is { } existing)
        {
            existing.Name = name;
            existing.Code = code;
            existing.Barcode = _txtBarcode.Text.Trim();
            existing.Category = _cmbCategory.SelectedItem?.ToString() ?? "أخرى";
            existing.Supplier = _cmbSupplier.SelectedItem?.ToString() ?? "";
            existing.Unit = _cmbUnit.SelectedItem?.ToString() ?? "قطعة";
            existing.Quantity = qty;
            existing.MinStock = min;
            existing.PurchasePrice = buy;
            existing.SellingPrice = sell;
            existing.ImagePath = PersistImage(_editingId, _pendingImagePath) ?? existing.ImagePath;
            ProductStore.Update(existing, _editingPreviousQty);
            AppMessageDialog.Success(this, "تم تحديث الصنف بنجاح", "الأصناف");
        }
        else
        {
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
            product.ImagePath = PersistImage(product.Id, _pendingImagePath);
            ProductStore.Add(product);
            AppMessageDialog.Success(this, "تم حفظ الصنف بنجاح", "الأصناف");
        }

        BeginAddNew();
        RefreshData();
    }

    private void DeleteProduct(string id)
    {
        var p = ProductStore.Find(id);
        if (p is null)
        {
            return;
        }

        var confirm = MessageBox.Show(this,
            $"حذف الصنف «{p.Name}»؟",
            "تأكيد الحذف",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        ProductStore.Remove(id);
        if (_editingId == id)
        {
            BeginAddNew();
        }

        RefreshData();
    }

    private void PickImage()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.webp",
            Title = "اختر صورة الصنف"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _pendingImagePath = dlg.FileName;
        LoadPreview(_pendingImagePath);
    }

    private void LoadPreview(string? path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                _imgPreview.Image = null;
                return;
            }

            using var fs = File.OpenRead(path);
            _imgPreview.Image = Image.FromStream(fs);
        }
        catch
        {
            _imgPreview.Image = null;
        }
    }

    private static string? PersistImage(string productId, string? sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return null;
        }

        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "Data", "ProductImages");
            Directory.CreateDirectory(dir);
            var ext = Path.GetExtension(sourcePath);
            if (string.IsNullOrWhiteSpace(ext))
            {
                ext = ".png";
            }

            var dest = Path.Combine(dir, productId + ext);
            if (!string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(dest), StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(sourcePath, dest, overwrite: true);
            }

            return dest;
        }
        catch
        {
            return sourcePath;
        }
    }

    private void ExportReport()
    {
        var lines = new List<string>
        {
            "تقرير المخزون - Ibrahim Abdo Auto Service",
            $"التاريخ: {DateTime.Now:dd/MM/yyyy HH:mm}",
            $"صافي الربح: {FormatMoney(ProductStore.NetProfit)}",
            $"قيمة المخزون: {FormatMoney(ProductStore.InventoryValue)}",
            $"عدد الأصناف: {ProductStore.ProductCount}",
            $"أصناف منخفضة: {ProductStore.LowStockCount}",
            "",
            "الكود,الاسم,التصنيف,الكمية,الشراء,البيع,الربح,المورد"
        };
        lines.AddRange(_filtered.Select(p =>
            $"{p.Code},{p.Name},{p.Category},{p.Quantity},{p.PurchasePrice},{p.SellingPrice},{p.ProfitPerItem},{p.Supplier}"));

        var dir = Path.Combine(AppContext.BaseDirectory, "Reports");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"inventory-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        File.WriteAllLines(path, lines);
        AppMessageDialog.Success(this, $"تم تصدير تقرير المخزون.\r\n{Path.GetFileName(path)}", "تقرير المخزون");
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch
        {
            // ignore open errors
        }
    }

    private static Guna2TextBox AddFormField(TableLayoutPanel body, string label, string placeholder)
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 4),
            BackColor = Color.Transparent
        };
        var lbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 18,
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
        var row = body.RowCount++;
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        body.Controls.Add(host, 0, row);
        return input;
    }

    private static Guna2ComboBox AddFormCombo(TableLayoutPanel body, string label, IEnumerable<string> items)
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 4),
            BackColor = Color.Transparent
        };
        var lbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 18,
            Text = label,
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont
        };
        var combo = CreateFilterCombo(0, label);
        combo.Dock = DockStyle.Fill;
        foreach (var item in items)
        {
            combo.Items.Add(item);
        }

        if (combo.Items.Count > 0)
        {
            combo.SelectedIndex = 0;
        }

        host.Controls.Add(combo);
        host.Controls.Add(lbl);
        var row = body.RowCount++;
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        body.Controls.Add(host, 0, row);
        return combo;
    }

    private static Guna2ComboBox CreateFilterCombo(int width, string placeholder)
    {
        var combo = new Guna2ComboBox
        {
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(90, InvoiceTheme.Gold),
            FillColor = InvoiceTheme.InputFill,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.SmallFont,
            Height = 36,
            Width = width > 0 ? width : 200,
            Margin = new Padding(0, 0, 8, 0),
            FocusedState = { BorderColor = InvoiceTheme.Gold }
        };
        if (width > 0)
        {
            // keep placeholder unused; filters start with "الكل"
            _ = placeholder;
        }

        return combo;
    }

    private static Control CreateStatCard(string title, Label valueLabel, string glyph, bool highlight = false)
    {
        var card = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = highlight ? Color.FromArgb(40, InvoiceTheme.Gold) : InvoiceTheme.Card,
            BorderColor = highlight ? InvoiceTheme.Gold : InvoiceTheme.CardBorder,
            BorderThickness = highlight ? 2 : 1,
            BorderRadius = InvoiceTheme.Radius,
            Margin = new Padding(4),
            Padding = new Padding(10, 8, 10, 8),
            ShadowDecoration = { Enabled = true, Depth = 8, Color = Color.Black, BorderRadius = InvoiceTheme.Radius }
        };

        var icon = new PictureBox
        {
            Dock = DockStyle.Left,
            Width = highlight ? 40 : 36,
            SizeMode = PictureBoxSizeMode.CenterImage,
            Image = GlyphHelper.Create(glyph, InvoiceTheme.Gold, highlight ? 22 : 18),
            BackColor = Color.Transparent
        };

        var texts = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var titleLbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = title,
            ForeColor = highlight ? InvoiceTheme.Gold : InvoiceTheme.Muted,
            Font = highlight ? InvoiceTheme.BodyFont : InvoiceTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleLeft
        };
        valueLabel.Dock = DockStyle.Fill;
        valueLabel.ForeColor = InvoiceTheme.Gold;
        valueLabel.Font = highlight
            ? new Font(InvoiceTheme.Family.FontFamily, 14F, FontStyle.Bold, GraphicsUnit.Point)
            : InvoiceTheme.SectionFont;
        valueLabel.TextAlign = ContentAlignment.MiddleLeft;
        valueLabel.Text = "0";
        texts.Controls.Add(valueLabel);
        texts.Controls.Add(titleLbl);

        card.Controls.Add(texts);
        card.Controls.Add(icon);
        return card;
    }

    private static DataGridView CreateGrid()
    {
        var g = new DataGridView
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
            AllowUserToResizeRows = false,
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
                SelectionForeColor = InvoiceTheme.White,
                Padding = new Padding(4, 0, 4, 0)
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
                Font = InvoiceTheme.TableHeaderFont,
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 4, 0)
            },
            ColumnHeadersHeight = 34
        };
        return g;
    }

    private static Guna2Button CreateToolbarButton(string text, bool primary, EventHandler onClick)
    {
        var btn = new Guna2Button
        {
            Text = text,
            Font = InvoiceTheme.SmallFont,
            Height = 36,
            MinimumSize = new Size(100, 36),
            Padding = new Padding(12, 0, 12, 0),
            BorderRadius = 8,
            Margin = new Padding(0, 0, 8, 0),
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
        var textW = TextRenderer.MeasureText(text, btn.Font).Width;
        btn.Width = Math.Max(btn.MinimumSize.Width, textW + 36);
        btn.Click += onClick;
        return btn;
    }

    private static Guna2Button CreateChromeIcon(string glyph) =>
        new()
        {
            Size = new Size(34, 34),
            BorderRadius = 8,
            FillColor = Color.Transparent,
            Font = InvoiceTheme.IconFont,
            Text = glyph,
            ForeColor = InvoiceTheme.Muted,
            Margin = new Padding(2, 0, 2, 0),
            HoverState = { FillColor = Color.FromArgb(40, 40, 40), ForeColor = InvoiceTheme.White }
        };

    private static void SelectCombo(Guna2ComboBox combo, string value)
    {
        for (var i = 0; i < combo.Items.Count; i++)
        {
            if (string.Equals(combo.Items[i]?.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedIndex = i;
                return;
            }
        }

        if (combo.Items.Count > 0)
        {
            combo.SelectedIndex = 0;
        }
    }

    private static string FormatMoney(decimal value) => $"{value:N0} ج.م";

    private void UpdateClock()
    {
        var now = DateTime.Now;
        _lblTime.Text = now.ToString("hh:mm tt");
        _lblDate.Text = now.ToString("dd MMM yyyy");
    }

    private static void EnableDoubleBuffering(DataGridView grid)
    {
        typeof(DataGridView)
            .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(grid, true, null);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _clockTimer.Stop();
        _clockTimer.Dispose();
        _editIcon.Dispose();
        _deleteIcon.Dispose();
        base.OnFormClosed(e);
    }
}
