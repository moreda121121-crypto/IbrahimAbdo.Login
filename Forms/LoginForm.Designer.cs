#nullable enable
using Guna.UI2.WinForms;
using Guna.UI2.WinForms.Enums;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

partial class LoginForm
{
    private System.ComponentModel.IContainer? components = null;

    private Guna2Panel _loginPanel = null!;
    private Guna2CircleButton _btnUserIcon = null!;
    private Label _lblWelcome = null!;
    private Label _lblSubtitle = null!;
    private Guna2TextBox _txtUsername = null!;
    private Guna2TextBox _txtPassword = null!;
    private LinkLabel _lnkForgotPassword = null!;
    private Guna2GradientButton _btnLogin = null!;
    private Label _lblVersion = null!;
    private Label _lblCopyright = null!;

    private const int ContentWidth = AppTheme.ContentWidth;
    private const int IconSize = 40;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            BackgroundImage?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _loginPanel = new Guna2Panel();
        _btnUserIcon = new Guna2CircleButton();
        _lblWelcome = new Label();
        _lblSubtitle = new Label();
        _txtUsername = new Guna2TextBox();
        _txtPassword = new Guna2TextBox();
        _lnkForgotPassword = new LinkLabel();
        _btnLogin = new Guna2GradientButton();
        _lblVersion = new Label();
        _lblCopyright = new Label();

        SuspendLayout();
        _loginPanel.SuspendLayout();

        // LoginForm — normal Windows window
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = AppTheme.Dark;
        BackgroundImageLayout = ImageLayout.Stretch;
        ClientSize = new Size(1280, 720);
        DoubleBuffered = true;
        Font = AppTheme.LabelFont;
        ForeColor = AppTheme.White;
        FormBorderStyle = FormBorderStyle.Sizable;
        KeyPreview = true;
        MaximizeBox = true;
        MaximumSize = new Size(1600, 900);
        MinimizeBox = true;
        MinimumSize = new Size(1100, 650);
        Name = "LoginForm";
        ShowIcon = true;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Ibrahim Abdo Auto Service - Login";
        WindowState = FormWindowState.Normal;

        // _loginPanel — 445x400 rectangle in the black center void
        _loginPanel.BackColor = Color.Transparent;
        _loginPanel.BorderColor = AppTheme.Gold;
        _loginPanel.BorderRadius = AppTheme.CornerRadius;
        _loginPanel.BorderThickness = 1;
        _loginPanel.FillColor = AppTheme.PanelBackground;
        _loginPanel.Name = "_loginPanel";
        _loginPanel.Size = new Size(AppTheme.PanelWidth, AppTheme.PanelHeight);
        _loginPanel.ShadowDecoration.BorderRadius = AppTheme.CornerRadius;
        _loginPanel.ShadowDecoration.Color = Color.Black;
        _loginPanel.ShadowDecoration.Depth = 24;
        _loginPanel.ShadowDecoration.Enabled = true;
        _loginPanel.ShadowDecoration.Shadow = new Padding(0, 5, 0, 8);
        _loginPanel.TabIndex = 0;

        var y = 12;

        // User icon — transparent gold outline
        _btnUserIcon.DisabledState.BorderColor = AppTheme.Gold;
        _btnUserIcon.DisabledState.CustomBorderColor = Color.Transparent;
        _btnUserIcon.DisabledState.FillColor = Color.Transparent;
        _btnUserIcon.DisabledState.ForeColor = AppTheme.Gold;
        _btnUserIcon.FillColor = Color.Transparent;
        _btnUserIcon.Font = AppTheme.IconFont;
        _btnUserIcon.ForeColor = AppTheme.Gold;
        _btnUserIcon.Location = new Point((AppTheme.PanelWidth - IconSize) / 2, y);
        _btnUserIcon.Name = "_btnUserIcon";
        _btnUserIcon.ShadowDecoration.Mode = ShadowMode.Circle;
        _btnUserIcon.ShadowDecoration.Enabled = false;
        _btnUserIcon.Size = new Size(IconSize, IconSize);
        _btnUserIcon.TabStop = false;
        _btnUserIcon.Text = "\uE77B";
        _btnUserIcon.BorderColor = AppTheme.Gold;
        _btnUserIcon.BorderThickness = 1;
        _btnUserIcon.Cursor = Cursors.Default;
        _btnUserIcon.Animated = false;
        _btnUserIcon.HoverState.FillColor = Color.Transparent;
        _btnUserIcon.HoverState.BorderColor = AppTheme.Gold;
        _btnUserIcon.HoverState.ForeColor = AppTheme.Gold;
        _btnUserIcon.PressedColor = Color.Transparent;
        _btnUserIcon.PressedDepth = 0;
        y += IconSize + AppTheme.ControlSpacing;

