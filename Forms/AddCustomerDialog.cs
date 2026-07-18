using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

internal sealed class AddCustomerDialog : Form
{
    private readonly FlowLayoutPanel _carsHost;
    private readonly Guna2TextBox _txtName;
    private readonly Guna2TextBox _txtPhone;
    private readonly Guna2TextBox _txtEmail;
    private readonly Guna2TextBox _txtAddress;
    private readonly Guna2TextBox _txtNotes;
    private readonly List<CarFormBlock> _carBlocks = [];

    public CustomerRecord? Result { get; private set; }

    public AddCustomerDialog()
    {
        SuspendLayout();
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "إضافة عميل جديد";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = InvoiceTheme.Background;
        ForeColor = InvoiceTheme.White;
        Font = InvoiceTheme.BodyFont;
        ClientSize = new Size(920, 620);
        RightToLeft = RightToLeft.No;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = InvoiceTheme.Background,
            Padding = new Padding(16)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "إضافة عميل جديد",
            Font = InvoiceTheme.TitleFont,
            ForeColor = InvoiceTheme.Gold,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = InvoiceTheme.Background,
            Padding = new Padding(4)
        };

        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Width = 860,
            BackColor = Color.Transparent
        };

        // —— Customer info ——
        var customerCard = CreateSectionCard("بيانات العميل", "\uE77B", extraRows: 1);
        var customerGrid = CreateTwoColumnGrid();
        _txtName = AddField(customerGrid, 0, 0, "اسم العميل *", "أدخل اسم العميل");
        _txtPhone = AddField(customerGrid, 1, 0, "رقم الهاتف *", "01xxxxxxxxx");
        _txtEmail = AddField(customerGrid, 0, 1, "البريد الإلكتروني", "اختياري");
        _txtAddress = AddField(customerGrid, 1, 1, "العنوان", "اختياري");
        customerCard.Controls.Add(customerGrid, 0, 1);

        var notesHost = new Panel { Dock = DockStyle.Fill, Height = 70, Margin = new Padding(8, 0, 8, 8) };
        var notesLbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Text = "ملاحظات",
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont
        };
        _txtNotes = CreateInput("اختياري");
        _txtNotes.Dock = DockStyle.Fill;
        _txtNotes.Multiline = true;
        notesHost.Controls.Add(_txtNotes);
        notesHost.Controls.Add(notesLbl);
        customerCard.Controls.Add(notesHost, 0, 2);
        customerCard.RowStyles[2] = new RowStyle(SizeType.Absolute, 78);

        // —— Cars ——
        var carsCard = CreateSectionCard("بيانات السيارة الأولى", "\uE7EC", extraRows: 1);
        _carsHost = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(4)
        };
        carsCard.Controls.Add(_carsHost, 0, 1);

        var addCarBtn = new Guna2Button
        {
            Text = "+ إضافة سيارة أخرى لهذا العميل",
            Font = InvoiceTheme.MenuFont,
            ForeColor = InvoiceTheme.Gold,
            FillColor = Color.Transparent,
            BorderColor = InvoiceTheme.Gold,
            BorderThickness = 1,
            BorderRadius = 8,
            Height = 40,
            Dock = DockStyle.Top,
            Margin = new Padding(8, 4, 8, 8),
            Cursor = Cursors.Hand,
            HoverState = { FillColor = Color.FromArgb(30, InvoiceTheme.Gold), ForeColor = InvoiceTheme.Gold }
        };
        addCarBtn.Click += (_, _) => AddCarBlock();
        carsCard.Controls.Add(addCarBtn, 0, 2);
        carsCard.RowStyles[2] = new RowStyle(SizeType.Absolute, 52);

        body.Controls.Add(customerCard);
        body.Controls.Add(carsCard);
        scroll.Controls.Add(body);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0)
        };
        var btnSave = CreateGoldButton("حفظ العميل", "\uE74E");
        btnSave.Click += (_, _) => Save();
        var btnCancel = CreateOutlineButton("إلغاء");
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        footer.Controls.Add(btnSave);
        footer.Controls.Add(btnCancel);

        root.Controls.Add(title, 0, 0);
        root.Controls.Add(scroll, 0, 1);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);

        AddCarBlock();
        ResumeLayout(true);
    }

    private void AddCarBlock()
    {
        var index = _carBlocks.Count + 1;
        var block = new CarFormBlock(index);
        _carBlocks.Add(block);
        _carsHost.Controls.Add(block.Panel);
        if (_carBlocks.Count > 1)
        {
            // update section title feel via first label inside panel
        }
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text) || string.IsNullOrWhiteSpace(_txtPhone.Text))
        {
            MessageBox.Show(this, "اسم العميل ورقم الهاتف مطلوبان.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var cars = new List<CarRecord>();
        foreach (var block in _carBlocks)
        {
            if (!block.TryBuild(out var car, out var error))
            {
                MessageBox.Show(this, error, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            cars.Add(car!);
        }

        if (cars.Count == 0)
        {
            MessageBox.Show(this, "أضف سيارة واحدة على الأقل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Result = new CustomerRecord
        {
            Name = _txtName.Text.Trim(),
            Phone = _txtPhone.Text.Trim(),
            Email = _txtEmail.Text.Trim(),
            Address = _txtAddress.Text.Trim(),
            Notes = _txtNotes.Text.Trim(),
            RegisteredAt = DateTime.Today,
            Cars = cars
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    private static TableLayoutPanel CreateSectionCard(string title, string glyph, int extraRows = 0)
    {
        var card = new TableLayoutPanel
        {
            Width = 860,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2 + extraRows,
            BackColor = InvoiceTheme.Card,
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(4),
            RightToLeft = RightToLeft.No
        };
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        for (var i = 0; i < extraRows; i++)
        {
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        }

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var icon = new PictureBox
        {
            Dock = DockStyle.Left,
            Width = 36,
            SizeMode = PictureBoxSizeMode.CenterImage,
            Image = GlyphHelper.Create(glyph, InvoiceTheme.Gold, 18),
            BackColor = Color.Transparent
        };
        var lbl = new Label
        {
            Dock = DockStyle.Fill,
            Text = title,
            Font = InvoiceTheme.SectionFont,
            ForeColor = InvoiceTheme.Gold,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(4, 0, 0, 0)
        };
        header.Controls.Add(lbl);
        header.Controls.Add(icon);
        card.Controls.Add(header, 0, 0);

        // border via paint
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(InvoiceTheme.CardBorder, 1F);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        return card;
    }

    private static TableLayoutPanel CreateTwoColumnGrid()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(8),
            BackColor = Color.Transparent
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        return grid;
    }

    private static Guna2TextBox AddField(TableLayoutPanel grid, int col, int row, string label, string placeholder)
    {
        var host = new Panel { Dock = DockStyle.Fill, Margin = new Padding(6), BackColor = Color.Transparent };
        var lbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = label,
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont
        };
        var input = CreateInput(placeholder);
        input.Dock = DockStyle.Fill;
        host.Controls.Add(input);
        host.Controls.Add(lbl);
        grid.Controls.Add(host, col, row);
        return input;
    }

    private static Guna2TextBox CreateInput(string placeholder) =>
        new()
        {
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(90, InvoiceTheme.Gold),
            FillColor = InvoiceTheme.InputFill,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.BodyFont,
            PlaceholderText = placeholder,
            PlaceholderForeColor = InvoiceTheme.Muted,
            Height = 34,
            FocusedState = { BorderColor = InvoiceTheme.Gold },
            HoverState = { BorderColor = InvoiceTheme.Gold }
        };

    private static Guna2Button CreateGoldButton(string text, string glyph)
    {
        var btn = new Guna2Button
        {
            Text = text,
            Font = InvoiceTheme.MenuFont,
            ForeColor = Color.Black,
            FillColor = InvoiceTheme.Gold,
            BorderRadius = InvoiceTheme.Radius,
            Size = new Size(160, 42),
            Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand,
            Image = GlyphHelper.Create(glyph, Color.Black, 16),
            ImageSize = new Size(16, 16),
            ImageAlign = HorizontalAlignment.Left,
            HoverState = { FillColor = InvoiceTheme.GoldDark, ForeColor = Color.Black }
        };
        return btn;
    }

    private static Guna2Button CreateOutlineButton(string text)
    {
        return new Guna2Button
        {
            Text = text,
            Font = InvoiceTheme.MenuFont,
            ForeColor = InvoiceTheme.White,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.Gold,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Size = new Size(120, 42),
            Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand,
            HoverState = { FillColor = Color.FromArgb(30, InvoiceTheme.Gold), ForeColor = InvoiceTheme.White }
        };
    }

    private sealed class CarFormBlock
    {
        public Panel Panel { get; }
        private readonly Guna2TextBox _plate;
        private readonly Guna2ComboBox _brand;
        private readonly Guna2ComboBox _model;
        private readonly Guna2ComboBox _year;
        private readonly Guna2ComboBox _color;
        private readonly Guna2TextBox _vin;
        private readonly Guna2TextBox _mileage;
        private readonly Guna2ComboBox _fuel;
        private readonly Guna2ComboBox _trans;

        public CarFormBlock(int index)
        {
            Panel = new Panel
            {
                Width = 820,
                Height = 220,
                Margin = new Padding(4, 4, 4, 10),
                BackColor = InvoiceTheme.InputFill,
                Padding = new Padding(8)
            };

            var title = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Text = index == 1 ? "السيارة الأولى" : $"سيارة إضافية #{index}",
                ForeColor = InvoiceTheme.Gold,
                Font = InvoiceTheme.SmallFont
            };

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 0)
            };
            for (var i = 0; i < 3; i++)
            {
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            }

            _plate = CreateMini("رقم اللوحة *");
            _brand = CreateCombo("Toyota", "Hyundai", "BMW", "Mercedes", "Kia", "Nissan", "Cherokee");
            _model = CreateCombo("Corolla", "Elantra", "320i", "C200", "Sportage", "Sunny", "Grand Cherokee");
            _year = CreateCombo(Enumerable.Range(2005, DateTime.Now.Year - 2004).Reverse().Select(y => y.ToString()).ToArray());
            _color = CreateCombo("أسود", "أبيض", "رمادي", "فضي", "أزرق", "أحمر");
            _vin = CreateMini("رقم الشاسيه");
            _mileage = CreateMini("قراءة العداد");
            _fuel = CreateCombo("بنزين", "ديزل", "هايبرد", "كهرباء");
            _trans = CreateCombo("أوتوماتيك", "مانيوال");

            Place(grid, Labeled("رقم اللوحة *", _plate), 0, 0);
            Place(grid, Labeled("النوع / الماركة *", _brand), 1, 0);
            Place(grid, Labeled("الموديل *", _model), 2, 0);
            Place(grid, Labeled("سنة الصنع *", _year), 0, 1);
            Place(grid, Labeled("اللون", _color), 1, 1);
            Place(grid, Labeled("VIN", _vin), 2, 1);
            Place(grid, Labeled("العداد (كم)", _mileage), 0, 2);
            Place(grid, Labeled("نوع الوقود", _fuel), 1, 2);
            Place(grid, Labeled("ناقل الحركة", _trans), 2, 2);

            Panel.Controls.Add(grid);
            Panel.Controls.Add(title);
        }

        public bool TryBuild(out CarRecord? car, out string error)
        {
            car = null;
            error = "";
            if (string.IsNullOrWhiteSpace(_plate.Text) ||
                _brand.SelectedItem is null ||
                _model.SelectedItem is null ||
                _year.SelectedItem is null)
            {
                error = "رقم اللوحة والنوع والموديل والسنة مطلوبة لكل سيارة.";
                return false;
            }

            _ = int.TryParse(_year.SelectedItem.ToString(), out var year);
            _ = int.TryParse(_mileage.Text.Replace(",", ""), out var mileage);

            car = new CarRecord
            {
                PlateNumber = _plate.Text.Trim(),
                Brand = _brand.SelectedItem.ToString()!,
                Model = _model.SelectedItem.ToString()!,
                Year = year,
                Color = _color.SelectedItem?.ToString() ?? "",
                Vin = _vin.Text.Trim(),
                Mileage = mileage,
                FuelType = _fuel.SelectedItem?.ToString() ?? "بنزين",
                Transmission = _trans.SelectedItem?.ToString() ?? "أوتوماتيك"
            };
            return true;
        }

        private static void Place(TableLayoutPanel grid, Control c, int col, int row)
        {
            c.Dock = DockStyle.Fill;
            c.Margin = new Padding(4);
            grid.Controls.Add(c, col, row);
        }

        private static Control Labeled(string text, Control input)
        {
            var host = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var lbl = new Label
            {
                Dock = DockStyle.Top,
                Height = 18,
                Text = text,
                ForeColor = InvoiceTheme.Muted,
                Font = InvoiceTheme.SmallFont
            };
            input.Dock = DockStyle.Fill;
            host.Controls.Add(input);
            host.Controls.Add(lbl);
            return host;
        }

        private static Guna2TextBox CreateMini(string placeholder) =>
            new()
            {
                BorderRadius = 8,
                BorderThickness = 1,
                BorderColor = Color.FromArgb(90, InvoiceTheme.Gold),
                FillColor = InvoiceTheme.Card,
                ForeColor = InvoiceTheme.White,
                Font = InvoiceTheme.BodyFont,
                PlaceholderText = placeholder,
                PlaceholderForeColor = InvoiceTheme.Muted,
                Height = 32,
                FocusedState = { BorderColor = InvoiceTheme.Gold }
            };

        private static Guna2ComboBox CreateCombo(params string[] items)
        {
            var combo = new Guna2ComboBox
            {
                BorderRadius = 8,
                BorderThickness = 1,
                BorderColor = Color.FromArgb(90, InvoiceTheme.Gold),
                FillColor = InvoiceTheme.Card,
                ForeColor = InvoiceTheme.White,
                Font = InvoiceTheme.BodyFont,
                Height = 32,
                FocusedColor = InvoiceTheme.Gold,
                ItemsAppearance = { BackColor = InvoiceTheme.Card, ForeColor = InvoiceTheme.White }
            };
            combo.Items.AddRange(items);
            if (items.Length > 0)
            {
                combo.SelectedIndex = 0;
            }

            return combo;
        }
    }
}
