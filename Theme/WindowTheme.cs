using System.Runtime.InteropServices;

namespace IbrahimAbdo.Login.Theme;

/// <summary>Applies a dark, app-colored title bar (caption) to top-level windows on Windows 10/11.</summary>
internal static class WindowTheme
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_CAPTION_COLOR = 35;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true)]
    private static extern int SetPreferredAppMode(int mode);

    [DllImport("uxtheme.dll", EntryPoint = "#104")]
    private static extern void FlushMenuThemes();

    /// <summary>Forces process-wide dark mode so native scrollbars/controls render dark (Windows 10 1809+).</summary>
    public static void EnableAppDarkMode()
    {
        try
        {
            // 2 = ForceDark
            SetPreferredAppMode(2);
            FlushMenuThemes();
        }
        catch
        {
            // undocumented API not available on this OS; ignore
        }
    }

    /// <summary>Hooks the form so its title bar matches the app background whenever the handle is (re)created.</summary>
    public static void Attach(Form form)
    {
        form.HandleCreated += (_, _) => ApplyDarkTitleBar(form.Handle);
        if (form.IsHandleCreated)
        {
            ApplyDarkTitleBar(form.Handle);
        }
    }

    public static void ApplyDarkTitleBar(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var useDark = 1;
            DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));

            var bg = InvoiceTheme.Background;
            var colorRef = bg.R | (bg.G << 8) | (bg.B << 16);
            DwmSetWindowAttribute(handle, DWMWA_CAPTION_COLOR, ref colorRef, sizeof(int));
        }
        catch
        {
            // title-bar theming not supported on this OS; ignore
        }
    }
}
