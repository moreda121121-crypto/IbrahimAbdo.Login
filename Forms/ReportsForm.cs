using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Services;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

internal sealed class ReportsForm : Form
{
    private static readonly Color ProfitGreen = Color.FromArgb(80, 200, 120);

    private readonly System.Windows.Forms.Timer _clockTimer = new() { Interval = 1000 };
    private readonly bool _embedded;
    private readonly ReportFilter _filter = new();

    private Label _lblDate = null!;
    private Label _lblSales = null!;
    private Label _lblInvoices = null!;
    private Label _lblAvg = null!;
    private Label _lblProfit = null!;
    private Label _lblNewCust = null!;
    private Label _lblSalesTrend = null!;
    private Label _lblInvTrend = null!;
    private Label _lblAvgTrend = null!;
    private Label _lblProfitTrend = null!;
    private Label _lblCustTrend = null!;
    private Label _lblPage = null!;
    private Label _donutCenter = null!;

    private Guna2ComboBox _cmbType = null!;
    private Guna2ComboBox _cmbCategory = null!;
    private Guna2ComboBox _cmbTech = null!;
    private Guna2TextBox _txtFrom = null!;
    private Guna2TextBox _txtTo = null!;

    private Panel _lineChart = null!;
    private Panel _donutChart = null!;
    private DataGridView _servicesGrid = null!;
    private DataGridView _productsGrid = null!;
    private FlowLayoutPanel _quickHost = null!;
    private FlowLayoutPanel _pager = null!;
    private FlowLayoutPanel _legend = null!;

    private ReportSnapshot _snap = new();
    private int _quickPage;
    private const int QuickPageSize = 8;

    private static readonly (string title, string desc, string glyph, string focus)[] QuickReports =
    [
        ("تقرير المبيعات", "ملخص فواتير البيع والإيرادات", "\uE9D2", "المبيعات"),
        ("تقرير الخدمات", "أداء الخدمات الأكثر طلباً", "\uE90F", "الخدمات"),
        ("تقرير العملاء", "العملاء الجدد وإجمالي التعامل", "\uE77B", "العملاء"),
        ("تقرير الفنيين", "أداء الفنيين حسب الفواتير", "\uE718", "الفنيين"),
        ("تقرير الأرباح والخسائر", "المبيعات مقابل التكاليف", "\uE8C8", "الأرباح والخسائر"),
        ("تقرير المشتريات", "فواتير الشراء والموردين", "\uE8CB", "المشتريات"),
        ("تقرير المخزون", "الأصناف والكميات الحالية", "\uE8F1", "المخزون"),
        ("تقرير الضرائب", "الضرائب المحصلة من الفواتير", "\uE8A5", "الضرائب"),
        ("تقرير يومي", "المبيعات حسب اليوم", "\uE787", "المبيعات"),
        ("تقرير التصنيفات", "المبيعات حسب التصنيف", "\uE81E", "الأصناف"),
        ("تقرير أكثر الخدمات", "أعلى الخدمات مبيعاً", "\uE8FD", "الخدمات"),
        ("تقرير أكثر الأصناف", "أعلى الأصناف مبيعاً", "\uE71B", "الأصناف"),
        ("تقرير شامل", "تصدير كل أقسام التقارير", "\uE8A5", "الكل"),
    ];

    public ReportsForm(bool embedded = false)
    {
        _embedded = embedded;
        SuspendLayout();
        WindowTheme.Attach(this);
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
        Name = "ReportsForm";
        RightToLeft = RightToLeft.No;
        ShowIcon = !_embedded;
        ShowInTaskbar = !_embedded;
        StartPosition = FormStartPosition.Manual;
        Text = _embedded ? string.Empty : "التقارير - Ibrahim Abdo Auto Service";

        ReportsService.EnsureLoaded();
        BuildUi();
        LoadFilters();
        ApplyReport();

        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();
        ResumeLayout(true);
    }