        // Title — tall enough so glyphs are not clipped mid-letter
        _lblWelcome.AutoSize = false;
        _lblWelcome.BackColor = Color.Transparent;
        _lblWelcome.Font = AppTheme.TitleFont;
        _lblWelcome.ForeColor = AppTheme.White;
        _lblWelcome.Location = new Point(AppTheme.SidePadding, y);
        _lblWelcome.Name = "_lblWelcome";
        _lblWelcome.Size = new Size(ContentWidth, 32);
        _lblWelcome.Text = "Welcome Back";
        _lblWelcome.TextAlign = ContentAlignment.MiddleCenter;
        _lblWelcome.UseCompatibleTextRendering = false;
        y += 32 + 2;

        // Subtitle
        _lblSubtitle.AutoSize = false;
        _lblSubtitle.BackColor = Color.Transparent;
        _lblSubtitle.Font = AppTheme.SubtitleFont;
        _lblSubtitle.ForeColor = AppTheme.Gray;
        _lblSubtitle.Location = new Point(AppTheme.SidePadding, y);
        _lblSubtitle.Name = "_lblSubtitle";
        _lblSubtitle.Size = new Size(ContentWidth, 18);
        _lblSubtitle.Text = "Please sign in to continue";
        _lblSubtitle.TextAlign = ContentAlignment.MiddleCenter;
        _lblSubtitle.UseCompatibleTextRendering = false;
        y += 18 + AppTheme.ControlSpacing;

        // Username
        ConfigureTextBox(_txtUsername, "Username", "\uE77B", y, false);
        y += AppTheme.InputHeight + AppTheme.ControlSpacing;

        // Password
        ConfigureTextBox(_txtPassword, "Password", "\uE72E", y, true);
        y += AppTheme.InputHeight + AppTheme.ControlSpacing;

        // Forgot Password (right-aligned)
        _lnkForgotPassword.ActiveLinkColor = AppTheme.GoldLight;
        _lnkForgotPassword.AutoSize = false;
        _lnkForgotPassword.BackColor = Color.Transparent;
        _lnkForgotPassword.Font = AppTheme.LabelFont;
        _lnkForgotPassword.LinkColor = AppTheme.Gold;
        _lnkForgotPassword.Location = new Point(AppTheme.SidePadding, y);
        _lnkForgotPassword.Name = "_lnkForgotPassword";
        _lnkForgotPassword.Size = new Size(ContentWidth, 18);
        _lnkForgotPassword.TabStop = true;
        _lnkForgotPassword.Text = "Forgot Password?";
        _lnkForgotPassword.TextAlign = ContentAlignment.MiddleRight;
        _lnkForgotPassword.LinkBehavior = LinkBehavior.HoverUnderline;
        _lnkForgotPassword.VisitedLinkColor = AppTheme.Gold;
        y += 18 + AppTheme.ControlSpacing;

        // LOGIN button
        _btnLogin.Animated = true;
        _btnLogin.BorderRadius = AppTheme.ButtonRadius;
        _btnLogin.DisabledState.BorderColor = Color.DarkGray;
        _btnLogin.DisabledState.CustomBorderColor = Color.DarkGray;
        _btnLogin.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
        _btnLogin.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
        _btnLogin.FillColor = AppTheme.GoldLight;
        _btnLogin.FillColor2 = AppTheme.GoldDark;
        _btnLogin.Font = AppTheme.ButtonFont;
        _btnLogin.ForeColor = AppTheme.White;
        _btnLogin.Location = new Point(AppTheme.SidePadding, y);
        _btnLogin.Name = "_btnLogin";
        _btnLogin.Size = new Size(ContentWidth, AppTheme.ButtonHeight);
        _btnLogin.Text = "LOGIN";
        _btnLogin.Cursor = Cursors.Hand;
        _btnLogin.HoverState.FillColor = AppTheme.Gold;
        _btnLogin.HoverState.FillColor2 = AppTheme.GoldDark;

