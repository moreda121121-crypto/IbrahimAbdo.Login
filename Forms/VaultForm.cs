using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

internal sealed class VaultForm : Form
{
    private static readonly Color ProfitGreen = Color.FromArgb(80, 200, 120);

    private readonly System.Windows.Forms.Timer _clockTimer = new() { Interval = 1000 };
    private readonly bool _embedded;

    private Label _lblDate = null!;
    private Label _lblBalance = null!;
    private Label _lblIncome = null!;
    private Label _lblExpense = null!;
    private Label _lblCount = null!;
    private Label _lblSafeBalance = null!;
    private Label _lblLastUpdate = null!;
    private Label _lblOpen = null!;
    private Label _lblDetailIncome = null!;
    private Label _lblDetailExpense = null!;
    private Label _lblDetailBalance = null!;
    private DataGridView _grid = null!;
    private FlowLayoutPanel _pager = null!;

    private int _page;
    private const int PageSize = 10;

    public VaultForm(bool embedded = false)
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
        Name = "VaultForm";
        RightToLeft = RightToLeft.No;
        ShowIcon = !_embedded;
        ShowInTaskbar = !_embedded;
        StartPosition = FormStartPosition.Manual;
        Text = _embedded ? string.Empty : "الخزنة - Ibrahim Abdo Auto Service";
        WindowState = FormWindowState.Normal;

