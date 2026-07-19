using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Controls;

internal class GoldTextBox : UserControl
{
    private readonly TextBox _textBox;
    private readonly Label _leadingIcon;
    private readonly Button _toggleButton;
    private float _focusGlow;
    private bool _isPasswordMode;

    public event EventHandler? LoginSubmit;

    public string InputText
    {
        get => _textBox.Text;
        set => _textBox.Text = value ?? string.Empty;
    }

    public bool UseSystemPasswordChar
    {
        get => _textBox.UseSystemPasswordChar;
        set => _textBox.UseSystemPasswordChar = value;
    }

    public bool IsPasswordField
    {
        get => _isPasswordMode;
        set
        {
            _isPasswordMode = value;
            _toggleButton.Visible = value;
            _textBox.UseSystemPasswordChar = value && _toggleButton.Tag?.Equals(true) != true;
            PerformLayout();
        }
    }

    public string? PlaceholderText
    {
        get => _textBox.PlaceholderText;
        set => _textBox.PlaceholderText = value ?? string.Empty;
    }

    public string? LeadingGlyph
    {
        get => _leadingIcon.Text;
        set => _leadingIcon.Text = value;
    }

    public GoldTextBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);

        _leadingIcon = new Label
        {
            AutoSize = false,
            Font = AppTheme.IconFont,
            ForeColor = AppTheme.MutedIcon,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent,
            Text = "\uE77B"
        };

        _textBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = AppTheme.InputBackground,
            ForeColor = AppTheme.White,
            Font = AppTheme.InputFont
        };

        _toggleButton = new Button
        {
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.InputBackground,
            ForeColor = AppTheme.MutedIcon,
            Font = AppTheme.IconFont,
            Text = "\uE890",
            Cursor = Cursors.Hand,
            TabStop = false,
            Visible = false
        };
        _toggleButton.FlatAppearance.BorderSize = 0;
        _toggleButton.Click += (_, _) => TogglePasswordVisibility();

        _textBox.GotFocus += (_, _) => SetFocused(true);
        _textBox.LostFocus += (_, _) => SetFocused(false);
        _textBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                LoginSubmit?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        };

        Controls.Add(_textBox);
        Controls.Add(_leadingIcon);
        Controls.Add(_toggleButton);

        Font = AppTheme.InputFont;
        Size = new Size(360, AppTheme.InputHeight);
    }

    private void SetFocused(bool focused)
    {
        _leadingIcon.ForeColor = focused ? AppTheme.Gold : AppTheme.MutedIcon;
        AnimateFocus(focused ? 1F : 0F);
    }

    private void AnimateFocus(float target)
    {
        var animator = new AnimationHelper();
        var start = _focusGlow;
        animator.Animate(start, target, 180, value =>
        {
            _focusGlow = value;
            Invalidate();
        }, animator.Dispose);
    }

    private void TogglePasswordVisibility()
    {
        var visible = _toggleButton.Tag?.Equals(true) == true;
        visible = !visible;
        _toggleButton.Tag = visible;
        _textBox.UseSystemPasswordChar = _isPasswordMode && !visible;
        _toggleButton.Text = visible ? "\uE7B3" : "\uE890";
        _toggleButton.ForeColor = visible ? AppTheme.Gold : AppTheme.MutedIcon;
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (_textBox is null)
        {
            return;
        }

        _textBox.Font = Font;
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);

        if (_leadingIcon is null || _textBox is null || _toggleButton is null)
        {
            return;
        }

        _leadingIcon.SetBounds(16, 0, 24, Height);
        var rightPadding = _toggleButton.Visible ? 44 : 16;
        _textBox.SetBounds(46, (Height - _textBox.PreferredHeight) / 2, Width - 46 - rightPadding, _textBox.PreferredHeight);

        if (_toggleButton.Visible)
        {
            _toggleButton.SetBounds(Width - 40, 0, 32, Height);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        GraphicsHelper.EnableHighQuality(graphics);

        var bounds = new RectangleF(0.5F, 0.5F, Width - 1F, Height - 1F);
        using var path = GraphicsHelper.CreateRoundedRectangle(bounds, AppTheme.InputRadius);

        using (var fill = new SolidBrush(AppTheme.InputBackground))
        {
            graphics.FillPath(fill, path);
        }

        if (_focusGlow > 0F)
        {
            GraphicsHelper.DrawGlow(graphics, bounds, Color.FromArgb((int)(60 * _focusGlow), AppTheme.Gold), AppTheme.InputRadius, 3, 1F);
        }

        var borderColor = _focusGlow > 0F
            ? Color.FromArgb((int)(220 + (35 * _focusGlow)), AppTheme.Gold)
            : AppTheme.InputBorder;

        using (var border = new Pen(borderColor, 1F) { Alignment = PenAlignment.Inset })
        {
            graphics.DrawPath(border, path);
        }
    }
}
