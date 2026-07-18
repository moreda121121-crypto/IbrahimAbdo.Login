using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

internal sealed class CustomersForm : Form
{
    private readonly System.Windows.Forms.Timer _clockTimer = new() { Interval = 1000 };
    private Label _lblTime = null!;
    private Label _lblDate = null!;
    private Guna2TextBox _txtSearch = null!;
    private DataGridView _grid = null!;
    private Label _lblTotalCustomers = null!;
    private Label _lblTotalCars = null!;
    private Label _lblTotalSales = null!;
    private Label _lblVip = null!;
    private Label _lblPage = null!;
    private Label _detailName = null!;
    private Label _detailPhone = null!;
    private Label _detailAddress = null!;
    private Label _detailRegistered = null!;
    private Label _detailNotes = null!;
    private DataGridView _carsGrid = null!;
    private DataGridView _invoicesGrid = null!;
    private DataGridView _servicesGrid = null!;
    private Size? _matchLoginSize;
    private readonly bool _embedded;

    private List<CustomerRecord> _filtered = [];
    private CustomerRecord? _selected;
    private int _page;
    private const int PageSize = 8;

    private Bitmap _viewIcon = null!;
    private Bitmap _editIcon = null!;
    private Bitmap _deleteIcon = null!;

    public CustomersForm(bool embedded = false)
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
        MaximumSize = new Size(1600, 900);
        MinimizeBox = !_embedded;
        MinimumSize = _embedded ? Size.Empty : new Size(1100, 650);
        Name = "CustomersForm";
        RightToLeft = RightToLeft.No;
        RightToLeftLayout = false;
        ShowIcon = !_embedded;
        ShowInTaskbar = !_embedded;
        StartPosition = FormStartPosition.Manual;
        Text = _embedded ? string.Empty : "العملاء - Ibrahim Abdo Auto Service";
        WindowState = FormWindowState.Normal;

        CustomerStore.Load();
        _viewIcon = GlyphHelper.Create("\uE7B3", InvoiceTheme.Gold, 14);
        _editIcon = GlyphHelper.Create("\uE70F", InvoiceTheme.Gold, 14);
        _deleteIcon = GlyphHelper.Create("\uE74D", InvoiceTheme.Danger, 14);

        BuildUi();
        RefreshData();

