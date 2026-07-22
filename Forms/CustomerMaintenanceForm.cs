using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

internal sealed class MaintenanceCustomerView
{
    public CustomerRecord Customer { get; init; } = null!;
    public List<InvoiceRecord> Invoices { get; init; } = [];
    public int VisitCount => Invoices.Count > 0 ? Invoices.Count : Math.Max(Customer.Services.Count, Customer.Invoices.Count);
    public DateTime? LastMaintenance =>
        Invoices.Count > 0 ? Invoices.Max(i => i.CreatedAt) : Customer.LastVisit;
    public decimal TotalExpenses =>
        Invoices.Count > 0 ? Invoices.Sum(i => i.GrandTotal) : Customer.TotalInvoices;
    public string Plate => !string.IsNullOrWhiteSpace(Customer.PrimaryPlate) && Customer.PrimaryPlate != "—"
        ? Customer.PrimaryPlate
        : Invoices.Select(i => $"{i.PlateLetters} {i.PlateNumber}".Trim()).FirstOrDefault(p => !string.IsNullOrWhiteSpace(p)) ?? "—";
    public string CarName => Customer.PrimaryCarName != "—"
        ? Customer.PrimaryCarName
        : Invoices.Select(i => i.CarModel).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)) ?? "—";
    public string LastService
    {
        get
        {
            var fromInv = Invoices.OrderByDescending(i => i.CreatedAt).SelectMany(i => i.Items.Select(x => x.Name))
                .Where(n => !string.IsNullOrWhiteSpace(n)).Take(3).ToList();
            if (fromInv.Count > 0)
            {
                return string.Join("، ", fromInv);
            }

            var fromSvc = Customer.Services.OrderByDescending(s => s.Date).Select(s => s.Service)
                .Where(n => !string.IsNullOrWhiteSpace(n)).Take(3).ToList();
            return fromSvc.Count > 0 ? string.Join("، ", fromSvc) : "—";
        }
    }
    public string CurrentKm
    {
        get
        {
            var fromCar = Customer.PrimaryCar?.Mileage ?? 0;
            if (fromCar > 0)
            {
                return $"{fromCar:N0} كم";
            }

            var odo = Invoices.OrderByDescending(i => i.CreatedAt)
                .Select(i => i.Odometer).FirstOrDefault(o => !string.IsNullOrWhiteSpace(o));
            return string.IsNullOrWhiteSpace(odo) ? "—" : (odo.Contains("كم") ? odo : $"{odo} كم");
        }
    }
}

internal sealed class CustomerMaintenanceForm : Form
{
    private readonly System.Windows.Forms.Timer _clockTimer = new() { Interval = 1000 };
    private readonly bool _embedded;

    private Label _lblDate = null!;
    private Label _lblDetailName = null!;
    private Label _lblPhone = null!;
    private Label _lblPlate = null!;
    private Label _lblCar = null!;
    private Label _lblLastDate = null!;
    private Label _lblTotalExp = null!;
    private Label _lblVisits = null!;
    private Label _lblLastService = null!;
    private Label _lblKm = null!;
    private Label _lblStatAvg = null!;
    private Label _lblStatTotal = null!;
    private Label _lblStatTimes = null!;
    private Label _lblStatCustomers = null!;

    private Guna2TextBox _txtSearch = null!;
    private Guna2ComboBox _cmbStatus = null!;
    private Guna2ComboBox _cmbType = null!;
    private Guna2ComboBox _cmbCar = null!;
    private Guna2TextBox _txtFrom = null!;
    private Guna2TextBox _txtTo = null!;

    private DataGridView _grid = null!;
    private DataGridView _historyGrid = null!;
    private FlowLayoutPanel _pager = null!;
    private FlowLayoutPanel _historyPager = null!;

    private List<MaintenanceCustomerView> _all = [];
    private List<MaintenanceCustomerView> _filtered = [];
    private MaintenanceCustomerView? _selected;
    private int _page;
    private int _historyPage;
    private const int PageSize = 8;
    private const int HistoryPageSize = 5;
    private bool _showAllHistory;

    public CustomerMaintenanceForm(bool embedded = false)
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
        Name = "CustomerMaintenanceForm";
        RightToLeft = RightToLeft.No;
        ShowIcon = !_embedded;
        ShowInTaskbar = !_embedded;
        StartPosition = FormStartPosition.Manual;
        Text = _embedded ? string.Empty : "صيانة العملاء - Ibrahim Abdo Auto Service";

