using IbrahimAbdo.Login.Forms;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        WindowTheme.EnableAppDarkMode();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        ApplicationConfiguration.Initialize();
        Application.Run(new LoginForm());
    }
}