        if (!_embedded)
        {
            Shown += (_, _) => PlaceCenteredOnScreen();
        }

        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();
        ResumeLayout(true);
    }

    public void MatchLoginWindow(Form login)
    {
        _matchLoginSize = login.Size;
        MinimumSize = login.MinimumSize;
        MaximumSize = login.MaximumSize;
        FormBorderStyle = login.FormBorderStyle;
        WindowState = FormWindowState.Normal;
        Size = login.Size;
    }

    private void PlaceCenteredOnScreen()
    {
        WindowState = FormWindowState.Normal;
        if (_matchLoginSize is { } match && match.Width > 0 && match.Height > 0)
        {
            Size = match;
        }
        else
        {
            ClientSize = new Size(1280, 720);
        }

        var area = Screen.FromPoint(Location).WorkingArea;
        Left = area.Left + Math.Max(0, (area.Width - Width) / 2);
        Top = area.Top + Math.Max(0, (area.Height - Height) / 2);
    }

    private void BuildUi()
    {
        var main = BuildMain();
        main.Dock = DockStyle.Fill;

        // Embedded inside MainShellForm: content only (sidebar lives on the shell).
        if (_embedded)
        {
            Controls.Add(main);
            return;
        }

        var shell = new Panel { Dock = DockStyle.Fill, BackColor = InvoiceTheme.Background, RightToLeft = RightToLeft.No };
        var sidebar = BuildSidebar();
        sidebar.Dock = DockStyle.Left;
        sidebar.Width = InvoiceTheme.SidebarWidth;
        shell.Controls.Add(main);
        shell.Controls.Add(sidebar);
        Controls.Add(shell);
    }

    private Control BuildSidebar()
    {
        var sidebar = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.Sidebar,
            CustomBorderColor = InvoiceTheme.CardBorder,
            CustomBorderThickness = new Padding(0, 0, 1, 0),
            Padding = new Padding(12, 12, 12, 16)
        };

        var logo = new Panel { Dock = DockStyle.Top, Height = 220, BackColor = Color.Transparent, Padding = new Padding(2, 0, 2, 6) };
        var logoImage = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent };
        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "logo-ibrahim.png");
        if (File.Exists(logoPath))
        {
            using var fs = File.OpenRead(logoPath);
            logoImage.Image = Image.FromStream(fs);
        }

        logo.Controls.Add(logoImage);

        var menuHost = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0)
        };

        string[][] items =
        [
            ["\uE80F", "لوحة التحكم", "dashboard"],
            ["\uE77B", "العملاء", "customers"],
            ["\uE7EC", "السيارات", "vehicles"],
            ["\uE90F", "الخدمات", "services"],
            ["\uE8A5", "فاتورة البيع", "invoice"],
            ["\uE8F1", "المخزون", "inventory"],
            ["\uE718", "الفنيون", "techs"],
            ["\uE716", "المستخدمون", "users"],
            ["\uE713", "الإعدادات", "settings"],
        ];

        foreach (var item in items)
        {
            menuHost.Controls.Add(CreateMenuItem(item[0], item[1], item[2], item[2] == "customers"));
        }

        sidebar.Controls.Add(menuHost);
        sidebar.Controls.Add(logo);
        return sidebar;
    }

    private Control CreateMenuItem(string glyph, string text, string key, bool active)
    {
        const int iconPx = 26;
        var row = new Guna2Panel
        {
            Width = InvoiceTheme.SidebarWidth - 28,
            Height = 46,
            Margin = new Padding(0, 0, 0, 4),
            BorderRadius = 8,
            FillColor = active ? Color.FromArgb(48, InvoiceTheme.Gold) : Color.Transparent,
            Cursor = Cursors.Hand,
            CustomBorderColor = InvoiceTheme.Gold,
            CustomBorderThickness = active ? new Padding(0, 1, 0, 1) : new Padding(0)
        };

        var icon = new PictureBox
        {
            Dock = DockStyle.Left,
            Width = 36,
            SizeMode = PictureBoxSizeMode.CenterImage,
            BackColor = Color.Transparent,
            Image = GlyphHelper.Create(glyph, InvoiceTheme.Gold, iconPx),
            Padding = new Padding(8, 0, 0, 0)
        };

        var label = new Label
        {
            Dock = DockStyle.Fill,
            Text = text,
            Font = InvoiceTheme.MenuFont,
            ForeColor = active ? InvoiceTheme.Gold : Color.FromArgb(210, 210, 210),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
            Padding = new Padding(45, 0, 8, 0)
        };

        void Hover(bool on)
        {
            if (active) return;
            row.FillColor = on ? Color.FromArgb(36, InvoiceTheme.Gold) : Color.Transparent;
            label.ForeColor = on ? InvoiceTheme.Gold : Color.FromArgb(210, 210, 210);
        }

        void ClickNavigate(object? s, EventArgs e)
        {
            if (key == "invoice")
            {
                DialogResult = DialogResult.Retry; // signal: open invoice
                Close();
            }
            else if (key != "customers")
            {
                MessageBox.Show(this, $"صفحة «{text}» قريباً.", "قريباً", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        foreach (Control c in new Control[] { row, icon, label })
        {
            c.MouseEnter += (_, _) => Hover(true);
            c.MouseLeave += (_, _) => Hover(false);
            c.Click += ClickNavigate;
        }

        row.Controls.Add(icon);
        row.Controls.Add(label);
        return row;
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
            Text = "العملاء",
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
            RowCount = 4,
            Margin = new Padding(0, 0, 8, 0),
            BackColor = Color.Transparent
        };
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        host.Controls.Add(BuildToolbar(), 0, 0);
        host.Controls.Add(BuildStats(), 0, 1);
        host.Controls.Add(BuildCustomersGrid(), 0, 2);
        host.Controls.Add(BuildPagination(), 0, 3);
        return host;
    }

    private Control BuildToolbar()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _txtSearch = new Guna2TextBox
        {
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(90, InvoiceTheme.Gold),
            FillColor = InvoiceTheme.InputFill,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.BodyFont,
            PlaceholderText = "بحث بالاسم / الهاتف / رقم اللوحة",
            PlaceholderForeColor = InvoiceTheme.Muted,
            Dock = DockStyle.Left,
            Width = 320,
            Height = 36,
            Margin = new Padding(0, 4, 8, 0),
            IconLeft = GlyphHelper.Create("\uE721", InvoiceTheme.Gold, 14),
            IconLeftSize = new Size(16, 16),
            FocusedState = { BorderColor = InvoiceTheme.Gold }
        };
        _txtSearch.TextChanged += (_, _) => { _page = 0; RefreshData(); };

        // Keep buttons on one horizontal row (no wrap under each other)
        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 0),
            Padding = Padding.Empty,
            Anchor = AnchorStyles.Right
        };
        buttons.Controls.Add(CreateToolbarButton("+ إضافة عميل", true, (_, _) => OpenAddCustomer()));
        buttons.Controls.Add(CreateToolbarButton("طباعة", false, (_, _) => MessageBox.Show(this, "طباعة قائمة العملاء", "طباعة")));
        buttons.Controls.Add(CreateToolbarButton("تصدير Excel", false, (_, _) => MessageBox.Show(this, "تم تجهيز التصدير", "تصدير")));

        bar.Controls.Add(_txtSearch, 0, 0);
        bar.Controls.Add(buttons, 1, 0);
        return bar;
    }

    private Control BuildStats()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 4)
        };
        for (var i = 0; i < 4; i++)
        {
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        }

        _lblTotalCustomers = new Label();
        _lblTotalCars = new Label();
        _lblTotalSales = new Label();
        _lblVip = new Label();

        row.Controls.Add(CreateStatCard("إجمالي العملاء", _lblTotalCustomers, "\uE716"), 0, 0);
        row.Controls.Add(CreateStatCard("إجمالي السيارات", _lblTotalCars, "\uE7EC"), 1, 0);
        row.Controls.Add(CreateStatCard("إجمالي المبيعات", _lblTotalSales, "\uE8A5"), 2, 0);
        row.Controls.Add(CreateStatCard("عملاء VIP", _lblVip, "\uE735"), 3, 0);
        return row;
    }

    private static Control CreateStatCard(string title, Label valueLabel, string glyph)
    {
        var card = new Guna2Panel
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

        var icon = new PictureBox
        {
            Dock = DockStyle.Left,
            Width = 36,
            SizeMode = PictureBoxSizeMode.CenterImage,
            Image = GlyphHelper.Create(glyph, InvoiceTheme.Gold, 18),
            BackColor = Color.Transparent
        };

        var texts = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var titleLbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = title,
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleLeft
        };
        valueLabel.Dock = DockStyle.Fill;
        valueLabel.ForeColor = InvoiceTheme.Gold;
        valueLabel.Font = InvoiceTheme.SectionFont;
        valueLabel.TextAlign = ContentAlignment.MiddleLeft;
        valueLabel.Text = "0";
        texts.Controls.Add(valueLabel);
        texts.Controls.Add(titleLbl);

        card.Controls.Add(texts);
        card.Controls.Add(icon);
        return card;
    }

    private Control BuildCustomersGrid()
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
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIndex", HeaderText = "#", FillWeight = 6 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "اسم العميل", FillWeight = 16 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPhone", HeaderText = "رقم الهاتف", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPlate", HeaderText = "رقم اللوحة", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCar", HeaderText = "السيارة", FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCars", HeaderText = "عدد السيارات", FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colVisit", HeaderText = "آخر زيارة", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal", HeaderText = "إجمالي الفواتير", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewImageColumn { Name = "colView", HeaderText = "إجراء", FillWeight = 5, Image = _viewIcon, ImageLayout = DataGridViewImageCellLayout.Zoom });
        _grid.Columns.Add(new DataGridViewImageColumn { Name = "colEdit", HeaderText = "", FillWeight = 5, Image = _editIcon, ImageLayout = DataGridViewImageCellLayout.Zoom });
        _grid.Columns.Add(new DataGridViewImageColumn { Name = "colDelete", HeaderText = "", FillWeight = 5, Image = _deleteIcon, ImageLayout = DataGridViewImageCellLayout.Zoom });

        _grid.SelectionChanged += (_, _) => OnRowSelected();
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
            Padding = new Padding(4, 4, 0, 0)
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

    private Control BuildRightPane()
    {
        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Margin = new Padding(6, 0, 0, 0),
            Padding = new Padding(4, 8, 8, 8) // top padding — no clipping
        };

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Width = 320,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 4)
        };

        stack.Controls.Add(BuildCustomerInfoCard());
        stack.Controls.Add(BuildCarsCard());
        stack.Controls.Add(BuildInvoicesCard());
        stack.Controls.Add(BuildServicesCard());
        scroll.Controls.Add(stack);
        return scroll;
    }

    private Control BuildCustomerInfoCard()
    {
        var card = CreateDetailCard("بيانات العميل", 155);
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.Transparent,
            Padding = new Padding(12, 8, 12, 10),
            Margin = Padding.Empty
        };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); // name
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); // phone
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); // address
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); // registered
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // notes

        _detailName = CenterDetailLabel(InvoiceTheme.Gold, InvoiceTheme.SectionFont);
        _detailPhone = CenterDetailLabel(InvoiceTheme.White, InvoiceTheme.BodyFont);
        _detailAddress = CenterDetailLabel(InvoiceTheme.Muted, InvoiceTheme.SmallFont);
        _detailRegistered = CenterDetailLabel(InvoiceTheme.Muted, InvoiceTheme.SmallFont);
        _detailNotes = CenterDetailLabel(InvoiceTheme.Muted, InvoiceTheme.SmallFont);

        body.Controls.Add(_detailName, 0, 0);
        body.Controls.Add(_detailPhone, 0, 1);
        body.Controls.Add(_detailAddress, 0, 2);
        body.Controls.Add(_detailRegistered, 0, 3);
        body.Controls.Add(_detailNotes, 0, 4);
        card.Controls.Add(body);
        return card;
    }

    private Control BuildCarsCard()
    {
        var card = CreateDetailCard("سيارات العميل", 190);
        var addBtn = new Guna2Button
        {
            Text = "+ إضافة سيارة",
            Font = InvoiceTheme.SmallFont,
            ForeColor = InvoiceTheme.Gold,
            FillColor = Color.Transparent,
            BorderColor = InvoiceTheme.Gold,
            BorderThickness = 1,
            BorderRadius = 8,
            Height = 30,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 6),
            Cursor = Cursors.Hand,
            HoverState = { FillColor = Color.FromArgb(30, InvoiceTheme.Gold) }
        };
        addBtn.Click += (_, _) =>
        {
            if (_selected is null)
            {
                MessageBox.Show(this, "اختر عميلاً أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            MessageBox.Show(this, "استخدم «إضافة عميل» لإضافة سيارات متعددة، أو حدّث من التعديل.", "إضافة سيارة");
        };

        _carsGrid = CreateMiniGrid();
        _carsGrid.Dock = DockStyle.Fill;
        _carsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "اللوحة", FillWeight = 30 });
        _carsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الماركة", FillWeight = 25 });
        _carsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الموديل", FillWeight = 25 });
        _carsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "السنة", FillWeight = 20 });

        var wrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 8) };
        wrap.Controls.Add(_carsGrid);
        wrap.Controls.Add(addBtn);
        card.Controls.Add(wrap);
        return card;
    }

    private Control BuildInvoicesCard()
    {
        var card = CreateDetailCard("آخر الفواتير", 170);
        _invoicesGrid = CreateMiniGrid();
        _invoicesGrid.Dock = DockStyle.Fill;
        _invoicesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "رقم الفاتورة", FillWeight = 30 });
        _invoicesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "التاريخ", FillWeight = 25 });
        _invoicesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "المبلغ", FillWeight = 25 });
        _invoicesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الحالة", FillWeight = 20 });
        var wrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
        wrap.Controls.Add(_invoicesGrid);
        card.Controls.Add(wrap);
        return card;
    }

    private Control BuildServicesCard()
    {
        var card = CreateDetailCard("آخر الخدمات", 170);
        _servicesGrid = CreateMiniGrid();
        _servicesGrid.Dock = DockStyle.Fill;
        _servicesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "التاريخ", FillWeight = 25 });
        _servicesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الخدمة", FillWeight = 30 });
        _servicesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الفني", FillWeight = 25 });
        _servicesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الحالة", FillWeight = 20 });
        var wrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
        wrap.Controls.Add(_servicesGrid);
        card.Controls.Add(wrap);
        return card;
    }

    private static Guna2Panel CreateDetailCard(string title, int height)
    {
        var card = new Guna2Panel
        {
            Width = 312,
            Height = height,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(0),
            ShadowDecoration = { Enabled = true, Depth = 8, Color = Color.Black, BorderRadius = InvoiceTheme.Radius }
        };
        var header = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            Text = title,
            Font = InvoiceTheme.SectionFont,
            ForeColor = InvoiceTheme.Gold,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        card.Controls.Add(header);
        return card;
    }

    private static Label CenterDetailLabel(Color color, Font font) =>
        new()
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            AutoEllipsis = true,
            ForeColor = color,
            Font = font,
            Text = "—",
            TextAlign = ContentAlignment.MiddleCenter,
            RightToLeft = RightToLeft.No
        };

    private void RefreshData()
    {
        _filtered = CustomerStore.Search(_txtSearch?.Text ?? "").ToList();
        _lblTotalCustomers.Text = CustomerStore.TotalCustomers.ToString();
        _lblTotalCars.Text = CustomerStore.TotalCars.ToString();
        _lblTotalSales.Text = CustomerStore.TotalSales.ToString("N0");
        _lblVip.Text = CustomerStore.VipCount.ToString();
        BindGrid();
    }

    private void BindGrid()
    {
        _grid.Rows.Clear();
        var pageItems = _filtered.Skip(_page * PageSize).Take(PageSize).ToList();
        var index = _page * PageSize + 1;
        foreach (var c in pageItems)
        {
            var visit = c.LastVisit?.ToString("dd/MM/yyyy") ?? "—";
            _grid.Rows.Add(
                index++,
                c.Name,
                c.Phone,
                c.PrimaryPlate,
                c.PrimaryCarName,
                c.CarsCount,
                visit,
                c.TotalInvoices.ToString("N2"),
                _viewIcon,
                _editIcon,
                _deleteIcon);
            _grid.Rows[^1].Tag = c;
        }

        var totalPages = Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)PageSize));
        _lblPage.Text = $"صفحة {_page + 1} من {totalPages}";

        if (_grid.Rows.Count > 0)
        {
            _grid.ClearSelection();
            _grid.Rows[0].Selected = true;
        }
        else
        {
            ClearDetails();
        }
    }

    private void OnRowSelected()
    {
        if (_grid.CurrentRow?.Tag is CustomerRecord customer)
        {
            _selected = customer;
            ShowDetails(customer);
        }
    }

    private void OnGridCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
        if (_grid.Rows[e.RowIndex].Tag is not CustomerRecord customer) return;

        var col = _grid.Columns[e.ColumnIndex].Name;
        if (col == "colDelete")
        {
            if (MessageBox.Show(this, $"حذف العميل «{customer.Name}»؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                CustomerStore.Remove(customer.Id);
                RefreshData();
            }
        }
        else if (col == "colEdit")
        {
            MessageBox.Show(this, $"تعديل: {customer.Name}", "تعديل عميل");
        }
        else if (col == "colView")
        {
            _selected = customer;
            ShowDetails(customer);
        }
    }

    private void ShowDetails(CustomerRecord c)
    {
        _detailName.Text = c.Name + (c.IsVip ? "  ★ VIP" : "");
        _detailPhone.Text = c.Phone;
        _detailAddress.Text = string.IsNullOrWhiteSpace(c.Address) ? "—" : c.Address;
        _detailRegistered.Text = $"تاريخ التسجيل: {c.RegisteredAt:dd/MM/yyyy}";
        _detailNotes.Text = string.IsNullOrWhiteSpace(c.Notes) ? "لا توجد ملاحظات" : c.Notes;

        BindMiniGrid(_carsGrid, c.Cars.Select(car => new object[]
        {
            car.PlateNumber, car.Brand, car.Model, car.Year.ToString()
        }));

        BindMiniGrid(_invoicesGrid, c.Invoices.OrderByDescending(i => i.Date).Take(5).Select(inv => new object[]
        {
            inv.Number, inv.Date.ToString("dd/MM/yyyy"), inv.Amount.ToString("N2"), inv.Status
        }));

        BindMiniGrid(_servicesGrid, c.Services.OrderByDescending(s => s.Date).Take(5).Select(svc => new object[]
        {
            svc.Date.ToString("dd/MM/yyyy"), svc.Service, svc.Technician, svc.Status
        }));
    }

    private static void BindMiniGrid(DataGridView grid, IEnumerable<object[]> rows)
    {
        grid.SuspendLayout();
        grid.Rows.Clear();
        foreach (var row in rows)
        {
            grid.Rows.Add(row);
        }

        grid.ClearSelection();
        grid.CurrentCell = null;
        grid.ResumeLayout();
        grid.Refresh();
    }

    private void ClearDetails()
    {
        _selected = null;
        _detailName.Text = "—";
        _detailPhone.Text = "—";
        _detailAddress.Text = "—";
        _detailRegistered.Text = "—";
        _detailNotes.Text = "—";
        _carsGrid.Rows.Clear();
        _invoicesGrid.Rows.Clear();
        _servicesGrid.Rows.Clear();
    }

    private void OpenAddCustomer()
    {
        using var dlg = new AddCustomerDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result is not null)
        {
            CustomerStore.Add(dlg.Result);
            _page = 0;
            RefreshData();
        }
    }

    private static DataGridView CreateGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = InvoiceTheme.Card,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            EnableHeadersVisualStyles = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AllowUserToResizeColumns = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 34 },
            Font = InvoiceTheme.BodyFont,
            GridColor = Color.FromArgb(45, 45, 45),
            RightToLeft = RightToLeft.Yes,
            AutoGenerateColumns = false
        };
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = InvoiceTheme.Gold,
            ForeColor = Color.Black,
            Font = InvoiceTheme.TableHeaderFont,
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = InvoiceTheme.Card,
            ForeColor = InvoiceTheme.White,
            SelectionBackColor = Color.FromArgb(50, InvoiceTheme.Gold),
            SelectionForeColor = InvoiceTheme.White,
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = InvoiceTheme.RowAlt,
            ForeColor = InvoiceTheme.White,
            SelectionBackColor = Color.FromArgb(50, InvoiceTheme.Gold),
            SelectionForeColor = InvoiceTheme.White,
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };
        grid.ColumnHeadersHeight = 36;
        EnableDoubleBuffering(grid);
        return grid;
    }

    private static DataGridView CreateMiniGrid()
    {
        var g = CreateGrid();
        g.RightToLeft = RightToLeft.Yes;
        g.RowTemplate.Height = 28;
        g.ColumnHeadersHeight = 30;
        g.Font = InvoiceTheme.SmallFont;
        g.ScrollBars = ScrollBars.None;
        g.ClearSelection();
        return g;
    }

    private static Guna2Button CreateToolbarButton(string text, bool primary, EventHandler onClick)
    {
        var btn = new Guna2Button
        {
            Text = text,
            Font = InvoiceTheme.SmallFont,
            Height = 36,
            MinimumSize = new Size(110, 36),
            Padding = new Padding(14, 0, 14, 0),
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
        // Measure text so Arabic labels aren't clipped
        var textW = TextRenderer.MeasureText(text, btn.Font).Width;
        btn.Width = Math.Max(btn.MinimumSize.Width, textW + 36);
        btn.Click += onClick;
        return btn;
    }

    private static Guna2Button CreateChromeIcon(string glyph)
    {
        return new Guna2Button
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
    }

    private static void EnableDoubleBuffering(DataGridView grid)
    {
        typeof(DataGridView)
            .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(grid, true, null);
    }

    private void UpdateClock()
    {
        _lblTime.Text = DateTime.Now.ToString("hh:mm tt");
        _lblDate.Text = DateTime.Now.ToString("dd / MM / yyyy");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _clockTimer.Dispose();
        }

        base.Dispose(disposing);
    }
}