    public void RefreshData()
    {
        ReportsService.EnsureLoaded();
        LoadFilters(preserveSelection: true);
        ApplyReport();
    }

    private void LoadFilters(bool preserveSelection = false)
    {
        var typeSel = _cmbType.SelectedItem?.ToString();
        var catSel = _cmbCategory.SelectedItem?.ToString();
        var techSel = _cmbTech.SelectedItem?.ToString();

        _cmbType.Items.Clear();
        foreach (var t in new[] { "الكل", "المبيعات", "الخدمات", "العملاء", "الفنيين", "الأرباح والخسائر", "المشتريات", "المخزون", "الضرائب" })
        {
            _cmbType.Items.Add(t);
        }

        _cmbCategory.Items.Clear();
        _cmbCategory.Items.Add("الكل");
        foreach (var c in ProductStore.Categories)
        {
            _cmbCategory.Items.Add(c);
        }

        _cmbTech.Items.Clear();
        _cmbTech.Items.Add("الكل");
        foreach (var n in TechnicianStore.Names())
        {
            _cmbTech.Items.Add(n);
        }

        SelectOrDefault(_cmbType, preserveSelection ? typeSel : "الكل");
        SelectOrDefault(_cmbCategory, preserveSelection ? catSel : "الكل");
        SelectOrDefault(_cmbTech, preserveSelection ? techSel : "الكل");

        if (!preserveSelection || string.IsNullOrWhiteSpace(_txtFrom.Text))
        {
            _txtFrom.Text = _filter.From.ToString("dd/MM/yyyy");
            _txtTo.Text = _filter.To.ToString("dd/MM/yyyy");
        }
    }

    private void ApplyReport()
    {
        ReadFilterFromUi();
        _snap = ReportsService.Build(_filter);
        BindKpis();
        BindTables();
        BindQuickReports();
        BuildPager();
        _lineChart.Invalidate();
        _donutChart.Invalidate();
        _legend.Controls.Clear();
        foreach (var slice in _snap.CategorySales.Take(6))
        {
            _legend.Controls.Add(CreateLegendItem(slice));
        }

        _donutCenter.Text = $"{_snap.Kpis.TotalSales:N0}\r\nإجمالي المبيعات";
    }

    private void ReadFilterFromUi()
    {
        if (DateTime.TryParse(_txtFrom.Text.Trim(), out var from))
        {
            _filter.From = from;
        }

        if (DateTime.TryParse(_txtTo.Text.Trim(), out var to))
        {
            _filter.To = to;
        }

        _filter.ReportType = _cmbType.SelectedItem?.ToString() ?? "الكل";
        _filter.Category = _cmbCategory.SelectedItem?.ToString() ?? "الكل";
        _filter.Technician = _cmbTech.SelectedItem?.ToString() ?? "الكل";
    }

    private void BindKpis()
    {
        var k = _snap.Kpis;
        _lblSales.Text = $"{k.TotalSales:N0} ج.م";
        _lblInvoices.Text = $"{k.InvoiceCount} فاتورة";
        _lblAvg.Text = $"{k.AvgInvoice:N0} ج.م";
        _lblProfit.Text = $"{k.TotalProfit:N0} ج.م";
        _lblNewCust.Text = $"{k.NewCustomers} عميل";
        SetTrend(_lblSalesTrend, k.SalesTrendPct);
        SetTrend(_lblInvTrend, k.InvoiceTrendPct);
        SetTrend(_lblAvgTrend, k.AvgTrendPct);
        SetTrend(_lblProfitTrend, k.ProfitTrendPct);
        SetTrend(_lblCustTrend, k.CustomerTrendPct);
    }

    private void BindTables()
    {
        _servicesGrid.Rows.Clear();
        var i = 1;
        foreach (var r in _snap.TopServices.Take(5))
        {
            _servicesGrid.Rows.Add(i++, r.Name, $"{r.Quantity:0}", $"{r.Total:N0} ج.م");
        }

        _productsGrid.Rows.Clear();
        i = 1;
        foreach (var r in _snap.TopProducts.Take(5))
        {
            _productsGrid.Rows.Add(i++, r.Name, $"{r.Quantity:0}", $"{r.Total:N0} ج.م");
        }
    }

