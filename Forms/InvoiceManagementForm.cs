using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

internal sealed class InvoiceManagementForm : Form
{
    private readonly System.Windows.Forms.Timer _clockTimer = new() { Interval = 1000 };
    private readonly bool _embedded;
    private Label _lblTime = null!;
    private Label _lblDate = null!;
    private Label _lblCount = null!;
    private Label _lblTotalSales = null!;
    private Label _lblPage = null!;
    private Guna2TextBox _txtSearch = null!;
    private DataGridView _grid = null!;
    private Bitmap _viewIcon = null!;
    private Bitmap _printIcon = null!;

    private List<InvoiceRecord> _filtered = [];
    private List<PurchaseInvoiceRecord> _filteredPurchases = [];
    private bool _showPurchase;
    private Guna2Button _btnSales = null!;
    private Guna2Button _btnPurchase = null!;
    private Label _lblTotalTitle = null!;
    private int _page;
    private const int PageSize = 16;

    public InvoiceManagementForm(bool embedded = false)
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
        Name = "InvoiceManagementForm";
        RightToLeft = RightToLeft.No;
        ShowIcon = !_embedded;
        ShowInTaskbar = !_embedded;
        StartPosition = FormStartPosition.Manual;
        Text = _embedded ? string.Empty : "إدارة الفواتير - Ibrahim Abdo Auto Service";
        WindowState = FormWindowState.Normal;

        InvoiceStore.Load();
        _viewIcon = GlyphHelper.Create("\uE7B3", InvoiceTheme.Gold, 14);
        _printIcon = GlyphHelper.Create("\uE749", InvoiceTheme.Gold, 14);

        BuildUi();
        RefreshData();

