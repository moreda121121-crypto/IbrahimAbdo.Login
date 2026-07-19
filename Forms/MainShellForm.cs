using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

/// <summary>
/// Single fixed window: sidebar stays, content pages swap in-place.
/// </summary>
internal sealed class MainShellForm : Form
{
    private readonly Panel _contentHost;
    private readonly Dictionary<string, Form> _pages = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Guna2Panel> _menuRows = new(StringComparer.Ordinal);
    private string _activeKey = "invoice";

    public MainShellForm()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = InvoiceTheme.Background;
        ClientSize = new Size(1280, 720);
        DoubleBuffered = true;
        Font = InvoiceTheme.BodyFont;
        ForeColor = InvoiceTheme.White;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MaximumSize = Size.Empty; // allow full screen
        MinimizeBox = true;
        MinimumSize = new Size(1100, 650);
        Name = "MainShellForm";
        RightToLeft = RightToLeft.No;
        ShowIcon = true;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Ibrahim Abdo Auto Service";
        WindowState = FormWindowState.Maximized;
        if (AppIcon.Current is { } appIcon)
        {
            Icon = appIcon;
        }

        CustomerStore.Load();
        UserStore.Load();

        var shell = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = InvoiceTheme.Background,
            RightToLeft = RightToLeft.No
        };

        var sidebar = BuildSidebar();
        sidebar.Dock = DockStyle.Left;
        sidebar.Width = InvoiceTheme.SidebarWidth;

        _contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = InvoiceTheme.Background,
            RightToLeft = RightToLeft.No
        };

        shell.Controls.Add(_contentHost);
        shell.Controls.Add(sidebar);
        Controls.Add(shell);

        WireWindowChrome();
        ResumeLayout(true);

        // Default page
        Navigate("invoice");
    }

    public void Navigate(string key)
    {
        if (key is not ("invoice" or "customers" or "users"))
        {
            var label = key switch
            {
                "dashboard" => "لوحة التحكم",
                "vehicles" => "السيارات",
                "services" => "الخدمات",
                "inventory" => "المخزون",
                "techs" => "الفنيون",
                "settings" => "الإعدادات",
                _ => key
            };
            MessageBox.Show(this, $"صفحة «{label}» قريباً.", "قريباً", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        foreach (Control c in _contentHost.Controls)
        {
            c.Visible = false;
        }

        if (!_pages.TryGetValue(key, out var page))
        {
            page = key switch
            {
                "customers" => new CustomersForm(embedded: true),
                "users" => new UsersForm(embedded: true),
                _ => new SalesInvoiceForm(embedded: true)
            };
            page.TopLevel = false;
            page.FormBorderStyle = FormBorderStyle.None;
            page.Dock = DockStyle.Fill;
            page.Visible = false;
            _contentHost.Controls.Add(page);
            page.Show();
            AttachDrag(page);
            _pages[key] = page;
        }

        page.Visible = true;
        page.BringToFront();
        _activeKey = key;
        UpdateMenuHighlight();
        Text = key switch
        {
            "customers" => "العملاء - Ibrahim Abdo Auto Service",
            "users" => "المستخدمون - Ibrahim Abdo Auto Service",
            _ => "فاتورة بيع - Ibrahim Abdo Auto Service"
        };
    }

    private void UpdateMenuHighlight()
    {
        foreach (var (key, row) in _menuRows)
        {
            var active = key == _activeKey;
            row.FillColor = active ? Color.FromArgb(48, InvoiceTheme.Gold) : Color.Transparent;
            row.CustomBorderThickness = active ? new Padding(0, 1, 0, 1) : new Padding(0);
            foreach (Control child in row.Controls)
            {
                if (child is Label lbl)
                {
                    lbl.ForeColor = active ? InvoiceTheme.Gold : Color.FromArgb(210, 210, 210);
                }
            }
        }
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
            menuHost.Controls.Add(CreateMenuItem(item[0], item[1], item[2]));
        }

        sidebar.Controls.Add(menuHost);
        sidebar.Controls.Add(logo);
        return sidebar;
    }

    private Control CreateMenuItem(string glyph, string text, string key)
    {
        const int iconPx = 26;
        var row = new Guna2Panel
        {
            Width = InvoiceTheme.SidebarWidth - 28,
            Height = 46,
            Margin = new Padding(0, 0, 0, 4),
            BorderRadius = 8,
            FillColor = Color.Transparent,
            Cursor = Cursors.Hand,
            CustomBorderColor = InvoiceTheme.Gold,
            Tag = "nav"
        };

        var icon = new PictureBox
        {
            Dock = DockStyle.Left,
            Width = 36,
            SizeMode = PictureBoxSizeMode.CenterImage,
            BackColor = Color.Transparent,
            Image = GlyphHelper.Create(glyph, InvoiceTheme.Gold, iconPx),
            Padding = new Padding(8, 0, 0, 0),
            Tag = "nav"
        };

        var label = new Label
        {
            Dock = DockStyle.Fill,
            Text = text,
            Font = InvoiceTheme.MenuFont,
            ForeColor = Color.FromArgb(210, 210, 210),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
            Padding = new Padding(45, 0, 8, 0),
            Tag = "nav"
        };

        void Hover(bool on)
        {
            if (key == _activeKey) return;
            row.FillColor = on ? Color.FromArgb(36, InvoiceTheme.Gold) : Color.Transparent;
            label.ForeColor = on ? InvoiceTheme.Gold : Color.FromArgb(210, 210, 210);
        }

        void ClickNav(object? s, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Navigate(key);
            }
        }

        foreach (Control c in new Control[] { row, icon, label })
        {
            c.MouseEnter += (_, _) => Hover(true);
            c.MouseLeave += (_, _) => Hover(false);
            c.MouseUp += ClickNav;
        }

        row.Controls.Add(icon);
        row.Controls.Add(label);
        _menuRows[key] = row;
        return row;
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
        if (control.Tag is "nav"
            || control is Guna2Button or Guna2TextBox or Guna2ComboBox or DataGridView
            or TextBoxBase or ComboBox or PictureBox)
        {
            foreach (Control child in control.Controls)
            {
                AttachDrag(child);
            }

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
}
