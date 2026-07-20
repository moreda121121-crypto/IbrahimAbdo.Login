using System.Globalization;
using Guna.UI2.WinForms;
using Guna.UI2.WinForms.Enums;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

internal sealed class SalesInvoiceForm : Form
{
    private readonly System.Windows.Forms.Timer _clockTimer = new() { Interval = 1000 };
    private Label _lblTime = null!;
    private Label _lblDate = null!;
    private DataGridView _grid = null!;
    private Label _lblSubtotal = null!;
    private Label _lblTax = null!;
    private Label _lblGrandTotal = null!;
    private Label _lblRemaining = null!;
    private Guna2TextBox _txtDiscount = null!;
    private Guna2TextBox _txtPaid = null!;
    private Guna2ComboBox _cmbDiscountUnit = null!;
    private Bitmap _editIcon = null!;
    private Bitmap _deleteIcon = null!;
    private Size? _matchLoginSize;
    private readonly bool _embedded;

    private Guna2ComboBox _cmbCustomer = null!;
    private Guna2TextBox _txtPhone = null!;
    private Guna2TextBox _txtAddress = null!;
    private Guna2TextBox _txtPlateLetters = null!;
    private Guna2TextBox _txtPlateNumber = null!;
    private Guna2TextBox _txtCarModel = null!;
    private Guna2ComboBox _cmbCarModel = null!;
    private Guna2TextBox _txtOdometer = null!;
    private Guna2ComboBox _cmbTechnician = null!;
    private Guna2ComboBox _cmbPayment = null!;
    private CustomerRecord? _selectedCustomer;
    private bool _suppressVehicleSync;

    private readonly List<InvoiceLine> _lines = [];

    public SalesInvoiceForm(bool embedded = false)
    {
        _embedded = embedded;
        SuspendLayout();
        // Match LoginForm scale + chrome so outer Size matches exactly
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
        Name = "SalesInvoiceForm";
        // Shell layout stays LTR so Dock.Left/Right match the reference.
        // Arabic RTL is applied only on text-bearing controls.
        RightToLeft = RightToLeft.No;
        RightToLeftLayout = false;
        ShowIcon = !_embedded;
        ShowInTaskbar = !_embedded;
        StartPosition = FormStartPosition.Manual;
        Text = _embedded ? string.Empty : "فاتورة بيع - Ibrahim Cherokee Auto Service";
        WindowState = FormWindowState.Normal;

        CustomerStore.Load();
        InvoiceStore.Load();
        ProductStore.Load();
        TechnicianStore.Load();

        BuildUi();
        if (!_embedded)
        {
            WireWindowChrome();
            Shown += (_, _) => PlaceCenteredOnScreen();
        }

        LoadGrid();
        RecalculateTotals();

        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();
        ResumeLayout(true);
    }

    /// <summary>Copy exact outer Size from the login window before ShowDialog.</summary>
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
        var main = BuildMainArea();
        main.Dock = DockStyle.Fill;
        main.RightToLeft = RightToLeft.No;

        // Embedded inside MainShellForm: content only (sidebar lives on the shell).
        if (_embedded)
        {
            Controls.Add(main);
            return;
        }

