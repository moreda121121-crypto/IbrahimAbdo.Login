using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

internal sealed partial class LoginForm : Form
{
    private bool _passwordVisible;

    public LoginForm()
    {
        InitializeComponent();
        WireEvents();
        ApplyIconGlyphs();
    }

    private void WireEvents()
    {
        Load += OnFormLoad;
        Resize += (_, _) => PositionLoginPanel();

        _btnLogin.Click += (_, _) => SubmitLogin();
        _txtUsername.KeyDown += OnUsernameKeyDown;
        _txtPassword.KeyDown += OnPasswordKeyDown;
        _txtPassword.IconRightClick += (_, _) => TogglePasswordVisibility();
        _lnkForgotPassword.LinkClicked += (_, _) =>
            MessageBox.Show(this, "Please contact your system administrator to reset your password.", "Forgot Password", MessageBoxButtons.OK, MessageBoxIcon.Information);

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };
    }

    private void ApplyIconGlyphs()
    {
        // Render MDL2 icons into small bitmaps for Guna textboxes
        _txtUsername.IconLeft = CreateGlyphIcon("\uE77B", AppTheme.Gray);
        _txtPassword.IconLeft = CreateGlyphIcon("\uE72E", AppTheme.Gray);
        _txtPassword.IconRight = CreateGlyphIcon("\uE890", AppTheme.Gray);
        _txtPassword.IconRightSize = new Size(18, 18);
        _txtUsername.IconLeftSize = new Size(18, 18);
        _txtPassword.IconLeftSize = new Size(18, 18);

        _txtUsername.Enter += (_, _) => _txtUsername.IconLeft = CreateGlyphIcon("\uE77B", AppTheme.Gold);
        _txtUsername.Leave += (_, _) => _txtUsername.IconLeft = CreateGlyphIcon("\uE77B", AppTheme.Gray);
        _txtPassword.Enter += (_, _) => _txtPassword.IconLeft = CreateGlyphIcon("\uE72E", AppTheme.Gold);
        _txtPassword.Leave += (_, _) => _txtPassword.IconLeft = CreateGlyphIcon("\uE72E", AppTheme.Gray);
    }

    private static Bitmap CreateGlyphIcon(string glyph, Color color)
    {
        var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using var font = new Font("Segoe MDL2 Assets", 12F, FontStyle.Regular, GraphicsUnit.Point);
        TextRenderer.DrawText(
            graphics,
            glyph,
            font,
            new Rectangle(0, 0, 24, 24),
            color,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        return bitmap;
    }

    private void OnFormLoad(object? sender, EventArgs e)
    {
        LoadBackgroundImage();
        PositionLoginPanel();
    }

    private void LoadBackgroundImage()
    {
        var imagePath = Path.Combine(AppContext.BaseDirectory, "Assets", "Rest finish.png");
        if (!File.Exists(imagePath))
        {
            MessageBox.Show(
                this,
                "Background image not found.\r\n\r\nExpected path:\r\nAssets\\Rest finish.png",
                "Background Image",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            BackgroundImage?.Dispose();
            using var stream = File.OpenRead(imagePath);
            BackgroundImage = Image.FromStream(stream);
            BackgroundImageLayout = ImageLayout.Stretch;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Unable to load background image.\r\n\r\n{ex.Message}", "Background Image", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PositionLoginPanel()
    {
        _loginPanel.Size = new Size(AppTheme.PanelWidth, AppTheme.PanelHeight);

        // Center in the black void: below AUTO SERVICE / ABDO, above bottom feature labels
        var x = (ClientSize.Width - _loginPanel.Width) / 2;
        var y = (int)(ClientSize.Height * AppTheme.LogoClearanceRatio) + AppTheme.PanelDownOffset;
        var maxY = ClientSize.Height - _loginPanel.Height - AppTheme.BottomArtworkClearance;
        if (y > maxY)
        {
            y = Math.Max(0, maxY);
        }

        _loginPanel.Location = new Point(x, y);
        RelayoutPanelContent();
    }

    private void RelayoutPanelContent()
    {
        const int contentWidth = AppTheme.ContentWidth;
        var side = AppTheme.SidePadding;

        _btnUserIcon.Left = (AppTheme.PanelWidth - _btnUserIcon.Width) / 2;
        _lblWelcome.SetBounds(side, _lblWelcome.Top, contentWidth, _lblWelcome.Height);
        _lblSubtitle.SetBounds(side, _lblSubtitle.Top, contentWidth, _lblSubtitle.Height);
        _txtUsername.SetBounds(side, _txtUsername.Top, contentWidth, AppTheme.InputHeight);
        _txtPassword.SetBounds(side, _txtPassword.Top, contentWidth, AppTheme.InputHeight);
        _lnkForgotPassword.SetBounds(side, _lnkForgotPassword.Top, contentWidth, _lnkForgotPassword.Height);
        _btnLogin.SetBounds(side, _btnLogin.Top, contentWidth, AppTheme.ButtonHeight);
        _lblVersion.SetBounds(side, _lblVersion.Top, contentWidth, _lblVersion.Height);
        _lblCopyright.SetBounds(side, _lblCopyright.Top, contentWidth, _lblCopyright.Height);
    }

    private void OnUsernameKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Down)
        {
            _txtPassword.Focus();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void OnPasswordKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Up)
        {
            _txtUsername.Focus();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.Enter)
        {
            SubmitLogin();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void TogglePasswordVisibility()
    {
        _passwordVisible = !_passwordVisible;
        _txtPassword.UseSystemPasswordChar = !_passwordVisible;
        _txtPassword.PasswordChar = _passwordVisible ? '\0' : '\u25CF';
        _txtPassword.IconRight?.Dispose();
        _txtPassword.IconRight = CreateGlyphIcon(_passwordVisible ? "\uE7B3" : "\uE890", _passwordVisible ? AppTheme.Gold : AppTheme.Gray);
    }

    private void SubmitLogin()
    {
        var username = _txtUsername.Text.Trim();
        var password = _txtPassword.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show(this, "Please enter your username and password.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Default test account
        if (username != "123" || password != "123")
        {
            MessageBox.Show(this, "Invalid username or password.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        Hide();
        Data.CustomerStore.Load();
        using var shell = new MainShellForm();
        shell.ShowDialog();
        Close();
    }
}