        VisibleChanged += (_, _) =>
        {
            if (Visible)
            {
                ReloadCurrentStore();
                RefreshData();
            }
        };

        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();
        ResumeLayout(true);
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
        main.Controls.Add(BuildContent(), 0, 1);
        Controls.Add(main);
    }

    private Control BuildTopBar()
    {
        var bar = new Panel { Dock = DockStyle.Fill, BackColor = InvoiceTheme.Background };
        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "إدارة الفواتير",
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
        right.Controls.Add(_lblDate);
        right.Controls.Add(CreateChromeIcon("\uE121"));
        right.Controls.Add(_lblTime);

        bar.Controls.Add(title);
        bar.Controls.Add(right);
        return bar;
    }

    private Control BuildContent()
    {
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        host.Controls.Add(BuildToolbar(), 0, 0);
        host.Controls.Add(BuildStats(), 0, 1);
        host.Controls.Add(BuildGridCard(), 0, 2);
        host.Controls.Add(BuildPagination(), 0, 3);
        return host;
    }

    private Control BuildToolbar()
    {
        var bar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0)
        };

        _txtSearch = new Guna2TextBox
        {
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(90, InvoiceTheme.Gold),
            FillColor = InvoiceTheme.InputFill,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.BodyFont,
            PlaceholderText = "بحث برقم الفاتورة / العميل / اللوحة / السيارة",
            PlaceholderForeColor = InvoiceTheme.Muted,
            Size = new Size(560, 40),
            Location = new Point(0, 0),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            IconLeft = GlyphHelper.Create("\uE721", InvoiceTheme.Gold, 16),
            IconLeftSize = new Size(18, 18),
            FocusedState = { BorderColor = InvoiceTheme.Gold }
        };
        _txtSearch.TextChanged += (_, _) => { _page = 0; RefreshData(); };

        _btnSales = CreateToolbarButton("فواتير البيع", true, (_, _) => SetMode(false));
        _btnSales.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        _btnPurchase = CreateToolbarButton("فواتير الشراء", false, (_, _) => SetMode(true));
        _btnPurchase.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        var refreshBtn = CreateToolbarButton("تحديث", false, (_, _) =>
        {
            ReloadCurrentStore();
            RefreshData();
        });
        refreshBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        void LayoutButtons()
        {
            refreshBtn.Top = 0;
            _btnPurchase.Top = 0;
            _btnSales.Top = 0;
            refreshBtn.Left = bar.ClientSize.Width - refreshBtn.Width;
            _btnPurchase.Left = refreshBtn.Left - 8 - _btnPurchase.Width;
            _btnSales.Left = _btnPurchase.Left - 8 - _btnSales.Width;
        }

        bar.Resize += (_, _) => LayoutButtons();

        bar.Controls.Add(_txtSearch);
        bar.Controls.Add(_btnSales);
        bar.Controls.Add(_btnPurchase);
        bar.Controls.Add(refreshBtn);
        LayoutButtons();
        return bar;
    }

    private void SetMode(bool showPurchase)
    {
        _showPurchase = showPurchase;
        _page = 0;
        UpdateModeButtons();
        ReloadCurrentStore();
        RefreshData();
    }

    private void UpdateModeButtons()
    {
        StyleModeButton(_btnSales, !_showPurchase);
        StyleModeButton(_btnPurchase, _showPurchase);
    }

    private static void StyleModeButton(Guna2Button btn, bool active)
    {
        btn.FillColor = active ? InvoiceTheme.Gold : InvoiceTheme.Card;
        btn.ForeColor = active ? Color.Black : InvoiceTheme.White;
        btn.BorderThickness = active ? 0 : 1;
        btn.HoverState.FillColor = active ? InvoiceTheme.GoldDark : Color.FromArgb(30, InvoiceTheme.Gold);
        btn.HoverState.ForeColor = active ? Color.Black : InvoiceTheme.White;
    }

    private void ReloadCurrentStore()
    {
        if (_showPurchase)
        {
            PurchaseInvoiceStore.Load();
        }
        else
        {
            InvoiceStore.Load();
        }
    }

    private Control BuildStats()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 4)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        _lblCount = new Label();
        _lblTotalSales = new Label();
        _lblTotalTitle = new Label();
        row.Controls.Add(CreateStatCard("عدد الفواتير", _lblCount, "\uE8A5"), 0, 0);
        row.Controls.Add(CreateStatCard("إجمالي المبيعات", _lblTotalSales, "\uE9D2", _lblTotalTitle), 1, 0);
        return row;
    }

    private Control BuildGridCard()
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

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = InvoiceTheme.Card,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.Single,
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
            RowTemplate = { Height = 36 },
            Font = InvoiceTheme.SmallFont,
            ForeColor = InvoiceTheme.White,
            RightToLeft = RightToLeft.Yes,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = InvoiceTheme.Card,
                ForeColor = InvoiceTheme.White,
                SelectionBackColor = Color.FromArgb(48, InvoiceTheme.Gold),
                SelectionForeColor = InvoiceTheme.White,
                Padding = new Padding(4, 0, 4, 0),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = InvoiceTheme.RowAlt,
                ForeColor = InvoiceTheme.White,
                SelectionBackColor = Color.FromArgb(48, InvoiceTheme.Gold),
                SelectionForeColor = InvoiceTheme.White,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = InvoiceTheme.Gold,
                Font = InvoiceTheme.TableHeaderFont,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            ColumnHeadersHeight = 36
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNumber", HeaderText = "رقم الفاتورة", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "التاريخ", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "اسم العميل", FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCar", HeaderText = "بيانات السيارة", FillWeight = 22 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal", HeaderText = "الإجمالي", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewImageColumn { Name = "colView", HeaderText = "عرض", FillWeight = 8, Image = _viewIcon, ImageLayout = DataGridViewImageCellLayout.Zoom });
        _grid.Columns.Add(new DataGridViewImageColumn { Name = "colPrint", HeaderText = "طباعة", FillWeight = 8, Image = _printIcon, ImageLayout = DataGridViewImageCellLayout.Zoom });
        _grid.CellClick += OnGridCellClick;
        _grid.CellDoubleClick += OnGridCellDoubleClick;
        EnableDoubleBuffering(_grid);

        card.Controls.Add(_grid);
        return card;
    }

    private Control BuildPagination()
    {
        var bar = PaginationStyle.CreateBar();
        bar.Controls.Add(PaginationStyle.CreateNavButton("السابق", (_, _) =>
        {
            if (_page > 0) { _page--; BindGrid(); }
        }));
        _lblPage = PaginationStyle.CreatePageLabel();
        bar.Controls.Add(_lblPage);
        bar.Controls.Add(PaginationStyle.CreateNavButton("التالي", (_, _) =>
        {
            var total = _showPurchase ? _filteredPurchases.Count : _filtered.Count;
            if ((_page + 1) * PageSize < total) { _page++; BindGrid(); }
        }));
        return bar;
    }

    public void RefreshData()
    {
        if (_showPurchase)
        {
            _filteredPurchases = FilterPurchases(_txtSearch.Text);
            if (_page * PageSize >= Math.Max(1, _filteredPurchases.Count))
            {
                _page = 0;
            }

            _lblCount.Text = _filteredPurchases.Count.ToString();
            _lblTotalSales.Text = $"{_filteredPurchases.Sum(i => i.GrandTotal):N0} ج.م";
        }
        else
        {
            _filtered = InvoiceStore.Search(_txtSearch.Text).ToList();
            if (_page * PageSize >= Math.Max(1, _filtered.Count))
            {
                _page = 0;
            }

            _lblCount.Text = _filtered.Count.ToString();
            _lblTotalSales.Text = $"{_filtered.Sum(i => i.GrandTotal):N0} ج.م";
        }

        ApplyModeToColumns();
        BindGrid();
    }

    private static List<PurchaseInvoiceRecord> FilterPurchases(string query)
    {
        IEnumerable<PurchaseInvoiceRecord> items = PurchaseInvoiceStore.All;
        query = query?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(query))
        {
            items = items.Where(i =>
                i.Number.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                i.SupplierName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                i.Phone.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        return items.OrderByDescending(i => i.CreatedAt).ToList();
    }

    private void ApplyModeToColumns()
    {
        _lblTotalTitle.Text = _showPurchase ? "إجمالي المشتريات" : "إجمالي المبيعات";
        _grid.Columns["colCustomer"].HeaderText = _showPurchase ? "المورد" : "اسم العميل";
        _grid.Columns["colCar"].HeaderText = _showPurchase ? "طريقة الدفع" : "بيانات السيارة";
    }

    private void BindGrid()
    {
        _grid.Rows.Clear();
        if (_showPurchase)
        {
            var purchasePage = _filteredPurchases.Skip(_page * PageSize).Take(PageSize).ToList();
            foreach (var inv in purchasePage)
            {
                var row = _grid.Rows.Add(
                    inv.Number,
                    inv.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    inv.SupplierName,
                    string.IsNullOrWhiteSpace(inv.PaymentMethod) ? "—" : inv.PaymentMethod,
                    $"{inv.GrandTotal:N2} ج.م",
                    _viewIcon,
                    _printIcon);
                _grid.Rows[row].Tag = inv.Id;
            }

            var purchasePages = Math.Max(1, (int)Math.Ceiling(_filteredPurchases.Count / (double)PageSize));
            _lblPage.Text = $"صفحة {_page + 1} من {purchasePages}  •  {_filteredPurchases.Count} فاتورة";
            return;
        }

        var pageItems = _filtered.Skip(_page * PageSize).Take(PageSize).ToList();
        foreach (var inv in pageItems)
        {
            var car = FormatCar(inv);
            var row = _grid.Rows.Add(
                inv.Number,
                inv.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                inv.CustomerName,
                car,
                $"{inv.GrandTotal:N2} ج.م",
                _viewIcon,
                _printIcon);
            _grid.Rows[row].Tag = inv.Id;
        }

        var pages = Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)PageSize));
        _lblPage.Text = $"صفحة {_page + 1} من {pages}  •  {_filtered.Count} فاتورة";
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
        if (_showPurchase)
        {
            var purchase = PurchaseInvoiceStore.All.FirstOrDefault(p => p.Id == id);
            if (purchase is null)
            {
                return;
            }

            if (col == "colView")
            {
                OpenPurchaseDetails(purchase);
            }
            else if (col == "colPrint")
            {
                PrintPurchase(purchase);
            }

            return;
        }

        var invoice = InvoiceStore.Find(id);
        if (invoice is null)
        {
            return;
        }

        if (col == "colView")
        {
            OpenDetails(invoice);
        }
        else if (col == "colPrint")
        {
            Reprint(invoice);
        }
    }

    private void OnGridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0)
        {
            return;
        }

        if (_grid.Rows[e.RowIndex].Tag is not string id)
        {
            return;
        }

        if (_showPurchase)
        {
            var purchase = PurchaseInvoiceStore.All.FirstOrDefault(p => p.Id == id);
            if (purchase is not null)
            {
                OpenPurchaseDetails(purchase);
            }

            return;
        }

        var invoice = InvoiceStore.Find(id);
        if (invoice is not null)
        {
            OpenDetails(invoice);
        }
    }

    private void OpenDetails(InvoiceRecord invoice)
    {
        try
        {
            using var dlg = new InvoiceDetailsDialog(invoice);
            dlg.ShowDialog(FindForm());
        }
        catch (Exception ex)
        {
            AppMessageDialog.Error(this, $"تعذر فتح التفاصيل.\r\n{ex.Message}", "عرض الفاتورة");
        }
    }

    private void OpenPurchaseDetails(PurchaseInvoiceRecord invoice)
    {
        try
        {
            using var dlg = new PurchaseInvoiceDetailsDialog(invoice);
            dlg.ShowDialog(FindForm());
        }
        catch (Exception ex)
        {
            AppMessageDialog.Error(this, $"تعذر فتح التفاصيل.\r\n{ex.Message}", "عرض الفاتورة");
        }
    }

    private void PrintPurchase(PurchaseInvoiceRecord invoice)
    {
        try
        {
            PurchasePdfGenerator.GenerateAndOpen(invoice);
            AppMessageDialog.Success(this, $"تم فتح فاتورة الشراء {invoice.Number} للطباعة.", "طباعة");
        }
        catch (Exception ex)
        {
            AppMessageDialog.Error(this, $"تعذر الطباعة.\r\n{ex.Message}", "طباعة");
        }
    }

    private void Reprint(InvoiceRecord invoice)
    {
        try
        {
            InvoicePdfGenerator.GenerateAndOpen(invoice);
            AppMessageDialog.Success(this, $"تم فتح الفاتورة {invoice.Number} للطباعة.", "إعادة طباعة");
        }
        catch (Exception ex)
        {
            AppMessageDialog.Error(this, $"تعذر إعادة الطباعة.\r\n{ex.Message}", "إعادة طباعة");
        }
    }

    private static string FormatCar(InvoiceRecord inv)
    {
        var plate = $"{inv.PlateLetters} {inv.PlateNumber}".Trim();
        if (string.IsNullOrWhiteSpace(plate) && string.IsNullOrWhiteSpace(inv.CarModel))
        {
            return "—";
        }

        if (string.IsNullOrWhiteSpace(plate))
        {
            return inv.CarModel;
        }

        if (string.IsNullOrWhiteSpace(inv.CarModel))
        {
            return plate;
        }

        return $"{inv.CarModel} • {plate}";
    }

    private static Control CreateStatCard(string title, Label valueLabel, string glyph, Label? titleLabel = null)
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
        var titleLbl = titleLabel ?? new Label();
        titleLbl.Dock = DockStyle.Top;
        titleLbl.Height = 20;
        titleLbl.Text = title;
        titleLbl.ForeColor = InvoiceTheme.Muted;
        titleLbl.Font = InvoiceTheme.SmallFont;
        titleLbl.TextAlign = ContentAlignment.MiddleLeft;
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
        _viewIcon.Dispose();
        _printIcon.Dispose();
        base.OnFormClosed(e);
    }
}