        // Footer
        _lblVersion.BackColor = Color.Transparent;
        _lblVersion.Font = AppTheme.FooterFont;
        _lblVersion.ForeColor = AppTheme.Gray;
        _lblVersion.Location = new Point(AppTheme.SidePadding, AppTheme.PanelHeight - 36);
        _lblVersion.Name = "_lblVersion";
        _lblVersion.Size = new Size(ContentWidth, 12);
        _lblVersion.Text = "Version 1.0.0";
        _lblVersion.TextAlign = ContentAlignment.MiddleCenter;

        _lblCopyright.BackColor = Color.Transparent;
        _lblCopyright.Font = AppTheme.FooterFont;
        _lblCopyright.ForeColor = Color.FromArgb(140, AppTheme.Gold);
        _lblCopyright.Location = new Point(AppTheme.SidePadding, AppTheme.PanelHeight - 22);
        _lblCopyright.Name = "_lblCopyright";
        _lblCopyright.Size = new Size(ContentWidth, 14);
        _lblCopyright.Text = "\u00A9 Ibrahim Abdo Auto Service";
        _lblCopyright.TextAlign = ContentAlignment.MiddleCenter;

        _loginPanel.Controls.Add(_btnUserIcon);
        _loginPanel.Controls.Add(_lblWelcome);
        _loginPanel.Controls.Add(_lblSubtitle);
        _loginPanel.Controls.Add(_txtUsername);
        _loginPanel.Controls.Add(_txtPassword);
        _loginPanel.Controls.Add(_lnkForgotPassword);
        _loginPanel.Controls.Add(_btnLogin);
        _loginPanel.Controls.Add(_lblVersion);
        _loginPanel.Controls.Add(_lblCopyright);

        Controls.Add(_loginPanel);

        _loginPanel.ResumeLayout(false);
        _loginPanel.PerformLayout();
        ResumeLayout(false);
    }

    private void ConfigureTextBox(Guna2TextBox textBox, string placeholder, string iconGlyph, int y, bool isPassword)
    {
        textBox.BorderColor = AppTheme.InputBorder;
        textBox.BorderRadius = AppTheme.InputRadius;
        textBox.BorderThickness = 1;
        textBox.Cursor = Cursors.IBeam;
        textBox.DefaultText = string.Empty;
        textBox.DisabledState.BorderColor = Color.FromArgb(208, 208, 208);
        textBox.DisabledState.FillColor = Color.FromArgb(226, 226, 226);
        textBox.DisabledState.ForeColor = Color.FromArgb(138, 138, 138);
        textBox.DisabledState.PlaceholderForeColor = Color.FromArgb(138, 138, 138);
        textBox.FillColor = AppTheme.InputBackground;
        textBox.FocusedState.BorderColor = AppTheme.Gold;
        textBox.Font = AppTheme.InputFont;
        textBox.ForeColor = AppTheme.White;
        textBox.HoverState.BorderColor = Color.FromArgb(120, 120, 120);
        textBox.IconLeft = null;
        textBox.Location = new Point(AppTheme.SidePadding, y);
        textBox.Name = isPassword ? "_txtPassword" : "_txtUsername";
        textBox.PasswordChar = isPassword ? '\u25CF' : '\0';
        textBox.PlaceholderForeColor = AppTheme.Gray;
        textBox.PlaceholderText = placeholder;
        textBox.SelectedText = string.Empty;
        textBox.Size = new Size(ContentWidth, AppTheme.InputHeight);
        textBox.TabIndex = isPassword ? 4 : 3;

        if (isPassword)
        {
            textBox.IconRight = null;
            textBox.IconRightSize = new Size(16, 16);
            textBox.PasswordChar = '\u25CF';
            textBox.UseSystemPasswordChar = true;
        }

        textBox.Tag = iconGlyph;
    }
}