        // Standalone fallback: sidebar LEFT, content RIGHT
        var shell = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = InvoiceTheme.Background,
            RightToLeft = RightToLeft.No
        };

        var sidebar = BuildSidebar();
        sidebar.Dock = DockStyle.Left;
        sidebar.Width = InvoiceTheme.SidebarWidth;
        sidebar.RightToLeft = RightToLeft.No;

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
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 0,
            CustomBorderColor = InvoiceTheme.CardBorder,
            CustomBorderThickness = new Padding(0, 0, 1, 0),
            Padding = new Padding(12, 12, 12, 16)
        };

        // Larger brand logo — top of sidebar
        var logo = new Panel
        {
            Dock = DockStyle.Top,
            Height = 220,
            BackColor = Color.Transparent,
            Padding = new Padding(2, 0, 2, 6)
        };

        var logoImage = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
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
            Padding = new Padding(0, 4, 0, 0),
            RightToLeft = RightToLeft.No
        };

        // glyph, label — matches reference order & style
        string[][] items =
        [
            ["\uE77B", "العملاء", "customers"],
            ["\uE80F", "إدارة الفواتير", "dashboard"],
            ["\uE81E", "الأصناف", "items"],
            ["\uE90F", "الخدمات", "services"],
            ["\uE8A5", "فاتورة البيع", "invoice"],
            ["\uE8CB", "فاتورة شراء", "purchase"],
            ["\uE8F1", "المخزون", "inventory"],
            ["\uE718", "الفنيين", "techs"],
            ["\uE716", "المستخدمون", "users"],
        ];

        foreach (var item in items)
        {
            menuHost.Controls.Add(CreateMenuItem(item[0], item[1], item[2], item[2] == "invoice"));
        }

        sidebar.Controls.Add(menuHost);
        sidebar.Controls.Add(logo);
        return sidebar;
    }

    private Control CreateMenuItem(string glyph, string text, string key, bool active)
    {
        // Arabic text on the right, gold icon on the LEFT (الشمال)
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

        // ~1.2cm gap between icon and text (0.2 + 1cm @ 96 DPI)
        const int gapPx = 45;

        var icon = new PictureBox
        {
            Dock = DockStyle.Left,
            Width = 36,
            SizeMode = PictureBoxSizeMode.CenterImage,
            BackColor = Color.Transparent,
            Image = CreateGlyphBitmap(glyph, InvoiceTheme.Gold, iconPx),
            Margin = Padding.Empty,
            Padding = new Padding(8, 0, 0, 0)
        };

        var label = new Label
        {
            Dock = DockStyle.Fill,
            Text = text,
            Font = InvoiceTheme.MenuFont,
            ForeColor = active ? InvoiceTheme.Gold : Color.FromArgb(210, 210, 210),
            TextAlign = ContentAlignment.MiddleLeft,
            RightToLeft = RightToLeft.No,
            BackColor = Color.Transparent,
            AutoEllipsis = false,
            Padding = new Padding(gapPx, 0, 8, 0)
        };

        void SetHover(bool on)
        {
            if (active)
            {
                return;
            }

            row.FillColor = on ? Color.FromArgb(36, InvoiceTheme.Gold) : Color.Transparent;
            label.ForeColor = on ? InvoiceTheme.Gold : Color.FromArgb(210, 210, 210);
        }

        void OnNavigate(object? s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (key == "customers")
            {
                OpenCustomersPage();
            }
            else if (key != "invoice")
            {
                MessageBox.Show(this, $"صفحة «{text}» قريباً.", "قريباً", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Tag so window-drag chrome won't steal clicks from menu items
        row.Tag = "nav";
        icon.Tag = "nav";
        label.Tag = "nav";

        row.MouseEnter += (_, _) => SetHover(true);
        row.MouseLeave += (_, _) => SetHover(false);
        label.MouseEnter += (_, _) => SetHover(true);
        label.MouseLeave += (_, _) => SetHover(false);
        icon.MouseEnter += (_, _) => SetHover(true);
        icon.MouseLeave += (_, _) => SetHover(false);
        // MouseUp is more reliable than Click when drag chrome is attached
        row.MouseUp += OnNavigate;
        label.MouseUp += OnNavigate;
        icon.MouseUp += OnNavigate;

        // Add icon first so Dock.Left stays on the left edge
        row.Controls.Add(icon);
        row.Controls.Add(label);
        return row;
    }

    private void OpenCustomersPage()
    {
        var bounds = Bounds;
        using var customers = new CustomersForm();
        customers.StartPosition = FormStartPosition.Manual;
        customers.Bounds = bounds;
        customers.MinimumSize = MinimumSize;
        customers.MaximumSize = MaximumSize;
        // Do not pass a hidden owner — ShowDialog(owner) fails when owner is Hide()'d
        Hide();
        customers.ShowDialog();
        Show();
        Activate();
        BringToFront();
    }

    private Control BuildMainArea()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = InvoiceTheme.Background,
            Padding = new Padding(14, 8, 14, 10),
            RightToLeft = RightToLeft.No
        };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 252));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

        main.Controls.Add(BuildTopBar(), 0, 0);
        main.Controls.Add(BuildTopCards(), 0, 1);
        main.Controls.Add(BuildCenterSection(), 0, 2);
        main.Controls.Add(BuildBottomActions(), 0, 3);
        return main;
    }

    private Control BuildTopBar()
    {
        var bar = new Panel { Dock = DockStyle.Fill, BackColor = InvoiceTheme.Background, RightToLeft = RightToLeft.No };

        var title = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font(InvoiceTheme.Family.FontFamily, 20F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = InvoiceTheme.White,
            Text = "فاتورة بيع",
            TextAlign = ContentAlignment.MiddleCenter,
            RightToLeft = RightToLeft.No,
            AutoEllipsis = false,
            UseCompatibleTextRendering = false,
            Padding = new Padding(0, 4, 0, 4)
        };

        var right = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 12, 0, 0),
            RightToLeft = RightToLeft.No
        };

        var bell = CreateChromeIconButton("\uE7E7");
        var badge = new Label
        {
            AutoSize = false,
            Size = new Size(16, 16),
            Text = "3",
            Font = new Font(InvoiceTheme.Family.FontFamily, 7F, FontStyle.Bold),
            ForeColor = Color.Black,
            BackColor = InvoiceTheme.Gold,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(-18, 0, 12, 0)
        };

        _lblDate = new Label
        {
            AutoSize = true,
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont,
            Text = "08 / 04 / 2026",
            Margin = new Padding(8, 8, 12, 0)
        };
        _lblTime = new Label
        {
            AutoSize = true,
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont,
            Text = "12:30 PM",
            Margin = new Padding(8, 8, 16, 0)
        };

        right.Controls.Add(bell);
        right.Controls.Add(badge);
        right.Controls.Add(CreateChromeIconButton("\uE787"));
        right.Controls.Add(_lblDate);
        right.Controls.Add(CreateChromeIconButton("\uE121"));
        right.Controls.Add(_lblTime);

        // Fill title first, then right chrome on top so icons stay visible
        bar.Controls.Add(title);
        bar.Controls.Add(right);
        return bar;
    }

    private Control BuildTopCards()
    {
        // LTR columns: Customer | Vehicle | Technician (exact reference order)
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 4),
            RightToLeft = RightToLeft.No
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27F));

        row.Controls.Add(BuildCustomerCard(), 0, 0);
        row.Controls.Add(BuildVehicleCard(), 1, 0);
        row.Controls.Add(BuildTechnicianCard(), 2, 0);
        return row;
    }

    private Control BuildCustomerCard()
    {
        var (card, body) = CreateCard("بيانات العميل");

        _cmbCustomer = CreateSearchableCustomerCombo();
        _txtPhone = CreateInput("");
        _txtPhone.PlaceholderText = "رقم الهاتف";
        _txtPhone.ReadOnly = true;
        _txtAddress = CreateInput("");
        _txtAddress.PlaceholderText = "العنوان";
        _txtAddress.ReadOnly = true;

        body.Controls.Add(CreateField("اسم العميل", _cmbCustomer), 0, 0);
        body.Controls.Add(CreateField("رقم الهاتف", _txtPhone), 0, 1);
        body.Controls.Add(CreateField("العنوان", _txtAddress), 0, 2);
        return card;
    }

    private Control BuildVehicleCard()
    {
        var (card, body) = CreateCard("بيانات السيارة");

        var plateRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            RightToLeft = RightToLeft.No
        };
        plateRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        plateRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        _txtPlateLetters = CreateInput("");
        _txtPlateLetters.PlaceholderText = "حروف اللوحة";
        _txtPlateLetters.ReadOnly = true;

        _txtPlateNumber = CreateInput("");
        _txtPlateNumber.PlaceholderText = "رقم اللوحة";
        _txtPlateNumber.ReadOnly = true;

        _txtCarModel = CreateInput("");
        _txtCarModel.PlaceholderText = "الماركة / الموديل";
        _txtCarModel.ReadOnly = true;
        _txtCarModel.Dock = DockStyle.Fill;

        _cmbCarModel = CreateCombo();
        _cmbCarModel.AutoCompleteSource = AutoCompleteSource.ListItems;
        _cmbCarModel.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _cmbCarModel.Dock = DockStyle.Fill;
        _cmbCarModel.Visible = false;
        _cmbCarModel.SelectedIndexChanged += (_, _) =>
        {
            if (!_suppressVehicleSync)
            {
                ApplyCarAtIndex(_cmbCarModel.SelectedIndex);
            }
        };

        var modelHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        modelHost.Controls.Add(_cmbCarModel);
        modelHost.Controls.Add(_txtCarModel);
        modelHost.Resize += (_, _) =>
        {
            _txtCarModel.SetBounds(0, 0, modelHost.ClientSize.Width, 34);
            _cmbCarModel.SetBounds(0, 0, modelHost.ClientSize.Width, 34);
        };

        _txtOdometer = CreateInput("");
        _txtOdometer.PlaceholderText = "قراءة العداد";
        _txtOdometer.ReadOnly = true;

        plateRow.Controls.Add(CreateField("حروف اللوحة", _txtPlateLetters), 0, 0);
        plateRow.Controls.Add(CreateField("رقم اللوحة", _txtPlateNumber), 1, 0);

        body.Controls.Add(plateRow, 0, 0);
        body.Controls.Add(CreateField("الماركة / الموديل", modelHost), 0, 1);
        body.Controls.Add(CreateField("قراءة العداد", _txtOdometer), 0, 2);
        return card;
    }

    private Guna2ComboBox CreateSearchableCustomerCombo()
    {
        var combo = CreateCombo();
        var names = CustomerStore.All.Select(c => c.Name).Distinct().ToArray();
        combo.Items.AddRange(names);

        // Guna combo is DropDownList — only ListItems autocomplete is allowed.
        combo.AutoCompleteSource = AutoCompleteSource.ListItems;
        combo.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        combo.SelectedIndex = -1;

        combo.SelectedIndexChanged += (_, _) => ApplySelectedCustomer();
        combo.TextChanged += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(combo.Text))
            {
                ClearCustomerAndVehicle();
            }
        };
        combo.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                TryApplyCustomerByTypedText();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        };

        return combo;
    }

    private void TryApplyCustomerByTypedText()
    {
        var typed = _cmbCustomer.Text.Trim();
        if (string.IsNullOrWhiteSpace(typed))
        {
            ClearCustomerAndVehicle();
            return;
        }

        var match = CustomerStore.All.FirstOrDefault(c =>
            c.Name.Equals(typed, StringComparison.OrdinalIgnoreCase) ||
            c.Name.Contains(typed, StringComparison.OrdinalIgnoreCase) ||
            c.Phone.Contains(typed));

        if (match is null)
        {
            return;
        }

        var index = _cmbCustomer.Items.IndexOf(match.Name);
        if (index >= 0 && _cmbCustomer.SelectedIndex != index)
        {
            _cmbCustomer.SelectedIndex = index;
        }
        else
        {
            ApplyCustomer(match);
        }
    }

    private void ApplySelectedCustomer()
    {
        if (_cmbCustomer.SelectedItem is not string name)
        {
            return;
        }

        var customer = CustomerStore.All.FirstOrDefault(c => c.Name == name);
        if (customer is not null)
        {
            ApplyCustomer(customer);
        }
    }

    private void ApplyCustomer(CustomerRecord customer)
    {
        _selectedCustomer = customer;
        _txtPhone.Text = customer.Phone;
        _txtAddress.Text = string.IsNullOrWhiteSpace(customer.Address) ? "—" : customer.Address;

        _cmbCarModel.Items.Clear();
        _txtCarModel.Text = "";
        _txtPlateLetters.Text = "";
        _txtPlateNumber.Text = "";
        _txtOdometer.Text = "";

        if (customer.Cars.Count == 0)
        {
            SetCarModelMode(multiCar: false);
            return;
        }

        foreach (var car in customer.Cars)
        {
            _cmbCarModel.Items.Add($"{car.Brand} - {car.Model} {car.Year}".Trim());
        }

        // Single car: black text field. Multiple cars: searchable combo.
        SetCarModelMode(multiCar: customer.Cars.Count > 1);
        ApplyCarAtIndex(0);
    }

    private void SetCarModelMode(bool multiCar)
    {
        _cmbCarModel.Visible = multiCar;
        _txtCarModel.Visible = !multiCar;
        if (multiCar)
        {
            _cmbCarModel.BringToFront();
        }
        else
        {
            _txtCarModel.BringToFront();
        }
    }

    private void ApplyCarAtIndex(int index)
    {
        if (_selectedCustomer is null || index < 0 || index >= _selectedCustomer.Cars.Count)
        {
            return;
        }

        var car = _selectedCustomer.Cars[index];
        var (letters, number) = SplitPlate(car.PlateNumber);
        var modelText = $"{car.Brand} - {car.Model} {car.Year}".Trim();

        _suppressVehicleSync = true;
        try
        {
            if (_cmbCarModel.Items.Count > index && _cmbCarModel.SelectedIndex != index)
            {
                _cmbCarModel.SelectedIndex = index;
            }

            _txtCarModel.Text = modelText;
            _txtPlateLetters.Text = letters;
            _txtPlateNumber.Text = string.IsNullOrWhiteSpace(number) ? car.PlateNumber : number;
            _txtOdometer.Text = car.Mileage > 0 ? $"{car.Mileage:N0} km" : "";
        }
        finally
        {
            _suppressVehicleSync = false;
        }
    }

    private void ClearCustomerAndVehicle()
    {
        _selectedCustomer = null;
        _txtPhone.Text = "";
        _txtAddress.Text = "";
        _txtPlateLetters.Text = "";
        _txtPlateNumber.Text = "";
        _txtCarModel.Text = "";
        _txtOdometer.Text = "";
        _cmbCarModel.Items.Clear();
        SetCarModelMode(multiCar: false);
    }

    private static (string Letters, string Number) SplitPlate(string plate)
    {
        var parts = plate.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return ("", "");
        }

        var last = parts[^1];
        if (last.All(char.IsDigit))
        {
            return (string.Join(" ", parts[..^1]), last);
        }

        return (plate, "");
    }

    private Control BuildTechnicianCard()
    {
        var (card, body) = CreateCard("الفني المسؤول");
        _cmbTechnician = CreateCombo(TechnicianStore.Names().ToArray());
        _cmbTechnician.SelectedIndex = -1;
        _cmbPayment = CreateCombo("نقدي", "انستا باي", "فيزا");
        _cmbPayment.SelectedIndex = -1;
        body.Controls.Add(CreateField("اختر الفني", _cmbTechnician), 0, 0);
        body.Controls.Add(CreateField("طريقة الدفع", _cmbPayment), 0, 1);
        return card;
    }

    /// <summary>Refresh technician dropdown after technicians are added/removed.</summary>
    public void ReloadTechnicians()
    {
        TechnicianStore.Load();
        if (_cmbTechnician is null)
        {
            return;
        }

        var selected = _cmbTechnician.Text;
        _cmbTechnician.Items.Clear();
        foreach (var name in TechnicianStore.Names())
        {
            _cmbTechnician.Items.Add(name);
        }

        if (!string.IsNullOrWhiteSpace(selected))
        {
            var idx = _cmbTechnician.Items.IndexOf(selected);
            _cmbTechnician.SelectedIndex = idx >= 0 ? idx : -1;
            if (idx < 0)
            {
                _cmbTechnician.Text = selected;
            }
        }
        else
        {
            _cmbTechnician.SelectedIndex = -1;
        }
    }

    private static (Guna2Panel Card, TableLayoutPanel Body) CreateCard(string title)
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
            ShadowDecoration =
            {
                Enabled = true,
                Depth = 10,
                Color = Color.Black,
                BorderRadius = InvoiceTheme.Radius
            }
        };

        var header = new Panel { Dock = DockStyle.Top, Height = 24, BackColor = Color.Transparent };
        var lbl = new Label
        {
            Dock = DockStyle.Fill,
            Text = title,
            Font = InvoiceTheme.SectionFont,
            ForeColor = InvoiceTheme.Gold,
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.Yes,
            AutoEllipsis = false,
            AutoSize = false
        };
        header.Controls.Add(lbl);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0),
            Margin = Padding.Empty,
            RightToLeft = RightToLeft.No
        };
        // label(18) + gap(2) + box(34) + spacing ≈ 64 per row
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));

        card.Controls.Add(body);
        card.Controls.Add(header);
        return (card, body);
    }

    private static Control CreateField(string label, Control input)
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = new Padding(2, 0, 2, 0)
        };

        var lbl = new Label
        {
            Location = new Point(0, 0),
            Size = new Size(10, 18),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = label,
            Font = InvoiceTheme.SmallFont,
            ForeColor = InvoiceTheme.Muted,
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.Yes,
            AutoEllipsis = false,
            AutoSize = false
        };

        input.Location = new Point(0, 20);
        input.Size = new Size(10, 34);
        input.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        input.Height = 34;

        host.Resize += (_, _) =>
        {
            lbl.Width = host.ClientSize.Width;
            input.Width = host.ClientSize.Width;
        };

        host.Controls.Add(input);
        host.Controls.Add(lbl);
        return host;
    }

    private static Guna2TextBox CreateInput(string text)
    {
        return new Guna2TextBox
        {
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(90, InvoiceTheme.Gold),
            FillColor = InvoiceTheme.InputFill,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.BodyFont,
            Text = text,
            Size = new Size(200, 34),
            Height = 34,
            PlaceholderForeColor = InvoiceTheme.Muted,
            FocusedState = { BorderColor = InvoiceTheme.Gold },
            HoverState = { BorderColor = InvoiceTheme.Gold },
            RightToLeft = RightToLeft.Yes,
            TextAlign = HorizontalAlignment.Right
        };
    }

    private static Guna2ComboBox CreateCombo(params string[] items)
    {
        var combo = new Guna2ComboBox
        {
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(90, InvoiceTheme.Gold),
            FillColor = InvoiceTheme.InputFill,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.BodyFont,
            Size = new Size(200, 34),
            Height = 34,
            FocusedColor = InvoiceTheme.Gold,
            HoverState = { BorderColor = InvoiceTheme.Gold },
            ItemsAppearance = { BackColor = InvoiceTheme.Card, ForeColor = InvoiceTheme.White },
            RightToLeft = RightToLeft.Yes
        };
        if (items.Length > 0)
        {
            combo.Items.AddRange(items);
            combo.SelectedIndex = 0;
        }

        return combo;
    }

    private Control BuildCenterSection()
    {
        // LTR: table CENTER-LEFT, summary RIGHT (matches reference)
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            RightToLeft = RightToLeft.No
        };
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));

        host.Controls.Add(BuildTablePanel(), 0, 0);
        host.Controls.Add(BuildSummaryPanel(), 1, 0);
        return host;
    }

    private Control BuildTablePanel()
    {
        var card = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Margin = new Padding(6),
            Padding = new Padding(10),
            ShadowDecoration = { Enabled = true, Depth = 10, Color = Color.Black, BorderRadius = InvoiceTheme.Radius }
        };

        _grid = new DataGridView
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
            EditMode = DataGridViewEditMode.EditProgrammatically,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 36 },
            Font = InvoiceTheme.BodyFont,
            GridColor = Color.FromArgb(45, 45, 45),
            RightToLeft = RightToLeft.Yes,
            Cursor = Cursors.Hand,
            // Prevents first-row ghosting / paint lag
            AutoGenerateColumns = false
        };
        EnableDoubleBuffering(_grid);

        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = InvoiceTheme.Gold,
            ForeColor = Color.Black,
            Font = InvoiceTheme.TableHeaderFont,
            Alignment = DataGridViewContentAlignment.MiddleCenter,
            SelectionBackColor = InvoiceTheme.Gold,
            SelectionForeColor = Color.Black,
            WrapMode = DataGridViewTriState.False
        };
        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = InvoiceTheme.Card,
            ForeColor = InvoiceTheme.White,
            SelectionBackColor = Color.FromArgb(50, InvoiceTheme.Gold),
            SelectionForeColor = InvoiceTheme.White,
            Alignment = DataGridViewContentAlignment.MiddleCenter,
            WrapMode = DataGridViewTriState.False
        };
        _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = InvoiceTheme.RowAlt,
            ForeColor = InvoiceTheme.White,
            SelectionBackColor = Color.FromArgb(50, InvoiceTheme.Gold),
            SelectionForeColor = InvoiceTheme.White,
            Alignment = DataGridViewContentAlignment.MiddleCenter,
            WrapMode = DataGridViewTriState.False
        };
        _grid.ColumnHeadersHeight = 38;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        _editIcon = CreateGlyphBitmap("\uE70F", InvoiceTheme.Gold, 16);
        _deleteIcon = CreateGlyphBitmap("\uE74D", InvoiceTheme.Danger, 16);

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIndex", HeaderText = "#", FillWeight = 8, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItem", HeaderText = "الصنف / الخدمة", FillWeight = 32, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty", HeaderText = "الكمية", FillWeight = 12, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice", HeaderText = "سعر الوحدة", FillWeight = 16, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal", HeaderText = "الإجمالي", FillWeight = 16, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewImageColumn
        {
            Name = "colEdit",
            HeaderText = "إجراء",
            FillWeight = 8,
            ReadOnly = true,
            Image = _editIcon,
            ValuesAreIcons = false,
            ImageLayout = DataGridViewImageCellLayout.Normal,
            DefaultCellStyle =
            {
                NullValue = _editIcon,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Padding = new Padding(8)
            }
        });
        _grid.Columns.Add(new DataGridViewImageColumn
        {
            Name = "colDelete",
            HeaderText = "",
            FillWeight = 8,
            ReadOnly = true,
            Image = _deleteIcon,
            ValuesAreIcons = false,
            ImageLayout = DataGridViewImageCellLayout.Normal,
            DefaultCellStyle =
            {
                NullValue = _deleteIcon,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Padding = new Padding(8)
            }
        });

        _grid.CellClick += OnGridActionClick;

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 6, 0, 0),
            RightToLeft = RightToLeft.No
        };
        actions.Controls.Add(CreateOutlineButton("حذف الكل", (_, _) =>
        {
            _lines.Clear();
            LoadGrid();
            RecalculateTotals();
        }, InvoiceTheme.Danger));
        actions.Controls.Add(CreateOutlineButton("+ إضافة صنف", (_, _) => AddLine()));

        // Dock order: bottom bar first, then fill grid
        card.Controls.Add(actions);
        card.Controls.Add(_grid);
        return card;
    }

    private Control BuildSummaryPanel()
    {
        var card = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Margin = new Padding(4),
            Padding = new Padding(18, 14, 18, 14),
            ShadowDecoration = { Enabled = true, Depth = 10, Color = Color.Black, BorderRadius = InvoiceTheme.Radius }
        };

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            Text = "ملخص الفاتورة",
            Font = InvoiceTheme.SectionFont,
            ForeColor = InvoiceTheme.Gold,
            TextAlign = ContentAlignment.MiddleCenter,
            RightToLeft = RightToLeft.No,
            AutoSize = false
        };

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0),
            RightToLeft = RightToLeft.No
        };
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        _lblSubtotal = CreateSummaryValue("0.00", InvoiceTheme.White, InvoiceTheme.BodyFont);
        _lblTax = CreateSummaryValue("0.00", InvoiceTheme.White, InvoiceTheme.BodyFont);
        _lblGrandTotal = CreateSummaryValue("0.00", InvoiceTheme.Gold, InvoiceTheme.TotalFont);
        _lblRemaining = CreateSummaryValue("0.00", InvoiceTheme.White, InvoiceTheme.BodyFont);

        _cmbDiscountUnit = new Guna2ComboBox
        {
            Dock = DockStyle.Fill,
            BorderRadius = 6,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(120, 120, 120),
            FillColor = InvoiceTheme.InputFill,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.BodyFont,
            Height = 28,
            FocusedColor = InvoiceTheme.Gold,
            HoverState = { BorderColor = InvoiceTheme.Gold },
            ItemsAppearance = { BackColor = InvoiceTheme.Card, ForeColor = InvoiceTheme.White },
            RightToLeft = RightToLeft.No,
            Margin = new Padding(0, 4, 6, 4)
        };
        _cmbDiscountUnit.Items.AddRange("%", "ج.م");
        _cmbDiscountUnit.SelectedIndex = 0;
        _cmbDiscountUnit.SelectedIndexChanged += (_, _) => RecalculateTotals();

        _txtDiscount = new Guna2TextBox
        {
            Dock = DockStyle.Fill,
            Text = "0.00",
            BorderThickness = 0,
            BorderRadius = 0,
            FillColor = InvoiceTheme.Card,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.BodyFont,
            TextAlign = HorizontalAlignment.Right,
            RightToLeft = RightToLeft.No,
            Margin = new Padding(0, 2, 0, 2),
            FocusedState = { BorderColor = InvoiceTheme.Card, FillColor = InvoiceTheme.Card },
            HoverState = { BorderColor = InvoiceTheme.Card, FillColor = InvoiceTheme.Card }
        };
        _txtDiscount.TextChanged += (_, _) => RecalculateTotals();

        _txtPaid = CreateSummaryInput("0.00");
        _txtPaid.KeyPress += OnPaidKeyPress;
        _txtPaid.TextChanged += (_, _) => RecalculateTotals();

        var separator = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        separator.Paint += (_, e) =>
        {
            using var pen = new Pen(InvoiceTheme.Gold, 1F);
            var y = Math.Max(0, separator.Height / 2);
            e.Graphics.DrawLine(pen, 0, y, separator.Width, y);
        };

        stack.Controls.Add(CreateSummaryRow("الإجمالي الفرعي", _lblSubtotal, InvoiceTheme.White), 0, 0);
        stack.Controls.Add(CreateDiscountSummaryRow(), 0, 1);
        stack.Controls.Add(CreateSummaryRow("الضريبة (0%)", _lblTax, InvoiceTheme.White), 0, 2);
        stack.Controls.Add(separator, 0, 3);
        stack.Controls.Add(CreateSummaryRow("المبلغ الإجمالي", _lblGrandTotal, InvoiceTheme.Gold), 0, 4);
        stack.Controls.Add(CreateSummaryRow("المدفوع", _txtPaid, InvoiceTheme.White), 0, 5);
        stack.Controls.Add(CreateSummaryRow("المتبقي", _lblRemaining, InvoiceTheme.White), 0, 6);

        card.Controls.Add(stack);
        card.Controls.Add(title);
        return card;
    }

    private static Label CreateSummaryValue(string text, Color color, Font font) =>
        new()
        {
            Dock = DockStyle.Fill,
            Text = text,
            Font = font,
            ForeColor = color,
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.No,
            AutoSize = false,
            Margin = Padding.Empty
        };

    private static Guna2TextBox CreateSummaryInput(string text) =>
        new()
        {
            Dock = DockStyle.Fill,
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(150, 150, 150),
            FillColor = InvoiceTheme.InputFill,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.BodyFont,
            Text = text,
            TextAlign = HorizontalAlignment.Right,
            RightToLeft = RightToLeft.No,
            Margin = new Padding(0, 4, 0, 4),
            FocusedState = { BorderColor = InvoiceTheme.Gold },
            HoverState = { BorderColor = Color.FromArgb(180, 180, 180) }
        };

    private static Label CreateSummaryLabel(string text, Color color) =>
        new()
        {
            Dock = DockStyle.Fill,
            Text = text,
            Font = InvoiceTheme.BodyFont,
            ForeColor = color,
            TextAlign = ContentAlignment.MiddleLeft,
            RightToLeft = RightToLeft.No,
            AutoSize = false,
            AutoEllipsis = false,
            Margin = Padding.Empty
        };

    // Arabic LEFT — value RIGHT
    private static Control CreateSummaryRow(string label, Control value, Color labelColor)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            RightToLeft = RightToLeft.No,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

        row.Controls.Add(CreateSummaryLabel(label, labelColor), 0, 0);
        value.Dock = DockStyle.Fill;
        row.Controls.Add(value, 1, 0);
        return row;
    }

    // الخصم LEFT | % dropdown | 0.00 RIGHT
    private Control CreateDiscountSummaryRow()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            RightToLeft = RightToLeft.No,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

        row.Controls.Add(CreateSummaryLabel("الخصم", InvoiceTheme.White), 0, 0);
        row.Controls.Add(_cmbDiscountUnit, 1, 0);
        row.Controls.Add(_txtDiscount, 2, 0);
        return row;
    }

    private Control BuildBottomActions()
    {
        // Fixed 3-column bottom bar: جديد | حفظ الفاتورة | طباعة
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Padding = new Padding(4, 6, 4, 0),
            RightToLeft = RightToLeft.No
        };
        for (var i = 0; i < 3; i++)
        {
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        }

        bar.Controls.Add(WrapBottomButton(CreateBottomAction("جديد", "\uE710", (_, _) => ResetInvoice())), 0, 0);

        bar.Controls.Add(WrapBottomButton(CreateBottomAction("حفظ الفاتورة", "\uE74E", (_, _) =>
        {
            if (SaveInvoice(showSuccess: true) is not null)
            {
                // saved
            }
        })), 1, 0);

        bar.Controls.Add(WrapBottomButton(CreateBottomAction("طباعة", "\uE749", (_, _) => PrintInvoice())), 2, 0);

        return bar;
    }

    private static Control WrapBottomButton(Control button)
    {
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(5, 0, 5, 0);
        return button;
    }

    private Guna2Button CreateBottomAction(string text, string glyph, EventHandler onClick)
    {
        var iconNormal = CreateGlyphBitmap(glyph, InvoiceTheme.White, 20);
        var iconHover = CreateGlyphBitmap(glyph, Color.Black, 20);
        var btn = new Guna2Button
        {
            Text = "  " + text,
            Font = new Font(InvoiceTheme.Family.FontFamily, 12F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = InvoiceTheme.White,
            FillColor = InvoiceTheme.Card,
            BorderColor = Color.FromArgb(70, 70, 70),
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Height = 46,
            MinimumSize = new Size(140, 46),
            Cursor = Cursors.Hand,
            Animated = true,
            Image = iconNormal,
            ImageSize = new Size(20, 20),
            ImageAlign = HorizontalAlignment.Left,
            TextAlign = HorizontalAlignment.Center,
            HoverState =
            {
                FillColor = InvoiceTheme.Gold,
                BorderColor = InvoiceTheme.Gold,
                ForeColor = Color.Black
            },
            PressedColor = InvoiceTheme.GoldDark,
            RightToLeft = RightToLeft.Yes
        };
        btn.MouseEnter += (_, _) => btn.Image = iconHover;
        btn.MouseLeave += (_, _) => btn.Image = iconNormal;
        btn.Click += onClick;
        return btn;
    }

    private static Guna2Button CreateOutlineButton(string text, EventHandler onClick, Color? border = null)
    {
        var color = border ?? InvoiceTheme.Gold;
        var btn = new Guna2Button
        {
            Text = text,
            Font = InvoiceTheme.SmallFont,
            ForeColor = InvoiceTheme.White,
            FillColor = InvoiceTheme.Background,
            BorderColor = color,
            BorderThickness = 1,
            BorderRadius = 8,
            Height = 34,
            Width = 130,
            AutoSize = false,
            Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand,
            HoverState = { FillColor = Color.FromArgb(30, color), BorderColor = color, ForeColor = InvoiceTheme.White },
            RightToLeft = RightToLeft.Yes
        };
        btn.Click += onClick;
        return btn;
    }

    private Guna2Button CreateChromeIconButton(string glyph)
    {
        return new Guna2Button
        {
            Size = new Size(34, 34),
            BorderRadius = 8,
            FillColor = Color.Transparent,
            Font = InvoiceTheme.IconFont,
            Text = glyph,
            ForeColor = InvoiceTheme.Gold,
            Margin = new Padding(4, 0, 4, 0),
            Cursor = Cursors.Hand,
            HoverState = { FillColor = Color.FromArgb(30, InvoiceTheme.Gold), ForeColor = InvoiceTheme.Gold }
        };
    }

    private void WireWindowChrome()
    {
        MouseDown += StartDrag;
        foreach (Control c in Controls)
        {
            AttachDrag(c);
        }
    }

    private void AttachDrag(Control control)
    {
        // Never attach drag to interactive / navigation controls
        if (control is Guna2Button or Guna2TextBox or Guna2ComboBox or DataGridView
            or TextBoxBase or ComboBox or PictureBox)
        {
            foreach (Control child in control.Controls)
            {
                AttachDrag(child);
            }

            return;
        }

        if (control.Tag is "nav")
        {
            return;
        }

        control.MouseDown += StartDrag;
        foreach (Control child in control.Controls)
        {
            AttachDrag(child);
        }
    }

    private void StartDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || WindowState == FormWindowState.Maximized)
        {
            return;
        }

        if (sender is Control { Tag: "nav" })
        {
            return;
        }

        Capture = false;
        _ = ReleaseCapture();
        _ = SendMessage(Handle, 0xA1, 2, 0);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

    private void LoadGrid()
    {
        _grid.SuspendLayout();
        _grid.Rows.Clear();
        var index = 1;
        foreach (var line in _lines)
        {
            _grid.Rows.Add(
                index++,
                line.Name,
                line.Qty.ToString(),
                line.UnitPrice.ToString("N2"),
                line.Total.ToString("N2"),
                _editIcon,
                _deleteIcon);
        }

        _grid.ClearSelection();
        _grid.CurrentCell = null;
        _grid.ResumeLayout();
        _grid.Refresh();
    }

    private void AddLine()
    {
        using var dlg = new SelectProductDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK || dlg.Selected is null)
        {
            return;
        }

        var product = dlg.Selected;
        // Merge qty if same product already on the invoice
        var existing = _lines.FindIndex(l =>
            (!string.IsNullOrEmpty(l.ProductId) && l.ProductId == product.Id) ||
            l.Name.Equals(product.Name, StringComparison.OrdinalIgnoreCase));
        if (existing >= 0)
        {
            var line = _lines[existing];
            if (line.Qty + 1 > product.Quantity)
            {
                AppMessageDialog.Warning(this, $"الكمية المتاحة من «{product.Name}» هي {product.Quantity} فقط");
                return;
            }

            _lines[existing] = line with { Qty = line.Qty + 1 };
        }
        else
        {
            _lines.Add(new InvoiceLine(
                _lines.Count + 1,
                product.Name,
                1,
                product.SellingPrice,
                product.Id));
        }

        LoadGrid();
        RecalculateTotals();
    }

    private void OnGridActionClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        var name = _grid.Columns[e.ColumnIndex].Name;
        if (name == "colDelete")
        {
            _lines.RemoveAt(e.RowIndex);
            LoadGrid();
            RecalculateTotals();
        }
        else if (name == "colEdit")
        {
            MessageBox.Show(this, $"تعديل: {_lines[e.RowIndex].Name}", "تعديل صنف");
        }
    }

    private static void EnableDoubleBuffering(DataGridView grid)
    {
        typeof(DataGridView)
            .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(grid, true, null);
    }

    private static void OnPaidKeyPress(object? sender, KeyPressEventArgs e)
    {
        if (char.IsControl(e.KeyChar))
        {
            return;
        }

        if (char.IsDigit(e.KeyChar) || e.KeyChar is '.' or ',')
        {
            return;
        }

        e.Handled = true;
    }

    private static bool TryParseMoney(string? text, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var normalized = text.Trim().Replace(",", "");
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private void ResetInvoice()
    {
        _cmbCustomer.SelectedIndex = -1;
        _cmbCustomer.Text = "";
        ClearCustomerAndVehicle();
        _cmbTechnician.SelectedIndex = -1;
        _cmbPayment.SelectedIndex = -1;
        _txtDiscount.Text = "0.00";
        _txtPaid.Text = "0.00";
        _lines.Clear();
        LoadGrid();
        RecalculateTotals();
    }

    private string GetCarModelText()
    {
        if (_cmbCarModel.Visible && _cmbCarModel.SelectedItem is string selected)
        {
            return selected;
        }

        return _txtCarModel.Text.Trim();
    }

    private bool TryValidateInvoice(out string missingField)
    {
        missingField = "";

        if (string.IsNullOrWhiteSpace(_cmbCustomer.Text))
        {
            missingField = "اسم العميل";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_txtPhone.Text))
        {
            missingField = "رقم الهاتف";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_txtAddress.Text) || _txtAddress.Text == "—")
        {
            missingField = "العنوان";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_txtPlateLetters.Text))
        {
            missingField = "حروف اللوحة";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_txtPlateNumber.Text))
        {
            missingField = "رقم اللوحة";
            return false;
        }

        if (string.IsNullOrWhiteSpace(GetCarModelText()))
        {
            missingField = "الماركة / الموديل";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_txtOdometer.Text))
        {
            missingField = "قراءة العداد";
            return false;
        }

        if (_cmbTechnician.SelectedIndex < 0 || string.IsNullOrWhiteSpace(_cmbTechnician.Text))
        {
            missingField = "الفني المسؤول";
            return false;
        }

        if (_cmbPayment.SelectedIndex < 0 || string.IsNullOrWhiteSpace(_cmbPayment.Text))
        {
            missingField = "طريقة الدفع";
            return false;
        }

        if (_lines.Count == 0)
        {
            missingField = "أصناف الفاتورة";
            return false;
        }

        return true;
    }

    private void PrintInvoice()
    {
        var invoice = SaveInvoice(showSuccess: false);
        if (invoice is null)
        {
            return;
        }

        try
        {
            var path = Helpers.InvoicePdfGenerator.GenerateAndOpen(invoice);
            AppMessageDialog.Success(this,
                $"تم حفظ الفاتورة وطباعتها.\r\nرقم الفاتورة: {invoice.Number}\r\nالملف: {Path.GetFileName(path)}",
                "طباعة");
        }
        catch (Exception ex)
        {
            AppMessageDialog.Error(this, $"تعذر إنشاء ملف الطباعة.\r\n{ex.Message}", "طباعة");
        }
    }

    private InvoiceRecord? SaveInvoice(bool showSuccess = true)
    {
        if (!TryValidateInvoice(out var missingField))
        {
            AppMessageDialog.Warning(this, $"برجاء إدخال {missingField}");
            return null;
        }

        var subtotal = _lines.Sum(l => l.Total);
        _ = TryParseMoney(_txtDiscount.Text, out var discountValue);
        var discountUnit = _cmbDiscountUnit.SelectedItem?.ToString() ?? "%";
        var discount = discountUnit == "%"
            ? subtotal * discountValue / 100m
            : discountValue;
        var tax = 0m;
        var grand = Math.Max(0, subtotal - discount + tax);
        _ = TryParseMoney(_txtPaid.Text, out var paid);
        if (paid > grand)
        {
            paid = grand;
        }

        var chassis = "";
        if (_selectedCustomer is not null)
        {
            var carIndex = _cmbCarModel.Visible && _cmbCarModel.SelectedIndex >= 0
                ? _cmbCarModel.SelectedIndex
                : 0;
            if (carIndex >= 0 && carIndex < _selectedCustomer.Cars.Count)
            {
                chassis = _selectedCustomer.Cars[carIndex].Vin;
            }
        }

        var invoice = new InvoiceRecord
        {
            Number = InvoiceStore.NextNumber(),
            CreatedAt = DateTime.Now,
            CustomerName = _cmbCustomer.Text.Trim(),
            Phone = _txtPhone.Text.Trim(),
            Address = _txtAddress.Text.Trim(),
            PlateLetters = _txtPlateLetters.Text.Trim(),
            PlateNumber = _txtPlateNumber.Text.Trim(),
            CarModel = GetCarModelText(),
            ChassisNumber = string.IsNullOrWhiteSpace(chassis) ? "—" : chassis,
            Odometer = _txtOdometer.Text.Trim(),
            Technician = _cmbTechnician.Text.Trim(),
            PaymentMethod = _cmbPayment.Text.Trim(),
            Subtotal = subtotal,
            Discount = discount,
            DiscountUnit = discountUnit,
            Tax = tax,
            GrandTotal = grand,
            Paid = paid,
            Remaining = Math.Max(0, grand - paid),
            Items = _lines.Select(l => new InvoiceItemRecord
            {
                Name = l.Name,
                Qty = l.Qty,
                UnitPrice = l.UnitPrice
            }).ToList()
        };

        // Validate stock for catalog-linked lines before committing
        foreach (var line in _lines)
        {
            var product = (!string.IsNullOrWhiteSpace(line.ProductId)
                              ? ProductStore.Find(line.ProductId)
                              : null)
                          ?? ProductStore.FindByCodeOrName(line.Name);
            if (product is not null && product.Quantity < line.Qty)
            {
                AppMessageDialog.Warning(this,
                    $"الكمية غير كافية للصنف «{line.Name}» (المتاح: {product.Quantity})");
                return null;
            }
        }

        InvoiceStore.Add(invoice);

        foreach (var line in _lines)
        {
            if (!ProductStore.ApplySale(line.ProductId, line.Name, line.Qty, out var stockError))
            {
                AppMessageDialog.Warning(this, stockError ?? "تعذر تحديث المخزون");
            }
        }

        if (showSuccess)
        {
            AppMessageDialog.Success(this,
                $"تم حفظ الفاتورة بنجاح.\r\nرقم الفاتورة: {invoice.Number}",
                "حفظ");
        }

        return invoice;
    }

    private void RecalculateTotals()
    {
        var subtotal = _lines.Sum(l => l.Total);
        _ = TryParseMoney(_txtDiscount.Text, out var discountValue);
        var discount = _cmbDiscountUnit.SelectedItem?.ToString() == "%"
            ? subtotal * discountValue / 100m
            : discountValue;
        var tax = 0m;
        var grand = Math.Max(0, subtotal - discount + tax);
        _ = TryParseMoney(_txtPaid?.Text, out var paid);
        if (paid > grand)
        {
            paid = grand;
        }

        var remaining = Math.Max(0, grand - paid);

        _lblSubtotal.Text = subtotal.ToString("N2");
        _lblTax.Text = tax.ToString("N2");
        _lblGrandTotal.Text = grand.ToString("N2");
        _lblRemaining.Text = remaining.ToString("N2");
    }

    private void UpdateClock()
    {
        _lblTime.Text = DateTime.Now.ToString("hh:mm tt");
        _lblDate.Text = DateTime.Now.ToString("dd / MM / yyyy");
    }

    private static Bitmap CreateGlyphBitmap(string glyph, Color color, int size)
    {
        var bmp = new Bitmap(size + 6, size + 6);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using var font = new Font("Segoe MDL2 Assets", size * 0.7F, FontStyle.Regular, GraphicsUnit.Pixel);
        TextRenderer.DrawText(g, glyph, font, new Rectangle(0, 0, bmp.Width, bmp.Height), color,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        return bmp;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _clockTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    private sealed record InvoiceLine(int Id, string Name, int Qty, decimal UnitPrice, string? ProductId = null)
    {
        public decimal Total => Qty * UnitPrice;
    }
}
