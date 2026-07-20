using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

internal sealed class TechniciansForm : Form
{
    private readonly System.Windows.Forms.Timer _clockTimer = new() { Interval = 1000 };
    private readonly bool _embedded;
    private Label _lblTime = null!;
    private Label _lblDate = null!;
    private Label _lblTotal = null!;
    private Guna2TextBox _txtSearch = null!;
    private DataGridView _grid = null!;
    private Bitmap _deleteIcon = null!;
    private List<TechnicianRecord> _filtered = [];

    public TechniciansForm(bool embedded = false)
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
        Name = "TechniciansForm";
        RightToLeft = RightToLeft.No;
        ShowIcon = !_embedded;
        ShowInTaskbar = !_embedded;
        StartPosition = FormStartPosition.Manual;
        Text = _embedded ? string.Empty : "الفنيين - Ibrahim Abdo Auto Service";
        WindowState = FormWindowState.Normal;

        TechnicianStore.Load();
        _deleteIcon = GlyphHelper.Create("\uE74D", InvoiceTheme.Danger, 14);

        BuildUi();
        RefreshData();

        VisibleChanged += (_, _) =>
        {
            if (Visible)
            {
                TechnicianStore.Load();
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
            Text = "الفنيين",
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
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
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
            PlaceholderText = "بحث بالاسم / الرقم / العنوان",
            PlaceholderForeColor = InvoiceTheme.Muted,
            Dock = DockStyle.Left,
            Width = 360,
            Height = 36,
            Margin = new Padding(0, 4, 8, 0),
            IconLeft = GlyphHelper.Create("\uE721", InvoiceTheme.Gold, 14),
            IconLeftSize = new Size(16, 16),
            FocusedState = { BorderColor = InvoiceTheme.Gold }
        };
        _txtSearch.TextChanged += (_, _) => RefreshData();

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 0)
        };
        buttons.Controls.Add(CreateToolbarButton("+ إضافة فني", true, (_, _) => OpenAdd()));

        bar.Controls.Add(_txtSearch, 0, 0);
        bar.Controls.Add(buttons, 1, 0);
        return bar;
    }

    private Control BuildStats()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 4)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _lblTotal = new Label();
        row.Controls.Add(CreateStatCard("إجمالي الفنيين", _lblTotal, "\uE718"), 0, 0);
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
            RowTemplate = { Height = 36 },
            Font = InvoiceTheme.BodyFont,
            ForeColor = InvoiceTheme.White,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = InvoiceTheme.Card,
                ForeColor = InvoiceTheme.White,
                SelectionBackColor = Color.FromArgb(48, InvoiceTheme.Gold),
                SelectionForeColor = InvoiceTheme.White
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
                Font = InvoiceTheme.TableHeaderFont
            },
            ColumnHeadersHeight = 36
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIndex", HeaderText = "#", FillWeight = 6 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "اسم الفني", FillWeight = 22 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPhone", HeaderText = "رقم الهاتف", FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAddress", HeaderText = "العنوان", FillWeight = 34 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCreated", HeaderText = "تاريخ الإضافة", FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewImageColumn
        {
            Name = "colDelete",
            HeaderText = "حذف",
            FillWeight = 8,
            Image = _deleteIcon,
            ImageLayout = DataGridViewImageCellLayout.Zoom
        });
        _grid.CellClick += OnGridCellClick;
        EnableDoubleBuffering(_grid);

        card.Controls.Add(_grid);
        return card;
    }

    public void RefreshData()
    {
        _filtered = TechnicianStore.Search(_txtSearch?.Text ?? "").ToList();
        _lblTotal.Text = TechnicianStore.Total.ToString();
        BindGrid();
    }

    private void BindGrid()
    {
        _grid.Rows.Clear();
        var index = 1;
        foreach (var t in _filtered)
        {
            var row = _grid.Rows.Add(
                index++,
                t.Name,
                t.Phone,
                string.IsNullOrWhiteSpace(t.Address) ? "—" : t.Address,
                t.CreatedAt.ToString("dd/MM/yyyy"),
                _deleteIcon);
            _grid.Rows[row].Tag = t.Id;
        }
    }

    private void OnGridCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        if (_grid.Columns[e.ColumnIndex].Name != "colDelete")
        {
            return;
        }

        if (_grid.Rows[e.RowIndex].Tag is not string id)
        {
            return;
        }

        var tech = TechnicianStore.All.FirstOrDefault(t => t.Id == id);
        if (tech is null)
        {
            return;
        }

        if (MessageBox.Show(this, $"حذف الفني «{tech.Name}»؟", "تأكيد",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        TechnicianStore.Remove(id);
        RefreshData();
    }

    private void OpenAdd()
    {
        using var dlg = new AddTechnicianDialog();
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK || dlg.Result is null)
        {
            return;
        }

        TechnicianStore.Add(dlg.Result);
        RefreshData();
        AppMessageDialog.Success(this, $"تم إضافة الفني «{dlg.Result.Name}» بنجاح.", "الفنيين");
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

    private static Guna2Button CreateToolbarButton(string text, bool primary, EventHandler onClick)
    {
        var btn = new Guna2Button
        {
            Text = text,
            Font = InvoiceTheme.SmallFont,
            Height = 36,
            MinimumSize = new Size(110, 36),
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
        _deleteIcon.Dispose();
        base.OnFormClosed(e);
    }
}
