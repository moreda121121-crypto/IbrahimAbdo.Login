using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Helpers;

/// <summary>
/// Shared dark/gold pagination button styling matching the Vault screenshot.
/// Visual design only — callers keep their own click handlers and page logic.
/// </summary>
internal static class PaginationStyle
{
    public static readonly Color ButtonFill = Color.FromArgb(28, 28, 28);
    public static readonly Color ButtonFillHover = Color.FromArgb(40, InvoiceTheme.Gold);
    public static readonly Color DisabledFill = Color.FromArgb(22, 22, 22);
    public static readonly Color DisabledText = Color.FromArgb(120, InvoiceTheme.Gold);
    public const int ButtonHeight = 32;
    public const int BorderRadius = 6;
    public const int Spacing = 4;

    /// <summary>Compact page-number / arrow button (Vault-style strip).</summary>
    public static Guna2Button CreatePageButton(string text, bool active = false, bool enabled = true)
    {
        var wide = text.Length > 2;
        var btn = new Guna2Button
        {
            Text = text,
            Size = new Size(wide ? 40 : 32, ButtonHeight),
            BorderRadius = BorderRadius,
            Margin = new Padding(Spacing, 0, Spacing, 0),
            Font = InvoiceTheme.SmallFont,
            Cursor = enabled && !active ? Cursors.Hand : (enabled ? Cursors.Hand : Cursors.Default),
            Enabled = enabled,
            Animated = false
        };
        Apply(btn, active, enabled);
        return btn;
    }

    /// <summary>Previous / Next nav button (keeps Arabic labels like السابق / التالي).</summary>
    public static Guna2Button CreateNavButton(string text, EventHandler onClick)
    {
        var btn = new Guna2Button
        {
            Text = text,
            Font = InvoiceTheme.SmallFont,
            Height = ButtonHeight,
            MinimumSize = new Size(88, ButtonHeight),
            Padding = new Padding(14, 0, 14, 0),
            BorderRadius = BorderRadius,
            Margin = new Padding(Spacing, 0, Spacing, 0),
            Cursor = Cursors.Hand,
            Animated = false
        };
        var textW = TextRenderer.MeasureText(text, btn.Font).Width;
        btn.Width = Math.Max(btn.MinimumSize.Width, textW + 28);
        Apply(btn, active: false, enabled: true);
        btn.Click += onClick;
        return btn;
    }

    public static void Apply(Guna2Button btn, bool active, bool enabled)
    {
        if (active)
        {
            btn.FillColor = InvoiceTheme.Gold;
            btn.ForeColor = Color.Black;
            btn.BorderColor = InvoiceTheme.Gold;
            btn.BorderThickness = 1;
            btn.HoverState.FillColor = InvoiceTheme.GoldDark;
            btn.HoverState.ForeColor = Color.Black;
            btn.HoverState.BorderColor = InvoiceTheme.GoldDark;
            btn.DisabledState.FillColor = InvoiceTheme.Gold;
            btn.DisabledState.ForeColor = Color.Black;
            btn.DisabledState.BorderColor = InvoiceTheme.Gold;
            return;
        }

        if (!enabled)
        {
            btn.FillColor = DisabledFill;
            btn.ForeColor = DisabledText;
            btn.BorderColor = Color.FromArgb(80, InvoiceTheme.Gold);
            btn.BorderThickness = 1;
            btn.HoverState.FillColor = DisabledFill;
            btn.HoverState.ForeColor = DisabledText;
            btn.HoverState.BorderColor = Color.FromArgb(80, InvoiceTheme.Gold);
            btn.DisabledState.FillColor = DisabledFill;
            btn.DisabledState.ForeColor = DisabledText;
            btn.DisabledState.BorderColor = Color.FromArgb(80, InvoiceTheme.Gold);
            return;
        }

        btn.FillColor = ButtonFill;
        btn.ForeColor = InvoiceTheme.Gold;
        btn.BorderColor = InvoiceTheme.Gold;
        btn.BorderThickness = 1;
        btn.HoverState.FillColor = ButtonFillHover;
        btn.HoverState.ForeColor = InvoiceTheme.Gold;
        btn.HoverState.BorderColor = InvoiceTheme.Gold;
        btn.DisabledState.FillColor = DisabledFill;
        btn.DisabledState.ForeColor = DisabledText;
        btn.DisabledState.BorderColor = Color.FromArgb(80, InvoiceTheme.Gold);
    }

    public static Label CreatePageLabel() =>
        new()
        {
            AutoSize = true,
            ForeColor = InvoiceTheme.Gold,
            Font = InvoiceTheme.SmallFont,
            Margin = new Padding(10, 8, 10, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "صفحة 1"
        };

    public static FlowLayoutPanel CreateBar() =>
        new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(4, 2, 0, 0)
        };
}