        VaultStore.Load();
        BuildUi();
        RefreshData();

        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();
        ResumeLayout(true);
    }

    public void RefreshData()
    {
        _lblBalance.Text = FormatMoney(VaultStore.CurrentBalance);
        _lblIncome.Text = FormatMoney(VaultStore.TotalIncome);
        _lblExpense.Text = FormatMoney(VaultStore.TotalExpense);
        _lblCount.Text = $"{VaultStore.MovementCount} حركة";

        _lblSafeBalance.Text = FormatMoney(VaultStore.CurrentBalance);
        _lblOpen.Text = FormatMoney(VaultStore.OpeningBalance);
        _lblDetailIncome.Text = FormatMoney(VaultStore.TotalIncome);
        _lblDetailExpense.Text = FormatMoney(VaultStore.TotalExpense);
        _lblDetailBalance.Text = FormatMoney(VaultStore.CurrentBalance);

        var latest = VaultStore.Movements.OrderByDescending(m => m.At).FirstOrDefault();
        var stamp = latest?.At ?? DateTime.Now;
        _lblLastUpdate.Text = $"أخر تحديث: {stamp:dd/MM/yyyy} - {stamp:hh:mm tt}";

        BindGrid();
        BuildPager();
    }

    private void BuildUi()
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
        main.Controls.Add(BuildBody(), 0, 1);
        Controls.Add(main);
    }

    private Control BuildTopBar()
    {
        var bar = new Panel { Dock = DockStyle.Fill, BackColor = InvoiceTheme.Background };

        var titleHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var titleInner = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.None
        };
        var titleIcon = new PictureBox
        {
            Size = new Size(32, 32),
            SizeMode = PictureBoxSizeMode.CenterImage,
            Image = GlyphHelper.CreateSafeIcon(InvoiceTheme.Gold, 24),
            Margin = new Padding(0, 4, 8, 0),
            BackColor = Color.Transparent
        };
        var title = new Label
        {
            AutoSize = true,
            Text = "الخزنة",
            Font = new Font(InvoiceTheme.Family.FontFamily, 20F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = InvoiceTheme.White,
            Margin = new Padding(0, 6, 0, 0)
        };
        titleInner.Controls.Add(titleIcon);
        titleInner.Controls.Add(title);
        titleHost.Controls.Add(titleInner);
        titleHost.Resize += (_, _) =>
        {
            titleInner.Left = Math.Max(0, (titleHost.ClientSize.Width - titleInner.Width) / 2);
            titleInner.Top = Math.Max(0, (titleHost.ClientSize.Height - titleInner.Height) / 2);
        };

        var export = CreateToolbarButton("تصدير تقرير الخزنة", false, (_, _) => ExportReport());
        export.Image = GlyphHelper.Create("\uE896", InvoiceTheme.Gold, 14);
        export.ImageAlign = HorizontalAlignment.Left;
        export.TextAlign = HorizontalAlignment.Right;
        export.Dock = DockStyle.Left;
        export.Margin = new Padding(0);
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

    private Control BuildBody()
    {
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        host.Controls.Add(BuildKpis(), 0, 0);
        host.Controls.Add(BuildSplit(), 0, 1);
        return host;
    }

    private Control BuildKpis()
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

        _lblBalance = new Label();
        _lblIncome = new Label();
        _lblExpense = new Label();
        _lblCount = new Label();

        row.Controls.Add(CreateKpiCard("الرصيد الحالي", _lblBalance, GlyphHelper.CreateSafeIcon(InvoiceTheme.Gold, 20)), 0, 0);
        row.Controls.Add(CreateKpiCard("إجمالي الإيرادات", _lblIncome, GlyphHelper.Create("\uE9D2", InvoiceTheme.Gold, 18), "+12.5%", true), 1, 0);
        row.Controls.Add(CreateKpiCard("إجمالي المصروفات", _lblExpense, GlyphHelper.Create("\uE9D2", InvoiceTheme.Gold, 18), "-8.3%", false), 2, 0);
        row.Controls.Add(CreateKpiCard("عدد الحركات", _lblCount, GlyphHelper.Create("\uE895", InvoiceTheme.Gold, 18)), 3, 0);
        return row;
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
        // Match screenshot: vault visual near sidebar (left), actions/history on the right
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64F));
        split.Controls.Add(BuildVaultColumn(), 0, 0);
        split.Controls.Add(BuildActionsColumn(), 1, 0);
        return split;
    }

    private Control BuildVaultColumn()
    {
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(4, 4, 8, 4),
            BackColor = Color.Transparent
        };
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 62F));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
        host.Controls.Add(BuildSafeBalanceCard(), 0, 0);
        host.Controls.Add(BuildBalanceDetailsCard(), 0, 1);
        return host;
    }

    private Control BuildSafeBalanceCard()
    {
        var card = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(14, 12, 14, 12),
            ShadowDecoration = { Enabled = true, Depth = 10, Color = Color.Black, BorderRadius = InvoiceTheme.Radius }
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        var header = new Label
        {
            Dock = DockStyle.Fill,
            Text = "رصيد الخزنة",
            Font = InvoiceTheme.SectionFont,
            ForeColor = InvoiceTheme.Gold,
            TextAlign = ContentAlignment.MiddleRight
        };

        var safeHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var safeImage = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        var safePath = Path.Combine(AppContext.BaseDirectory, "Assets", "vault-safe.png");
        if (File.Exists(safePath))
        {
            using var fs = File.OpenRead(safePath);
            safeImage.Image = Image.FromStream(fs);
        }

        safeHost.Controls.Add(safeImage);

        _lblSafeBalance = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font(InvoiceTheme.Family.FontFamily, 18F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = InvoiceTheme.Gold,
            TextAlign = ContentAlignment.MiddleCenter
        };
        var balCaption = new Label
        {
            Dock = DockStyle.Top,
            Height = 18,
            Text = "الرصيد الحالي",
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleCenter
        };
        var balBlock = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        balBlock.Controls.Add(_lblSafeBalance);
        balBlock.Controls.Add(balCaption);

        _lblLastUpdate = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleCenter
        };

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(safeHost, 0, 1);
        root.Controls.Add(balBlock, 0, 2);
        root.Controls.Add(_lblLastUpdate, 0, 3);
        card.Controls.Add(root);
        return card;
    }

    private Control BuildBalanceDetailsCard()
    {
        var card = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Padding = new Padding(10, 8, 10, 8),
            ShadowDecoration = { Enabled = true, Depth = 8, Color = Color.Black, BorderRadius = InvoiceTheme.Radius }
        };

        var details = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        for (var i = 0; i < 4; i++)
        {
            details.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        }

        _lblOpen = new Label();
        _lblDetailIncome = new Label();
        _lblDetailExpense = new Label();
        _lblDetailBalance = new Label();

        details.Controls.Add(CreateDetailRow("رصيد افتتاحي", _lblOpen, InvoiceTheme.White, false), 0, 0);
        details.Controls.Add(CreateDetailRow("إجمالي الإيرادات", _lblDetailIncome, ProfitGreen, false), 0, 1);
        details.Controls.Add(CreateDetailRow("إجمالي المصروفات", _lblDetailExpense, InvoiceTheme.Danger, false), 0, 2);
        details.Controls.Add(CreateDetailRow("الرصيد الحالي", _lblDetailBalance, InvoiceTheme.Gold, true), 0, 3);
        card.Controls.Add(details);
        return card;
    }

    private Control BuildActionsColumn()
    {
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(4, 4, 4, 4),
            BackColor = Color.Transparent
        };
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        host.Controls.Add(BuildQuickActions(), 0, 0);
        host.Controls.Add(BuildMovementsCard(), 0, 1);
        return host;
    }

    private Control BuildQuickActions()
    {
        var card = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(12, 10, 12, 10),
            ShadowDecoration = { Enabled = true, Depth = 8, Color = Color.Black, BorderRadius = InvoiceTheme.Radius }
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var header = new Label
        {
            Dock = DockStyle.Fill,
            Text = "عمليات سريعة",
            Font = InvoiceTheme.SectionFont,
            ForeColor = InvoiceTheme.Gold,
            TextAlign = ContentAlignment.MiddleRight
        };

        var actions = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        for (var i = 0; i < 5; i++)
        {
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        }

        actions.Controls.Add(CreateQuickAction("إضافة إيراد", ProfitGreen, "\uE74B", VaultMovementType.Income), 0, 0);
        actions.Controls.Add(CreateQuickAction("إضافة مصروف", InvoiceTheme.Danger, "\uE74A", VaultMovementType.Expense), 1, 0);
        actions.Controls.Add(CreateQuickAction("تحويل بين الحسابات", InvoiceTheme.Gold, "\uE8AB", VaultMovementType.Transfer), 2, 0);
        actions.Controls.Add(CreateQuickAction("سحب نقدي", InvoiceTheme.Gold, "\uE8C7", VaultMovementType.Withdraw), 3, 0);
        actions.Controls.Add(CreateQuickAction("إيداع نقدي", InvoiceTheme.Gold, "\uE8C8", VaultMovementType.Deposit), 4, 0);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(actions, 0, 1);
        card.Controls.Add(root);
        return card;
    }

    private Control BuildMovementsCard()
    {
        var card = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Padding = new Padding(10, 8, 10, 8),
            ShadowDecoration = { Enabled = true, Depth = 10, Color = Color.Black, BorderRadius = InvoiceTheme.Radius }
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "آخر الحركات",
            Font = InvoiceTheme.SectionFont,
            ForeColor = InvoiceTheme.Gold,
            TextAlign = ContentAlignment.MiddleRight
        };
        var viewAll = new LinkLabel
        {
            AutoSize = true,
            Text = "عرض كل الحركات",
            LinkColor = InvoiceTheme.Gold,
            ActiveLinkColor = InvoiceTheme.GoldDark,
            VisitedLinkColor = InvoiceTheme.Gold,
            Font = InvoiceTheme.SmallFont,
            Dock = DockStyle.Left,
            LinkBehavior = LinkBehavior.HoverUnderline,
            Margin = new Padding(0, 6, 0, 0)
        };
        viewAll.LinkClicked += (_, _) =>
        {
            _page = 0;
            RefreshData();
        };
        header.Controls.Add(title);
        header.Controls.Add(viewAll);

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
            AllowUserToResizeRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 34 },
            Font = InvoiceTheme.BodyFont,
            ForeColor = InvoiceTheme.White,
            RightToLeft = RightToLeft.Yes
        };
        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = InvoiceTheme.Card,
            ForeColor = InvoiceTheme.White,
            SelectionBackColor = Color.FromArgb(48, InvoiceTheme.Gold),
            SelectionForeColor = InvoiceTheme.White,
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };
        _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = InvoiceTheme.RowAlt,
            ForeColor = InvoiceTheme.White,
            SelectionBackColor = Color.FromArgb(48, InvoiceTheme.Gold),
            SelectionForeColor = InvoiceTheme.White
        };
        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = InvoiceTheme.Gold,
            ForeColor = Color.Black,
            Font = InvoiceTheme.TableHeaderFont,
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };
        _grid.ColumnHeadersHeight = 34;
        EnableDoubleBuffering(_grid);

        // RTL visual order: Date | Type | Description | Amount | Balance after | By
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "التاريخ", FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType", HeaderText = "النوع", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDesc", HeaderText = "الوصف", FillWeight = 26 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmount", HeaderText = "المبلغ", FillWeight = 16 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBal", HeaderText = "الرصيد بعد الحركة", FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBy", HeaderText = "تم بواسطة", FillWeight = 14 });

        _pager = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0)
        };

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(_grid, 0, 1);
        root.Controls.Add(_pager, 0, 2);
        card.Controls.Add(root);
        return card;
    }

    private void BindGrid()
    {
        _grid.Rows.Clear();
        var totalPages = Math.Max(1, (int)Math.Ceiling(VaultStore.MovementCount / (double)PageSize));
        if (_page >= totalPages)
        {
            _page = totalPages - 1;
        }

        var rows = VaultStore.Paged(_page, PageSize);
        foreach (var m in rows)
        {
            var income = VaultStore.IsIncomeLike(m.Type);
            var typeText = (income ? "▼ " : "▲ ") + VaultStore.TypeLabel(m.Type);
            var idx = _grid.Rows.Add(
                m.At.ToString("dd/MM/yyyy"),
                typeText,
                m.Description,
                FormatMoney(m.Amount),
                FormatMoney(m.BalanceAfter),
                m.By);

            var color = income ? ProfitGreen : InvoiceTheme.Danger;
            _grid.Rows[idx].Cells["colType"].Style.ForeColor = color;
            _grid.Rows[idx].Cells["colAmount"].Style.ForeColor = color;
        }
    }

    private void BuildPager()
    {
        _pager.Controls.Clear();
        var totalPages = Math.Max(1, (int)Math.Ceiling(VaultStore.MovementCount / (double)PageSize));
        void AddPage(string text, int target, bool active = false, bool enabled = true)
        {
            var btn = PaginationStyle.CreatePageButton(text, active, enabled);
            if (enabled)
            {
                btn.Click += (_, _) =>
                {
                    _page = target;
                    RefreshData();
                };
            }

            _pager.Controls.Add(btn);
        }

        // Same buttons and order as before — only visual style changed via PaginationStyle
        AddPage("‹", Math.Max(0, _page - 1), enabled: _page > 0);
        var window = 5;
        var start = Math.Max(0, Math.Min(_page - 2, totalPages - window));
        var end = Math.Min(totalPages - 1, start + window - 1);
        if (start > 0)
        {
            AddPage("1", 0, _page == 0);
            if (start > 1)
            {
                AddPage("…", _page, enabled: false);
            }
        }

        for (var i = start; i <= end; i++)
        {
            if (i == 0 && start > 0)
            {
                continue;
            }

            AddPage((i + 1).ToString(), i, i == _page);
        }

        if (end < totalPages - 1)
        {
            if (end < totalPages - 2)
            {
                AddPage("…", _page, enabled: false);
            }

            AddPage(totalPages.ToString(), totalPages - 1, _page == totalPages - 1);
        }

        AddPage("›", Math.Min(totalPages - 1, _page + 1), enabled: _page < totalPages - 1);

        _pager.Padding = new Padding(Math.Max(0, (_pager.ClientSize.Width - _pager.PreferredSize.Width) / 2), 4, 0, 0);
    }

    private void OpenMovement(VaultMovementType type)
    {
        using var dlg = new AddVaultMovementDialog(type);
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        VaultStore.Add(type, dlg.Description, dlg.Amount);
        _page = 0;
        RefreshData();
        AppMessageDialog.Success(this, "تم تسجيل الحركة بنجاح.", "الخزنة");
    }

    private void ExportReport()
    {
        var lines = new List<string>
        {
            "تقرير الخزنة - Ibrahim Abdo Auto Service",
            $"التاريخ: {DateTime.Now:dd/MM/yyyy HH:mm}",
            $"رصيد افتتاحي: {FormatMoney(VaultStore.OpeningBalance)}",
            $"إجمالي الإيرادات: {FormatMoney(VaultStore.TotalIncome)}",
            $"إجمالي المصروفات: {FormatMoney(VaultStore.TotalExpense)}",
            $"الرصيد الحالي: {FormatMoney(VaultStore.CurrentBalance)}",
            $"عدد الحركات: {VaultStore.MovementCount}",
            "",
            "التاريخ,النوع,الوصف,المبلغ,الرصيد بعد الحركة,تم بواسطة"
        };
        lines.AddRange(VaultStore.Movements.OrderByDescending(m => m.At).Select(m =>
            $"{m.At:dd/MM/yyyy HH:mm},{VaultStore.TypeLabel(m.Type)},{m.Description},{m.Amount:0.00},{m.BalanceAfter:0.00},{m.By}"));

        var dir = Path.Combine(AppContext.BaseDirectory, "Reports");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"vault-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        File.WriteAllLines(path, lines);
        AppMessageDialog.Success(this, $"تم تصدير تقرير الخزنة.\r\n{Path.GetFileName(path)}", "تقرير الخزنة");
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
            // ignore
        }
    }

    private static Control CreateKpiCard(string title, Label valueLabel, Image iconImage, string? trend = null, bool? trendUp = null)
    {
        var card = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Margin = new Padding(4),
            Padding = new Padding(12, 10, 12, 10),
            ShadowDecoration = { Enabled = true, Depth = 8, Color = Color.Black, BorderRadius = InvoiceTheme.Radius }
        };

        var iconBadge = new Guna2Panel
        {
            Dock = DockStyle.Left,
            Width = 42,
            BorderRadius = 21,
            FillColor = Color.FromArgb(40, InvoiceTheme.Gold),
            Margin = new Padding(0, 4, 8, 4)
        };
        var icon = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.CenterImage,
            Image = iconImage,
            BackColor = Color.Transparent
        };
        iconBadge.Controls.Add(icon);

        var texts = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var titleRow = new Panel { Dock = DockStyle.Top, Height = 22, BackColor = Color.Transparent };
        var titleLbl = new Label
        {
            Dock = DockStyle.Fill,
            Text = title,
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleLeft
        };
        titleRow.Controls.Add(titleLbl);

        if (!string.IsNullOrEmpty(trend) && trendUp.HasValue)
        {
            var badge = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Right,
                Text = (trendUp.Value ? "▲ " : "▼ ") + trend,
                ForeColor = Color.White,
                BackColor = trendUp.Value ? ProfitGreen : InvoiceTheme.Danger,
                Font = InvoiceTheme.SmallFont,
                Padding = new Padding(6, 2, 6, 2),
                Margin = new Padding(0, 2, 0, 0)
            };
            titleRow.Controls.Add(badge);
        }

        valueLabel.Dock = DockStyle.Fill;
        valueLabel.ForeColor = InvoiceTheme.Gold;
        valueLabel.Font = new Font(InvoiceTheme.Family.FontFamily, 13F, FontStyle.Bold, GraphicsUnit.Point);
        valueLabel.TextAlign = ContentAlignment.MiddleLeft;
        valueLabel.Text = "0";

        texts.Controls.Add(valueLabel);
        texts.Controls.Add(titleRow);
        card.Controls.Add(texts);
        card.Controls.Add(iconBadge);
        return card;
    }

    private static Control CreateDetailRow(string label, Label valueLabel, Color valueColor, bool highlight)
    {
        var row = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = highlight ? Color.FromArgb(48, InvoiceTheme.Gold) : Color.Transparent,
            BorderRadius = 8,
            Margin = new Padding(0, 2, 0, 2),
            Padding = new Padding(10, 0, 10, 0)
        };

        var name = new Label
        {
            Dock = DockStyle.Fill,
            Text = label,
            ForeColor = highlight ? InvoiceTheme.Gold : InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleRight,
            BackColor = Color.Transparent
        };
        valueLabel.Dock = DockStyle.Left;
        valueLabel.AutoSize = false;
        valueLabel.Width = 130;
        valueLabel.ForeColor = valueColor;
        valueLabel.Font = highlight ? InvoiceTheme.SectionFont : InvoiceTheme.BodyFont;
        valueLabel.TextAlign = ContentAlignment.MiddleLeft;
        valueLabel.BackColor = Color.Transparent;

        if (highlight)
        {
            var icon = new PictureBox
            {
                Dock = DockStyle.Right,
                Width = 28,
                SizeMode = PictureBoxSizeMode.CenterImage,
                Image = GlyphHelper.CreateSafeIcon(InvoiceTheme.Gold, 16),
                BackColor = Color.Transparent
            };
            row.Controls.Add(icon);
        }

        row.Controls.Add(name);
        row.Controls.Add(valueLabel);
        return row;
    }

    private Control CreateQuickAction(string text, Color accent, string glyph, VaultMovementType type)
    {
        var btn = new Guna2Button
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 2, 4, 2),
            BorderRadius = 10,
            FillColor = InvoiceTheme.InputFill,
            BorderColor = Color.FromArgb(70, InvoiceTheme.Gold),
            BorderThickness = 1,
            Cursor = Cursors.Hand,
            Font = InvoiceTheme.SmallFont,
            ForeColor = InvoiceTheme.White,
            Text = text,
            TextAlign = HorizontalAlignment.Center,
            Image = GlyphHelper.Create(glyph, accent, 18),
            ImageAlign = HorizontalAlignment.Center,
            ImageSize = new Size(22, 22),
            HoverState =
            {
                FillColor = Color.FromArgb(30, InvoiceTheme.Gold),
                BorderColor = InvoiceTheme.Gold,
                ForeColor = InvoiceTheme.White
            }
        };
        // Stack image above text via Text offset
        btn.TextOffset = new Point(0, 12);
        btn.ImageOffset = new Point(0, -10);
        btn.Click += (_, _) => OpenMovement(type);
        return btn;
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
            Margin = new Padding(0, 8, 8, 0),
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

    private static string FormatMoney(decimal value) => $"{value:N2} ج.م";

    private void UpdateClock()
    {
        _lblDate.Text = DateTime.Now.ToString("dd MMM yyyy");
    }

    private static void EnableDoubleBuffering(DataGridView grid)
    {
        typeof(DataGridView)
            .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(grid, true);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _clockTimer.Stop();
        _clockTimer.Dispose();
        base.OnFormClosed(e);
    }
}
