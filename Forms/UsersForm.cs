using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

internal sealed class UsersForm : Form
{
    private readonly System.Windows.Forms.Timer _clockTimer = new() { Interval = 1000 };
    private Label _lblTime = null!;
    private Label _lblDate = null!;
    private Guna2TextBox _txtSearch = null!;
    private DataGridView _grid = null!;
    private Label _lblTotal = null!;
    private Label _lblActive = null!;
    private readonly bool _embedded;

    private List<UserRecord> _filtered = [];
    private Bitmap _deleteIcon = null!;

    public UsersForm(bool embedded = false)
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
        Name = "UsersForm";
        RightToLeft = RightToLeft.No;
        ShowIcon = !_embedded;
        ShowInTaskbar = !_embedded;
        StartPosition = FormStartPosition.CenterScreen;
        Text = _embedded ? string.Empty : "المستخدمون - Ibrahim Abdo Auto Service";
        WindowState = FormWindowState.Normal;

        UserStore.Load();
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
            Text = "المستخدمون",
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
            RowCount = 3,
            BackColor = Color.Transparent
        };
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        host.Controls.Add(BuildToolbar(), 0, 0);
        host.Controls.Add(BuildStats(), 0, 1);
        host.Controls.Add(BuildGridCard(), 0, 2);
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
            PlaceholderText = "بحث بالاسم / الحساب / الصلاحية",
            PlaceholderForeColor = InvoiceTheme.Muted,
            Size = new Size(520, 40),
            Location = new Point(0, 0),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            IconLeft = GlyphHelper.Create("\uE721", InvoiceTheme.Gold, 16),
            IconLeftSize = new Size(18, 18),
            FocusedState = { BorderColor = InvoiceTheme.Gold }
        };
        _txtSearch.TextChanged += (_, _) => RefreshData();

        var addBtn = CreateToolbarButton("+ إضافة مستخدم", true, (_, _) => OpenAddUser());
        addBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        bar.Resize += (_, _) =>
        {
            addBtn.Left = Math.Max(_txtSearch.Right + 16, bar.ClientSize.Width - addBtn.Width);
            addBtn.Top = 0;
        };

        bar.Controls.Add(_txtSearch);
        bar.Controls.Add(addBtn);
        addBtn.Left = Math.Max(_txtSearch.Right + 16, bar.ClientSize.Width - addBtn.Width);
        return bar;
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

        _lblTotal = new Label();
        _lblActive = new Label();
        row.Controls.Add(CreateStatCard("إجمالي المستخدمين", _lblTotal, "\uE716"), 0, 0);
        row.Controls.Add(CreateStatCard("الحسابات النشطة", _lblActive, "\uE73E"), 1, 0);
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

        _grid = CreateGrid();
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIndex", HeaderText = "#", FillWeight = 6 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "الاسم", FillWeight = 20 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUsername", HeaderText = "اسم الحساب", FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRole", HeaderText = "الصلاحية", FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "الحالة", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCreated", HeaderText = "تاريخ الإنشاء", FillWeight = 16 });
        _grid.Columns.Add(new DataGridViewImageColumn
        {
            Name = "colDelete",
            HeaderText = "حذف",
            FillWeight = 8,
            Image = _deleteIcon,
            ImageLayout = DataGridViewImageCellLayout.Zoom
        });

        _grid.CellClick += OnGridCellClick;
        card.Controls.Add(_grid);
        return card;
    }

    private void RefreshData()
    {
        _filtered = UserStore.Search(_txtSearch?.Text ?? "").ToList();
        _lblTotal.Text = UserStore.TotalUsers.ToString();
        _lblActive.Text = UserStore.ActiveUsers.ToString();
        BindGrid();
    }

    private void BindGrid()
    {
        _grid.Rows.Clear();
        var index = 1;
        foreach (var u in _filtered)
        {
            _grid.Rows.Add(
                index++,
                u.DisplayName,
                u.Username,
                u.Role,
                u.IsActive ? "نشط" : "موقوف",
                u.CreatedAt.ToString("dd/MM/yyyy"),
                _deleteIcon);
            _grid.Rows[^1].Tag = u;
        }
    }

    private void OnGridCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
        if (_grid.Columns[e.ColumnIndex].Name != "colDelete") return;
        if (_grid.Rows[e.RowIndex].Tag is not UserRecord user) return;

        if (UserStore.TotalUsers <= 1)
        {
            AppMessageDialog.Warning(this, "لا يمكن حذف آخر مستخدم في النظام.");
            return;
        }

        if (AppMessageDialog.Confirm(this, $"حذف المستخدم «{user.DisplayName}»؟"))
        {
            UserStore.Remove(user.Id);
            RefreshData();
        }
    }

    private void OpenAddUser()
    {
        using var dlg = new AddUserDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result is not null)
        {
            UserStore.Add(dlg.Result);
            RefreshData();
            AppMessageDialog.Success(this,
                $"تم إنشاء الحساب بنجاح.\r\n\r\nاسم الحساب: {dlg.Result.Username}");
        }
    }

    private static DataGridView CreateGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = InvoiceTheme.Card,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.Single,
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
            RowTemplate = { Height = 36 },
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
        typeof(DataGridView)
            .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(grid, true, null);
        return grid;
    }

    private static Guna2Button CreateToolbarButton(string text, bool primary, EventHandler onClick)
    {
        var btn = new Guna2Button
        {
            Text = text,
            Font = InvoiceTheme.SmallFont,
            Height = 36,
            MinimumSize = new Size(130, 36),
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