        CustomerStore.Load();
        InvoiceStore.Load();
        BuildUi();
        RefreshData();

        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();
        ResumeLayout(true);
    }

    public void RefreshData()
    {
        CustomerStore.Load();
        InvoiceStore.Load();
        _all = BuildViews();
        PopulateCarFilter();
        ApplyFilters(resetPage: false);
    }

    private void PopulateCarFilter()
    {
        if (_cmbCar is null)
        {
            return;
        }

        var selected = _cmbCar.SelectedItem?.ToString();
        var cars = _all.Select(v => v.CarName).Where(c => c != "—").Distinct().OrderBy(c => c).ToList();
        _cmbCar.SelectedIndexChanged -= OnFilterChanged;
        _cmbCar.Items.Clear();
        _cmbCar.Items.Add("الكل");
        foreach (var c in cars)
        {
            _cmbCar.Items.Add(c);
        }

        var idx = selected is null ? 0 : _cmbCar.Items.IndexOf(selected);
        _cmbCar.SelectedIndex = idx >= 0 ? idx : 0;
        _cmbCar.SelectedIndexChanged += OnFilterChanged;
    }

    private void OnFilterChanged(object? sender, EventArgs e) => ApplyFilters();

    private static List<MaintenanceCustomerView> BuildViews()
    {
        var invoices = InvoiceStore.All.ToList();
        var list = new List<MaintenanceCustomerView>();
        foreach (var c in CustomerStore.All)
        {
            var matched = invoices.Where(i =>
                (!string.IsNullOrWhiteSpace(c.Phone) &&
                 string.Equals(i.Phone?.Trim(), c.Phone.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                string.Equals(i.CustomerName?.Trim(), c.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i => i.CreatedAt)
                .ToList();

            if (matched.Count == 0 && c.Services.Count == 0 && c.Invoices.Count == 0)
            {
                continue;
            }

            list.Add(new MaintenanceCustomerView { Customer = c, Invoices = matched });
        }

        // Also include invoice-only customers not in store
        foreach (var group in invoices.GroupBy(i => (i.Phone?.Trim() ?? "") + "|" + (i.CustomerName?.Trim() ?? "")))
        {
            var sample = group.First();
            var already = list.Any(v =>
                (!string.IsNullOrWhiteSpace(sample.Phone) &&
                 string.Equals(v.Customer.Phone, sample.Phone, StringComparison.OrdinalIgnoreCase)) ||
                string.Equals(v.Customer.Name, sample.CustomerName, StringComparison.OrdinalIgnoreCase));
            if (already)
            {
                continue;
            }

            var synthetic = new CustomerRecord
            {
                Name = sample.CustomerName,
                Phone = sample.Phone,
                Cars =
                [
                    new CarRecord
                    {
                        PlateNumber = $"{sample.PlateLetters} {sample.PlateNumber}".Trim(),
                        Brand = sample.CarModel,
                        Mileage = int.TryParse(new string(sample.Odometer.Where(char.IsDigit).ToArray()), out var km) ? km : 0
                    }
                ]
            };
            list.Add(new MaintenanceCustomerView
            {
                Customer = synthetic,
                Invoices = group.OrderByDescending(i => i.CreatedAt).ToList()
            });
        }

        return list.OrderByDescending(v => v.LastMaintenance ?? DateTime.MinValue).ToList();
    }

    private void ApplyFilters(bool resetPage = true)
    {
        if (resetPage)
        {
            _page = 0;
        }

        IEnumerable<MaintenanceCustomerView> q = _all;
        var search = _txtSearch?.Text?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(search))
        {
            q = q.Where(v =>
                v.Customer.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                v.Customer.Phone.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                v.Plate.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                v.CarName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (_cmbStatus?.SelectedIndex > 0)
        {
            var vip = string.Equals(_cmbStatus.SelectedItem?.ToString(), "VIP", StringComparison.OrdinalIgnoreCase);
            q = q.Where(v => v.Customer.IsVip == vip);
        }

        if (_cmbType?.SelectedIndex > 0)
        {
            var t = _cmbType.SelectedItem?.ToString() ?? "";
            if (t == "متكرر")
            {
                q = q.Where(v => v.VisitCount >= 3);
            }
            else if (t == "جديد")
            {
                q = q.Where(v => v.VisitCount <= 2);
            }
        }

        if (_cmbCar?.SelectedIndex > 0)
        {
            var car = _cmbCar.SelectedItem?.ToString() ?? "";
            q = q.Where(v => v.CarName.Contains(car, StringComparison.OrdinalIgnoreCase));
        }

        if (TryParseDate(_txtFrom?.Text, out var from))
        {
            q = q.Where(v => v.LastMaintenance >= from);
        }

        if (TryParseDate(_txtTo?.Text, out var to))
        {
            q = q.Where(v => v.LastMaintenance <= to.Date.AddDays(1).AddTicks(-1));
        }

        _filtered = q.ToList();
        UpdateStats();
        BindGrid();
    }

    private void UpdateStats()
    {
        var totalCustomers = _filtered.Count;
        var totalVisits = _filtered.Sum(v => v.VisitCount);
        var totalExp = _filtered.Sum(v => v.TotalExpenses);
        var avg = totalVisits > 0 ? totalExp / totalVisits : 0m;
        _lblStatCustomers.Text = $"{totalCustomers} عميل";
        _lblStatTimes.Text = $"{totalVisits} مرة";
        _lblStatTotal.Text = FormatMoney(totalExp);
        _lblStatAvg.Text = FormatMoney(avg);
    }

    private void BuildUi()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = InvoiceTheme.Background,
            Padding = new Padding(14, 8, 14, 10)
        };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        main.Controls.Add(BuildTopBar(), 0, 0);
        main.Controls.Add(BuildFilters(), 0, 1);
        main.Controls.Add(BuildSplit(), 0, 2);
        Controls.Add(main);
    }

    private Control BuildTopBar()
    {
        var bar = new Panel { Dock = DockStyle.Fill, BackColor = InvoiceTheme.Background };

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
            Image = GlyphHelper.Create("\uE90F", InvoiceTheme.Gold, 22),
            Margin = new Padding(0, 4, 8, 0),
            BackColor = Color.Transparent
        });
        titleInner.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "صيانة العملاء",
            Font = new Font(InvoiceTheme.Family.FontFamily, 20F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = InvoiceTheme.Gold,
            Margin = new Padding(0, 6, 0, 0)
        });
        titleHost.Controls.Add(titleInner);
        titleHost.Resize += (_, _) =>
        {
            titleInner.Left = Math.Max(0, (titleHost.ClientSize.Width - titleInner.Width) / 2);
            titleInner.Top = Math.Max(0, (titleHost.ClientSize.Height - titleInner.Height) / 2);
        };

        var export = CreateToolbarButton("تصدير تقرير الصيانة", false, (_, _) => ExportReport());
        export.Image = GlyphHelper.Create("\uE896", InvoiceTheme.Gold, 14);
        export.ImageAlign = HorizontalAlignment.Left;
        export.TextAlign = HorizontalAlignment.Right;
        export.Dock = DockStyle.Left;
        export.Width = Math.Max(170, TextRenderer.MeasureText(export.Text, export.Font).Width + 48);

        var right = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
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
            Padding = new Padding(0, 6, 0, 0)
        };

        _txtSearch = new Guna2TextBox
        {
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(90, InvoiceTheme.Gold),
            FillColor = InvoiceTheme.InputFill,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.BodyFont,
            PlaceholderText = "بحث باسم العميل / رقم الهاتف / رقم السيارة",
            PlaceholderForeColor = InvoiceTheme.Muted,
            Size = new Size(280, 36),
            Margin = new Padding(0, 0, 8, 0),
            IconLeft = GlyphHelper.Create("\uE721", InvoiceTheme.Gold, 14),
            IconLeftSize = new Size(16, 16),
            FocusedState = { BorderColor = InvoiceTheme.Gold }
        };
        _txtSearch.TextChanged += (_, _) => ApplyFilters();

        _cmbStatus = CreateFilterCombo("حالة العميل", ["الكل", "عادي", "VIP"]);
        _cmbType = CreateFilterCombo("النوع", ["الكل", "جديد", "متكرر"]);
        _cmbCar = CreateFilterCombo("السيارة", ["الكل"]);
        _cmbStatus.SelectedIndexChanged += OnFilterChanged;
        _cmbType.SelectedIndexChanged += OnFilterChanged;
        _cmbCar.SelectedIndexChanged += OnFilterChanged;

        _txtFrom = CreateDateBox("من تاريخ");
        _txtTo = CreateDateBox("إلى تاريخ");

        var filterBtn = CreateToolbarButton("تصفية", false, (_, _) => ApplyFilters());
        filterBtn.Image = GlyphHelper.Create("\uE71C", InvoiceTheme.Gold, 14);
        filterBtn.ImageAlign = HorizontalAlignment.Left;
        filterBtn.TextAlign = HorizontalAlignment.Right;
        filterBtn.Height = 36;

        bar.Controls.Add(_txtSearch);
        bar.Controls.Add(_cmbStatus);
        bar.Controls.Add(_cmbType);
        bar.Controls.Add(_cmbCar);
        bar.Controls.Add(_txtFrom);
        bar.Controls.Add(_txtTo);
        bar.Controls.Add(filterBtn);
        return bar;
    }

    private Control BuildSplit()
    {
        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
        split.Controls.Add(BuildLeftColumn(), 0, 0);
        split.Controls.Add(BuildRightColumn(), 1, 0);
        return split;
    }

    private Control BuildLeftColumn()
    {
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 0, 8, 0),
            BackColor = Color.Transparent
        };
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 62F));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
        host.Controls.Add(BuildCustomersCard(), 0, 0);
        host.Controls.Add(BuildStatsCard(), 0, 1);
        return host;
    }

    private Control BuildRightColumn()
    {
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));
        host.Controls.Add(BuildDetailsCard(), 0, 0);
        host.Controls.Add(BuildHistoryCard(), 0, 1);
        return host;
    }

    private Control BuildCustomersCard()
    {
        var card = CreateCard(new Padding(10, 8, 10, 8));
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        root.Controls.Add(SectionTitle("قائمة العملاء الذين لديهم صيانة"), 0, 0);

        _grid = CreateGrid();
        _grid.RightToLeft = RightToLeft.Yes;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIndex", HeaderText = "#", FillWeight = 5 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "اسم العميل", FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPhone", HeaderText = "الهاتف", FillWeight = 11 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCar", HeaderText = "السيارة", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPlate", HeaderText = "رقم اللوحة", FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colVisits", HeaderText = "عدد الصيانات", FillWeight = 9 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLast", HeaderText = "آخر صيانة", FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal", HeaderText = "إجمالي المصروفات", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "colAction",
            HeaderText = "إجراءات",
            Text = "عرض التفاصيل",
            UseColumnTextForButtonValue = true,
            FlatStyle = FlatStyle.Flat,
            FillWeight = 14
        });
        _grid.CellPainting += PaintActionButton;
        _grid.CellClick += OnCustomerCellClick;
        _grid.SelectionChanged += (_, _) => OnCustomerSelected();
        EnableDoubleBuffering(_grid);
        root.Controls.Add(_grid, 0, 1);

        _pager = PaginationStyle.CreateBar();
        root.Controls.Add(_pager, 0, 2);
        card.Controls.Add(root);
        return card;
    }

    private Control BuildStatsCard()
    {
        var card = CreateCard(new Padding(10, 8, 10, 8));
        card.Margin = new Padding(0, 8, 0, 0);
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.Controls.Add(SectionTitle("إحصائيات الصيانة"), 0, 0);

        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        for (var i = 0; i < 4; i++)
        {
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        }

        _lblStatAvg = new Label();
        _lblStatTotal = new Label();
        _lblStatTimes = new Label();
        _lblStatCustomers = new Label();
        row.Controls.Add(CreateMiniStat("متوسط تكلفة الصيانة", _lblStatAvg, "\uE8C7"), 0, 0);
        row.Controls.Add(CreateMiniStat("إجمالي مصروفات الصيانة", _lblStatTotal, "\uE8D4"), 1, 0);
        row.Controls.Add(CreateMiniStat("إجمالي مرات الصيانة", _lblStatTimes, "\uE90F"), 2, 0);
        row.Controls.Add(CreateMiniStat("إجمالي العملاء بالصيانة", _lblStatCustomers, "\uE716"), 3, 0);
        root.Controls.Add(row, 0, 1);
        card.Controls.Add(root);
        return card;
    }

    private Control BuildDetailsCard()
    {
        var card = CreateCard(new Padding(12, 10, 12, 10));
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var title = SectionTitle("تفاصيل العميل");
        title.Dock = DockStyle.Fill;
        var icon = new PictureBox
        {
            Dock = DockStyle.Right,
            Width = 28,
            SizeMode = PictureBoxSizeMode.CenterImage,
            Image = GlyphHelper.Create("\uE77B", InvoiceTheme.Gold, 16),
            BackColor = Color.Transparent
        };
        header.Controls.Add(title);
        header.Controls.Add(icon);

        _lblDetailName = new Label
        {
            Dock = DockStyle.Fill,
            Font = InvoiceTheme.SectionFont,
            ForeColor = InvoiceTheme.Gold,
            TextAlign = ContentAlignment.MiddleRight,
            Text = "—"
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        for (var c = 0; c < 3; c++)
        {
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        }

        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        _lblPhone = new Label();
        _lblPlate = new Label();
        _lblCar = new Label();
        _lblLastDate = new Label();
        _lblTotalExp = new Label();
        _lblVisits = new Label();
        grid.Controls.Add(CreateInfoChip("الهاتف", _lblPhone), 0, 0);
        grid.Controls.Add(CreateInfoChip("رقم السيارة", _lblPlate), 1, 0);
        grid.Controls.Add(CreateInfoChip("نوع السيارة", _lblCar), 2, 0);
        grid.Controls.Add(CreateInfoChip("تاريخ آخر صيانة", _lblLastDate), 0, 1);
        grid.Controls.Add(CreateInfoChip("إجمالي مصروفات الصيانة", _lblTotalExp), 1, 1);
        grid.Controls.Add(CreateInfoChip("عدد مرات الصيانة", _lblVisits), 2, 1);

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        _lblLastService = new Label();
        _lblKm = new Label();
        footer.Controls.Add(CreateWideChip("آخر خدمة تم تنفيذها", _lblLastService, null), 0, 0);
        footer.Controls.Add(CreateWideChip("الكيلومتر الحالي", _lblKm, "\uE7EC"), 0, 1);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(_lblDetailName, 0, 1);
        root.Controls.Add(grid, 0, 2);
        root.Controls.Add(footer, 0, 3);
        card.Controls.Add(root);
        return card;
    }

    private Control BuildHistoryCard()
    {
        var card = CreateCard(new Padding(10, 8, 10, 8));
        card.Margin = new Padding(0, 8, 0, 0);
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var title = SectionTitle("سجل صيانة العميل");
        title.Dock = DockStyle.Fill;
        var icon = new PictureBox
        {
            Dock = DockStyle.Right,
            Width = 28,
            SizeMode = PictureBoxSizeMode.CenterImage,
            Image = GlyphHelper.Create("\uE8A5", InvoiceTheme.Gold, 16),
            BackColor = Color.Transparent
        };
        header.Controls.Add(title);
        header.Controls.Add(icon);

        _historyGrid = CreateGrid();
        _historyGrid.RightToLeft = RightToLeft.Yes;
        _historyGrid.RowTemplate.Height = 30;
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "التاريخ", FillWeight = 14 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInv", HeaderText = "رقم الفاتورة", FillWeight = 14 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSvc", HeaderText = "الخدمات المنفذة", FillWeight = 24 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colKm", HeaderText = "الكيلومتر", FillWeight = 12 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTech", HeaderText = "اسم الفني", FillWeight = 14 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal", HeaderText = "الإجمالي", FillWeight = 12 });
        _historyGrid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "colView",
            HeaderText = "إجراء",
            Text = "عرض",
            UseColumnTextForButtonValue = true,
            FlatStyle = FlatStyle.Flat,
            FillWeight = 10
        });
        _historyGrid.CellPainting += PaintActionButton;
        _historyGrid.CellClick += OnHistoryCellClick;
        EnableDoubleBuffering(_historyGrid);

        _historyPager = PaginationStyle.CreateBar();
        var viewAll = CreateToolbarButton("عرض الكل", false, (_, _) =>
        {
            _showAllHistory = !_showAllHistory;
            _historyPage = 0;
            BindHistory();
        });
        viewAll.Height = 30;
        viewAll.Margin = new Padding(0, 2, 0, 0);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(_historyGrid, 0, 1);
        root.Controls.Add(_historyPager, 0, 2);
        root.Controls.Add(viewAll, 0, 3);
        card.Controls.Add(root);
        return card;
    }

    private void BindGrid()
    {
        _grid.Rows.Clear();
        var totalPages = Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)PageSize));
        if (_page >= totalPages)
        {
            _page = totalPages - 1;
        }

        var pageItems = _filtered.Skip(_page * PageSize).Take(PageSize).ToList();
        var index = _page * PageSize + 1;
        foreach (var v in pageItems)
        {
            _grid.Rows.Add(
                index++,
                v.Customer.Name,
                v.Customer.Phone,
                v.CarName,
                v.Plate,
                v.VisitCount,
                v.LastMaintenance?.ToString("dd/MM/yyyy") ?? "—",
                FormatMoney(v.TotalExpenses),
                "عرض التفاصيل");
            _grid.Rows[^1].Tag = v;
            _grid.Rows[^1].Cells["colIndex"].Style.ForeColor = InvoiceTheme.Gold;
        }

        BuildNumberedPager(_pager, totalPages, _page, p =>
        {
            _page = p;
            BindGrid();
        });

        if (_grid.Rows.Count > 0)
        {
            _grid.ClearSelection();
            _grid.Rows[0].Selected = true;
            OnCustomerSelected();
        }
        else
        {
            ClearDetails();
        }
    }

    private void BindHistory()
    {
        _historyGrid.Rows.Clear();
        var invoices = _selected?.Invoices ?? [];
        if (invoices.Count == 0 && _selected is not null)
        {
            // Fall back to customer embedded invoice/service summaries
            foreach (var s in _selected.Customer.Services.OrderByDescending(x => x.Date)
                         .Skip(_historyPage * HistoryPageSize).Take(HistoryPageSize))
            {
                _historyGrid.Rows.Add(
                    s.Date.ToString("dd/MM/yyyy"),
                    "—",
                    s.Service,
                    "—",
                    s.Technician,
                    "—",
                    "عرض");
            }

            foreach (var inv in _selected.Customer.Invoices.OrderByDescending(x => x.Date)
                         .Skip(_historyPage * HistoryPageSize).Take(HistoryPageSize))
            {
                _historyGrid.Rows.Add(
                    inv.Date.ToString("dd/MM/yyyy"),
                    inv.Number,
                    "—",
                    "—",
                    "—",
                    FormatMoney(inv.Amount),
                    "عرض");
            }

            var fallbackCount = Math.Max(_selected.Customer.Services.Count, _selected.Customer.Invoices.Count);
            var fallbackPages = Math.Max(1, (int)Math.Ceiling(fallbackCount / (double)HistoryPageSize));
            BuildNumberedPager(_historyPager, fallbackPages, _historyPage, p =>
            {
                _historyPage = p;
                BindHistory();
            }, compact: true);
            return;
        }

        if (!_showAllHistory)
        {
            invoices = invoices.Take(HistoryPageSize * 3).ToList();
        }

        var totalPages = Math.Max(1, (int)Math.Ceiling(invoices.Count / (double)HistoryPageSize));
        if (_historyPage >= totalPages)
        {
            _historyPage = totalPages - 1;
        }

        foreach (var inv in invoices.Skip(_historyPage * HistoryPageSize).Take(HistoryPageSize))
        {
            var services = inv.Items.Count > 0
                ? string.Join("، ", inv.Items.Select(i => i.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Take(3))
                : "—";
            _historyGrid.Rows.Add(
                inv.CreatedAt.ToString("dd/MM/yyyy"),
                inv.Number,
                services,
                string.IsNullOrWhiteSpace(inv.Odometer) ? "—" : inv.Odometer,
                string.IsNullOrWhiteSpace(inv.Technician) ? "—" : inv.Technician,
                FormatMoney(inv.GrandTotal),
                "عرض");
            _historyGrid.Rows[^1].Tag = inv;
        }

        BuildNumberedPager(_historyPager, totalPages, _historyPage, p =>
        {
            _historyPage = p;
            BindHistory();
        }, compact: true);
    }

    private void OnCustomerSelected()
    {
        if (_grid.CurrentRow?.Tag is MaintenanceCustomerView v)
        {
            ShowDetails(v);
        }
    }

    private void OnCustomerCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        if (_grid.Columns[e.ColumnIndex].Name == "colAction" && _grid.Rows[e.RowIndex].Tag is MaintenanceCustomerView v)
        {
            _selected = v;
            ShowDetails(v);
            _grid.ClearSelection();
            _grid.Rows[e.RowIndex].Selected = true;
        }
    }

    private void OnHistoryCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        if (_historyGrid.Columns[e.ColumnIndex].Name == "colView" && _historyGrid.Rows[e.RowIndex].Tag is InvoiceRecord inv)
        {
            using var dlg = new InvoiceDetailsDialog(inv);
            dlg.ShowDialog(FindForm());
        }
    }

    private void ShowDetails(MaintenanceCustomerView v)
    {
        _selected = v;
        _historyPage = 0;
        _lblDetailName.Text = v.Customer.Name;
        _lblPhone.Text = string.IsNullOrWhiteSpace(v.Customer.Phone) ? "—" : v.Customer.Phone;
        _lblPlate.Text = v.Plate;
        _lblCar.Text = v.CarName;
        _lblLastDate.Text = v.LastMaintenance?.ToString("dd/MM/yyyy") ?? "—";
        _lblTotalExp.Text = FormatMoney(v.TotalExpenses);
        _lblVisits.Text = v.VisitCount.ToString();
        _lblLastService.Text = v.LastService;
        _lblKm.Text = v.CurrentKm;
        BindHistory();
    }

    private void ClearDetails()
    {
        _selected = null;
        _lblDetailName.Text = "—";
        _lblPhone.Text = "—";
        _lblPlate.Text = "—";
        _lblCar.Text = "—";
        _lblLastDate.Text = "—";
        _lblTotalExp.Text = "—";
        _lblVisits.Text = "—";
        _lblLastService.Text = "—";
        _lblKm.Text = "—";
        _historyGrid.Rows.Clear();
        _historyPager.Controls.Clear();
    }

    private void ExportReport()
    {
        var lines = new List<string>
        {
            "تقرير الصيانة - Ibrahim Abdo Auto Service",
            $"التاريخ: {DateTime.Now:dd/MM/yyyy HH:mm}",
            $"عدد العملاء: {_filtered.Count}",
            $"إجمالي مرات الصيانة: {_filtered.Sum(v => v.VisitCount)}",
            $"إجمالي المصروفات: {FormatMoney(_filtered.Sum(v => v.TotalExpenses))}",
            "",
            "الاسم,الهاتف,السيارة,اللوحة,عدد الصيانات,آخر صيانة,الإجمالي"
        };
        lines.AddRange(_filtered.Select(v =>
            $"{v.Customer.Name},{v.Customer.Phone},{v.CarName},{v.Plate},{v.VisitCount},{v.LastMaintenance:dd/MM/yyyy},{v.TotalExpenses:0.00}"));

        var dir = Path.Combine(AppContext.BaseDirectory, "Reports");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"maintenance-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        File.WriteAllLines(path, lines);
        AppMessageDialog.Success(this, $"تم تصدير تقرير الصيانة.\r\n{Path.GetFileName(path)}", "تقرير الصيانة");
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch
        {
            // ignore
        }
    }

    private static void BuildNumberedPager(FlowLayoutPanel host, int totalPages, int current, Action<int> go, bool compact = false)
    {
        host.Controls.Clear();
        host.Controls.Add(PaginationStyle.CreateNavButton("السابق", (_, _) =>
        {
            if (current > 0)
            {
                go(current - 1);
            }
        }));

        var maxButtons = compact ? 5 : 8;
        var start = Math.Max(0, Math.Min(current - 2, Math.Max(0, totalPages - maxButtons)));
        var end = Math.Min(totalPages - 1, start + maxButtons - 1);
        for (var i = start; i <= end; i++)
        {
            var page = i;
            var btn = PaginationStyle.CreatePageButton((page + 1).ToString(), active: page == current);
            btn.Click += (_, _) => go(page);
            host.Controls.Add(btn);
        }

        host.Controls.Add(PaginationStyle.CreateNavButton("التالي", (_, _) =>
        {
            if (current < totalPages - 1)
            {
                go(current + 1);
            }
        }));
    }

    private void PaintActionButton(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (sender is not DataGridView grid || e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        var name = grid.Columns[e.ColumnIndex].Name;
        if (name is not ("colAction" or "colView"))
        {
            return;
        }

        e.PaintBackground(e.ClipBounds, true);
        var text = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].FormattedValue?.ToString() ?? "";
        var rect = e.CellBounds;
        rect.Inflate(-6, -6);
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        const int r = 6;
        path.AddArc(rect.X, rect.Y, r, r, 180, 90);
        path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
        path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
        path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
        path.CloseFigure();
        using var brush = new SolidBrush(InvoiceTheme.InputFill);
        using var pen = new Pen(InvoiceTheme.Gold, 1f);
        e.Graphics!.FillPath(brush, path);
        e.Graphics.DrawPath(pen, path);
        TextRenderer.DrawText(e.Graphics, text, InvoiceTheme.SmallFont, rect, InvoiceTheme.Gold,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        e.Handled = true;
    }

    private static Guna2Panel CreateCard(Padding padding) =>
        new()
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Padding = padding,
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

    private static Control CreateMiniStat(string title, Label value, string glyph)
    {
        var card = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.InputFill,
            BorderColor = Color.FromArgb(80, InvoiceTheme.Gold),
            BorderThickness = 1,
            BorderRadius = 10,
            Margin = new Padding(4),
            Padding = new Padding(10, 8, 10, 8)
        };
        var icon = new PictureBox
        {
            Dock = DockStyle.Left,
            Width = 34,
            SizeMode = PictureBoxSizeMode.CenterImage,
            Image = GlyphHelper.Create(glyph, InvoiceTheme.Gold, 18),
            BackColor = Color.Transparent
        };
        var texts = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var titleLbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            Text = title,
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleLeft
        };
        value.Dock = DockStyle.Fill;
        value.ForeColor = InvoiceTheme.Gold;
        value.Font = InvoiceTheme.SectionFont;
        value.TextAlign = ContentAlignment.MiddleLeft;
        value.Text = "0";
        texts.Controls.Add(value);
        texts.Controls.Add(titleLbl);
        card.Controls.Add(texts);
        card.Controls.Add(icon);
        return card;
    }

    private static Control CreateInfoChip(string title, Label value)
    {
        var card = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.InputFill,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = 8,
            Margin = new Padding(3),
            Padding = new Padding(8, 4, 8, 4)
        };
        var titleLbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 18,
            Text = title,
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleRight
        };
        value.Dock = DockStyle.Fill;
        value.ForeColor = InvoiceTheme.White;
        value.Font = InvoiceTheme.BodyFont;
        value.TextAlign = ContentAlignment.MiddleRight;
        value.Text = "—";
        card.Controls.Add(value);
        card.Controls.Add(titleLbl);
        return card;
    }

    private static Control CreateWideChip(string title, Label value, string? glyph)
    {
        var card = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.InputFill,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = 8,
            Margin = new Padding(0, 2, 0, 2),
            Padding = new Padding(10, 2, 10, 2)
        };
        if (glyph is not null)
        {
            card.Controls.Add(new PictureBox
            {
                Dock = DockStyle.Left,
                Width = 24,
                SizeMode = PictureBoxSizeMode.CenterImage,
                Image = GlyphHelper.Create(glyph, InvoiceTheme.Gold, 14),
                BackColor = Color.Transparent
            });
        }

        var titleLbl = new Label
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            Text = title + ": ",
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 6, 0, 0)
        };
        value.Dock = DockStyle.Fill;
        value.ForeColor = InvoiceTheme.Gold;
        value.Font = InvoiceTheme.BodyFont;
        value.TextAlign = ContentAlignment.MiddleRight;
        value.Text = "—";
        card.Controls.Add(value);
        card.Controls.Add(titleLbl);
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
            Font = InvoiceTheme.BodyFont,
            ForeColor = InvoiceTheme.White,
            ColumnHeadersHeight = 34
        };
        g.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = InvoiceTheme.Card,
            ForeColor = InvoiceTheme.White,
            SelectionBackColor = Color.FromArgb(48, InvoiceTheme.Gold),
            SelectionForeColor = InvoiceTheme.White,
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };
        g.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = InvoiceTheme.RowAlt,
            ForeColor = InvoiceTheme.White,
            SelectionBackColor = Color.FromArgb(48, InvoiceTheme.Gold),
            SelectionForeColor = InvoiceTheme.White
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

    private static Guna2ComboBox CreateFilterCombo(string placeholder, string[] items)
    {
        var combo = new Guna2ComboBox
        {
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(90, InvoiceTheme.Gold),
            FillColor = InvoiceTheme.InputFill,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.SmallFont,
            Size = new Size(120, 36),
            Margin = new Padding(0, 0, 8, 0),
            FocusedColor = InvoiceTheme.Gold,
            ItemsAppearance = { BackColor = InvoiceTheme.Card, ForeColor = InvoiceTheme.White }
        };
        foreach (var item in items)
        {
            combo.Items.Add(item);
        }

        combo.SelectedIndex = 0;
        _ = placeholder;
        return combo;
    }

    private static Guna2TextBox CreateDateBox(string placeholder) =>
        new()
        {
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(90, InvoiceTheme.Gold),
            FillColor = InvoiceTheme.InputFill,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.SmallFont,
            PlaceholderText = placeholder,
            PlaceholderForeColor = InvoiceTheme.Muted,
            Size = new Size(110, 36),
            Margin = new Padding(0, 0, 8, 0),
            IconLeft = GlyphHelper.Create("\uE787", InvoiceTheme.Gold, 14),
            IconLeftSize = new Size(16, 16),
            FocusedState = { BorderColor = InvoiceTheme.Gold }
        };

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
        btn.Width = Math.Max(btn.MinimumSize.Width, TextRenderer.MeasureText(text, btn.Font).Width + 36);
        btn.Click += onClick;
        return btn;
    }

    private static bool TryParseDate(string? text, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return DateTime.TryParse(text.Trim(), out date);
    }

    private static string FormatMoney(decimal value) => $"{value:N0} ج.م";

    private void UpdateClock() => _lblDate.Text = DateTime.Now.ToString("dd MMM yyyy");

    private static void EnableDoubleBuffering(DataGridView grid) =>
        typeof(DataGridView)
            .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(grid, true);

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _clockTimer.Stop();
        _clockTimer.Dispose();
        base.OnFormClosed(e);
    }
}