    private void BindQuickReports()
    {
        _quickHost.Controls.Clear();
        var totalPages = Math.Max(1, (int)Math.Ceiling(QuickReports.Length / (double)QuickPageSize));
        if (_quickPage >= totalPages)
        {
            _quickPage = totalPages - 1;
        }

        foreach (var item in QuickReports.Skip(_quickPage * QuickPageSize).Take(QuickPageSize))
        {
            _quickHost.Controls.Add(CreateQuickCard(item.title, item.desc, item.glyph, item.focus));
        }

        _lblPage.Text = $"صفحة {_quickPage + 1} من {totalPages} ({QuickReports.Length} تقرير)";
    }

    private void BuildPager()
    {
        _pager.Controls.Clear();
        var totalPages = Math.Max(1, (int)Math.Ceiling(QuickReports.Length / (double)QuickPageSize));
        _pager.Controls.Add(PaginationStyle.CreateNavButton("السابق", (_, _) =>
        {
            if (_quickPage > 0)
            {
                _quickPage--;
                BindQuickReports();
                BuildPager();
            }
        }));
        for (var p = 0; p < totalPages; p++)
        {
            var page = p;
            var btn = PaginationStyle.CreatePageButton((page + 1).ToString(), active: page == _quickPage);
            btn.Click += (_, _) =>
            {
                _quickPage = page;
                BindQuickReports();
                BuildPager();
            };
            _pager.Controls.Add(btn);
        }

        _pager.Controls.Add(PaginationStyle.CreateNavButton("التالي", (_, _) =>
        {
            if (_quickPage < totalPages - 1)
            {
                _quickPage++;
                BindQuickReports();
                BuildPager();
            }
        }));
    }

    private void BuildUi()
    {
        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = InvoiceTheme.Background,
            Padding = new Padding(14, 8, 14, 10)
        };

        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = InvoiceTheme.Background,
            Width = 1200
        };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 260));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 280));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        main.Controls.Add(BuildTopBar(), 0, 0);
        main.Controls.Add(BuildFilters(), 0, 1);
        main.Controls.Add(BuildKpis(), 0, 2);
        main.Controls.Add(BuildCharts(), 0, 3);
        main.Controls.Add(BuildBottom(), 0, 4);
        main.Controls.Add(BuildFooter(), 0, 5);

        scroll.Controls.Add(main);
        scroll.Resize += (_, _) => main.Width = Math.Max(1100, scroll.ClientSize.Width - 30);
        Controls.Add(scroll);
    }

    private Control BuildTopBar()
    {
        var bar = new Panel { Dock = DockStyle.Fill, BackColor = InvoiceTheme.Background, Height = 48 };
        var titleHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var titleInner = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.None
        };
        titleInner.Controls.Add(new PictureBox
        {
            Size = new Size(30, 30),
            SizeMode = PictureBoxSizeMode.CenterImage,
            Image = GlyphHelper.Create("\uE9F9", InvoiceTheme.Gold, 22),
            Margin = new Padding(0, 4, 8, 0),
            BackColor = Color.Transparent
        });
        titleInner.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "التقارير",
            Font = new Font(InvoiceTheme.Family.FontFamily, 20F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = InvoiceTheme.White,
            Margin = new Padding(0, 6, 0, 0)
        });
        titleHost.Controls.Add(titleInner);
        titleHost.Resize += (_, _) =>
        {
            titleInner.Left = Math.Max(0, (titleHost.ClientSize.Width - titleInner.Width) / 2);
            titleInner.Top = Math.Max(0, (titleHost.ClientSize.Height - titleInner.Height) / 2);
        };

        var export = CreateOutlinedButton("تصدير جميع التقارير", (_, _) => Export("الكل"));
        export.Image = GlyphHelper.Create("\uE896", InvoiceTheme.Gold, 14);
        export.ImageAlign = HorizontalAlignment.Left;
        export.TextAlign = HorizontalAlignment.Right;
        export.Dock = DockStyle.Left;
        export.Width = Math.Max(180, TextRenderer.MeasureText(export.Text, export.Font).Width + 48);

        var right = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            Padding = new Padding(0, 10, 0, 0)
        };
        _lblDate = new Label
        {
            AutoSize = true,
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont,
            Margin = new Padding(8, 8, 12, 0)
        };
        right.Controls.Add(_lblDate);

        bar.Controls.Add(titleHost);
        bar.Controls.Add(export);
        bar.Controls.Add(right);
        return bar;
    }

    private Control BuildFilters()
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0)
        };

        _cmbType = CreateCombo(130);
        _cmbCategory = CreateCombo(120);
        _cmbTech = CreateCombo(120);
        _txtFrom = CreateDateBox();
        _txtTo = CreateDateBox();

        var apply = CreateGoldButton("عرض التقرير", (_, _) => ApplyReport());
        apply.Image = GlyphHelper.Create("\uE721", Color.Black, 14);
        apply.ImageAlign = HorizontalAlignment.Left;
        apply.TextAlign = HorizontalAlignment.Right;

        var filterIcon = new Guna2Button
        {
            Size = new Size(36, 36),
            BorderRadius = 8,
            FillColor = InvoiceTheme.Gold,
            Image = GlyphHelper.Create("\uE71C", Color.Black, 16),
            ImageSize = new Size(18, 18),
            Margin = new Padding(0, 0, 8, 0),
            Cursor = Cursors.Hand
        };
        filterIcon.Click += (_, _) => ApplyReport();

        bar.Controls.Add(Labeled("نوع التقرير", _cmbType));
        bar.Controls.Add(Labeled("التصنيف", _cmbCategory));
        bar.Controls.Add(Labeled("الفني", _cmbTech));
        bar.Controls.Add(Labeled("من تاريخ", _txtFrom));
        bar.Controls.Add(Labeled("إلى تاريخ", _txtTo));
        bar.Controls.Add(filterIcon);
        bar.Controls.Add(apply);
        return bar;
    }

    private Control BuildKpis()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        for (var i = 0; i < 5; i++)
        {
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        }

        _lblSales = new Label();
        _lblInvoices = new Label();
        _lblAvg = new Label();
        _lblProfit = new Label();
        _lblNewCust = new Label();
        _lblSalesTrend = new Label();
        _lblInvTrend = new Label();
        _lblAvgTrend = new Label();
        _lblProfitTrend = new Label();
        _lblCustTrend = new Label();

        row.Controls.Add(CreateKpi("إجمالي المبيعات", _lblSales, _lblSalesTrend, "\uE9D2"), 0, 0);
        row.Controls.Add(CreateKpi("إجمالي الفواتير", _lblInvoices, _lblInvTrend, "\uE8A5"), 1, 0);
        row.Controls.Add(CreateKpi("متوسط قيمة الفاتورة", _lblAvg, _lblAvgTrend, "\uE8C7"), 2, 0);
        row.Controls.Add(CreateKpi("إجمالي الأرباح", _lblProfit, _lblProfitTrend, "\uE8C8"), 3, 0);
        row.Controls.Add(CreateKpi("عدد العملاء الجدد", _lblNewCust, _lblCustTrend, "\uE8FA"), 4, 0);
        return row;
    }

    private Control BuildCharts()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 6)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        row.Controls.Add(BuildLineChartCard(), 0, 0);
        row.Controls.Add(BuildDonutCard(), 1, 0);
        return row;
    }

    private Control BuildLineChartCard()
    {
        var card = CreateCard();
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Color.Transparent };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.Controls.Add(SectionTitle("المبيعات اليومية"), 0, 0);
        _lineChart = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        _lineChart.Paint += PaintLineChart;
        root.Controls.Add(_lineChart, 0, 1);
        card.Controls.Add(root);
        return card;
    }

    private Control BuildDonutCard()
    {
        var card = CreateCard();
        card.Margin = new Padding(8, 4, 4, 4);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Color.Transparent };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        var title = SectionTitle("المبيعات حسب التصنيف");
        root.SetColumnSpan(title, 2);
        root.Controls.Add(title, 0, 0);

        var donutHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        _donutChart = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        _donutChart.Paint += PaintDonut;
        _donutCenter = new Label
        {
            AutoSize = false,
            Size = new Size(110, 48),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = InvoiceTheme.Gold,
            Font = InvoiceTheme.SmallFont,
            BackColor = Color.Transparent
        };
        donutHost.Controls.Add(_donutCenter);
        donutHost.Controls.Add(_donutChart);
        donutHost.Resize += (_, _) =>
        {
            _donutCenter.Left = (donutHost.Width - _donutCenter.Width) / 2;
            _donutCenter.Top = (donutHost.Height - _donutCenter.Height) / 2;
        };

        _legend = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(4)
        };
        root.Controls.Add(donutHost, 0, 1);
        root.Controls.Add(_legend, 1, 1);
        card.Controls.Add(root);
        return card;
    }

    private Control BuildBottom()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
        row.Controls.Add(BuildQuickCard(), 0, 0);
        row.Controls.Add(BuildRankCard("أكثر الخدمات مبيعاً", out _servicesGrid, "عرض كل الخدمات", () => Export("الخدمات")), 1, 0);
        row.Controls.Add(BuildRankCard("أكثر الأصناف مبيعاً", out _productsGrid, "عرض كل الأصناف", () => Export("الأصناف")), 2, 0);
        return row;
    }

    private Control BuildQuickCard()
    {
        var card = CreateCard();
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Color.Transparent };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.Controls.Add(SectionTitle("التقارير السريعة"), 0, 0);
        _quickHost = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true,
            BackColor = Color.Transparent
        };
        root.Controls.Add(_quickHost, 0, 1);
        card.Controls.Add(root);
        return card;
    }

    private Control BuildRankCard(string title, out DataGridView grid, string moreText, Action onMore)
    {
        var card = CreateCard();
        card.Margin = new Padding(8, 4, 4, 4);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Color.Transparent };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.Controls.Add(SectionTitle(title), 0, 0);
        grid = CreateMiniGrid();
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "#", FillWeight = 10 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = title.Contains("خدمات") ? "الخدمة" : "الصنف", FillWeight = 40 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = title.Contains("خدمات") ? "عدد المرات" : "الكمية", FillWeight = 20 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "إجمالي المبيعات", FillWeight = 30 });
        root.Controls.Add(grid, 0, 1);
        var more = CreateOutlinedButton(moreText, (_, _) => onMore());
        more.Dock = DockStyle.Right;
        more.Height = 30;
        root.Controls.Add(more, 0, 2);
        card.Controls.Add(root);
        return card;
    }

    private Control BuildFooter()
    {
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        _pager = PaginationStyle.CreateBar();
        _pager.Anchor = AnchorStyles.None;
        var pagerWrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        pagerWrap.Controls.Add(_pager);
        _lblPage = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont
        };
        host.Controls.Add(pagerWrap, 0, 0);
        host.Controls.Add(_lblPage, 0, 1);
        return host;
    }

    private void PaintLineChart(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var points = _snap.DailySales;
        var rect = _lineChart.ClientRectangle;
        rect.Inflate(-24, -18);
        if (rect.Width < 20 || rect.Height < 20)
        {
            return;
        }

        using var axisPen = new Pen(InvoiceTheme.CardBorder, 1f);
        g.DrawLine(axisPen, rect.Left, rect.Bottom, rect.Right, rect.Bottom);
        g.DrawLine(axisPen, rect.Left, rect.Top, rect.Left, rect.Bottom);

        if (points.Count == 0)
        {
            TextRenderer.DrawText(g, "لا توجد بيانات في الفترة المحددة", InvoiceTheme.BodyFont, rect, InvoiceTheme.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        var max = Math.Max(1m, points.Max(p => p.Amount));
        var stepX = points.Count <= 1 ? 0 : (float)rect.Width / (points.Count - 1);
        var pts = new List<PointF>();
        for (var i = 0; i < points.Count; i++)
        {
            var x = rect.Left + i * stepX;
            var y = rect.Bottom - (float)((double)points[i].Amount / (double)max * rect.Height);
            pts.Add(new PointF(x, y));
        }

        if (pts.Count >= 2)
        {
            var fillPts = new List<PointF>(pts)
            {
                new(pts[^1].X, rect.Bottom),
                new(pts[0].X, rect.Bottom)
            };
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect, Color.FromArgb(90, InvoiceTheme.Gold), Color.FromArgb(10, InvoiceTheme.Gold), 90f);
            g.FillPolygon(brush, fillPts.ToArray());
            using var pen = new Pen(InvoiceTheme.Gold, 2.2f);
            g.DrawLines(pen, pts.ToArray());
        }

        using var font = InvoiceTheme.SmallFont;
        // Y labels
        for (var i = 0; i <= 3; i++)
        {
            var val = max * (3 - i) / 3m;
            var y = rect.Top + rect.Height * i / 3f;
            TextRenderer.DrawText(g, $"{val:0}", font, new Point(4, (int)y - 6), InvoiceTheme.Muted);
        }

        // sparse X labels
        var labelEvery = Math.Max(1, points.Count / 6);
        for (var i = 0; i < points.Count; i += labelEvery)
        {
            var x = (int)pts[i].X;
            TextRenderer.DrawText(g, points[i].Day.ToString("dd/MM"), font, new Point(x - 14, rect.Bottom + 2), InvoiceTheme.Muted);
        }
    }

    private void PaintDonut(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var slices = _snap.CategorySales;
        var bounds = _donutChart.ClientRectangle;
        var size = Math.Min(bounds.Width, bounds.Height) - 20;
        if (size < 40)
        {
            return;
        }

        var rect = new Rectangle((bounds.Width - size) / 2, (bounds.Height - size) / 2, size, size);
        if (slices.Count == 0)
        {
            using var empty = new Pen(InvoiceTheme.CardBorder, 14f);
            g.DrawEllipse(empty, rect);
            return;
        }

        float start = -90f;
        foreach (var s in slices)
        {
            var sweep = (float)(s.Percent / 100m * 360m);
            using var brush = new SolidBrush(s.Color);
            g.FillPie(brush, rect, start, Math.Max(0.5f, sweep));
            start += sweep;
        }

        var hole = Rectangle.Inflate(rect, -size / 4, -size / 4);
        using var holeBrush = new SolidBrush(InvoiceTheme.Card);
        g.FillEllipse(holeBrush, hole);
    }

    private void Export(string focus)
    {
        ReadFilterFromUi();
        var snap = ReportsService.Build(_filter);
        var lines = ReportsService.ExportLines(snap, _filter, focus).ToList();
        var dir = Path.Combine(AppContext.BaseDirectory, "Reports");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"report-{focus}-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        File.WriteAllLines(path, lines);
        AppMessageDialog.Success(this, $"تم تصدير التقرير من بيانات النظام.\r\n{Path.GetFileName(path)}", "التقارير");
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch
        {
            // ignore
        }
    }

    private Control CreateQuickCard(string title, string desc, string glyph, string focus)
    {
        var btn = new Guna2Button
        {
            Size = new Size(148, 78),
            Margin = new Padding(4),
            BorderRadius = 10,
            FillColor = InvoiceTheme.InputFill,
            BorderColor = Color.FromArgb(80, InvoiceTheme.Gold),
            BorderThickness = 1,
            Cursor = Cursors.Hand,
            Font = InvoiceTheme.SmallFont,
            ForeColor = InvoiceTheme.White,
            Text = title + "\n" + desc,
            TextAlign = HorizontalAlignment.Right,
            Image = GlyphHelper.Create(glyph, InvoiceTheme.Gold, 16),
            ImageAlign = HorizontalAlignment.Left,
            ImageSize = new Size(20, 20),
            HoverState =
            {
                FillColor = Color.FromArgb(30, InvoiceTheme.Gold),
                BorderColor = InvoiceTheme.Gold,
                ForeColor = InvoiceTheme.White
            }
        };
        btn.Click += (_, _) => Export(focus);
        return btn;
    }

    private static Control CreateLegendItem(CategorySalesSlice slice)
    {
        var row = new Panel { Width = 160, Height = 36, Margin = new Padding(0, 2, 0, 2), BackColor = Color.Transparent };
        var swatch = new Panel
        {
            Width = 12,
            Height = 12,
            Left = 2,
            Top = 10,
            BackColor = slice.Color
        };
        var lbl = new Label
        {
            AutoSize = false,
            Width = 140,
            Height = 34,
            Left = 18,
            Top = 0,
            Text = $"{slice.Name}\n{slice.Percent:0.#}% • {slice.Amount:N0} ج.م",
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont
        };
        row.Controls.Add(swatch);
        row.Controls.Add(lbl);
        return row;
    }

    private static Control CreateKpi(string title, Label value, Label trend, string glyph)
    {
        var card = CreateCard();
        card.Padding = new Padding(10, 8, 10, 8);
        var icon = new PictureBox
        {
            Dock = DockStyle.Right,
            Width = 34,
            SizeMode = PictureBoxSizeMode.CenterImage,
            Image = GlyphHelper.Create(glyph, InvoiceTheme.Gold, 18),
            BackColor = Color.Transparent
        };
        var texts = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var titleLbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 18,
            Text = title,
            ForeColor = InvoiceTheme.Gold,
            Font = InvoiceTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleLeft
        };
        value.Dock = DockStyle.Fill;
        value.ForeColor = InvoiceTheme.White;
        value.Font = InvoiceTheme.SectionFont;
        value.TextAlign = ContentAlignment.MiddleLeft;
        trend.Dock = DockStyle.Bottom;
        trend.Height = 18;
        trend.Font = InvoiceTheme.SmallFont;
        trend.TextAlign = ContentAlignment.MiddleLeft;
        texts.Controls.Add(value);
        texts.Controls.Add(trend);
        texts.Controls.Add(titleLbl);
        card.Controls.Add(texts);
        card.Controls.Add(icon);
        return card;
    }

    private static void SetTrend(Label lbl, decimal pct)
    {
        var up = pct >= 0;
        lbl.Text = $"{(up ? "▲" : "▼")} {Math.Abs(pct):0.0}%";
        lbl.ForeColor = up ? ProfitGreen : InvoiceTheme.Danger;
    }

    private static Guna2Panel CreateCard() =>
        new()
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Margin = new Padding(4),
            Padding = new Padding(10, 8, 10, 8),
            ShadowDecoration = { Enabled = true, Depth = 8, Color = Color.Black, BorderRadius = InvoiceTheme.Radius }
        };

    private static Label SectionTitle(string text) =>
        new()
        {
            Dock = DockStyle.Fill,
            Text = text,
            Font = InvoiceTheme.SectionFont,
            ForeColor = InvoiceTheme.Gold,
            TextAlign = ContentAlignment.MiddleRight
        };

    private static DataGridView CreateMiniGrid()
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
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 28 },
            Font = InvoiceTheme.SmallFont,
            ForeColor = InvoiceTheme.White,
            ColumnHeadersHeight = 28,
            RightToLeft = RightToLeft.Yes
        };
        g.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = InvoiceTheme.Card,
            ForeColor = InvoiceTheme.White,
            SelectionBackColor = Color.FromArgb(48, InvoiceTheme.Gold),
            SelectionForeColor = InvoiceTheme.White,
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };
        g.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(40, InvoiceTheme.Gold),
            ForeColor = InvoiceTheme.Gold,
            Font = InvoiceTheme.TableHeaderFont,
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };
        return g;
    }

    private static Control Labeled(string label, Control control)
    {
        var host = new Panel { Width = control.Width + 8, Height = 40, Margin = new Padding(0, 0, 8, 0), BackColor = Color.Transparent };
        var lbl = new Label
        {
            AutoSize = true,
            Text = label,
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont,
            Location = new Point(0, 0)
        };
        control.Location = new Point(0, 4);
        control.Width = host.Width - 4;
        // Prefer single-line filter look: hide tiny label overlay by putting placeholder on control only
        lbl.Visible = false;
        host.Controls.Add(control);
        host.Controls.Add(lbl);
        return host;
    }

    private static Guna2ComboBox CreateCombo(int width) =>
        new()
        {
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(90, InvoiceTheme.Gold),
            FillColor = InvoiceTheme.InputFill,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.SmallFont,
            Size = new Size(width, 36),
            FocusedColor = InvoiceTheme.Gold,
            ItemsAppearance = { BackColor = InvoiceTheme.Card, ForeColor = InvoiceTheme.White }
        };

    private static Guna2TextBox CreateDateBox() =>
        new()
        {
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(90, InvoiceTheme.Gold),
            FillColor = InvoiceTheme.InputFill,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.SmallFont,
            Size = new Size(110, 36),
            IconLeft = GlyphHelper.Create("\uE787", InvoiceTheme.Gold, 14),
            IconLeftSize = new Size(16, 16),
            FocusedState = { BorderColor = InvoiceTheme.Gold }
        };

    private static Guna2Button CreateOutlinedButton(string text, EventHandler onClick)
    {
        var btn = new Guna2Button
        {
            Text = text,
            Font = InvoiceTheme.SmallFont,
            Height = 36,
            BorderRadius = 8,
            Margin = new Padding(0, 0, 8, 0),
            Cursor = Cursors.Hand,
            ForeColor = InvoiceTheme.White,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.Gold,
            BorderThickness = 1,
            HoverState =
            {
                FillColor = Color.FromArgb(30, InvoiceTheme.Gold),
                ForeColor = InvoiceTheme.White
            }
        };
        btn.Width = Math.Max(110, TextRenderer.MeasureText(text, btn.Font).Width + 36);
        btn.Click += onClick;
        return btn;
    }

    private static Guna2Button CreateGoldButton(string text, EventHandler onClick)
    {
        var btn = new Guna2Button
        {
            Text = text,
            Font = InvoiceTheme.SmallFont,
            Height = 36,
            BorderRadius = 8,
            Margin = new Padding(0, 0, 8, 0),
            Cursor = Cursors.Hand,
            ForeColor = Color.Black,
            FillColor = InvoiceTheme.Gold,
            BorderThickness = 0,
            HoverState = { FillColor = InvoiceTheme.GoldDark, ForeColor = Color.Black }
        };
        btn.Width = Math.Max(120, TextRenderer.MeasureText(text, btn.Font).Width + 40);
        btn.Click += onClick;
        return btn;
    }

    private static void SelectOrDefault(Guna2ComboBox combo, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var idx = combo.Items.IndexOf(value);
            if (idx >= 0)
            {
                combo.SelectedIndex = idx;
                return;
            }
        }

        if (combo.Items.Count > 0)
        {
            combo.SelectedIndex = 0;
        }
    }

    private void UpdateClock() => _lblDate.Text = DateTime.Now.ToString("dd MMM yyyy");

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _clockTimer.Stop();
        _clockTimer.Dispose();
        base.OnFormClosed(e);
    }
}
