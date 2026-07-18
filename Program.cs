using IbrahimAbdo.Login.Forms;

namespace IbrahimAbdo.Login;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        ApplicationConfiguration.Initialize();
        Application.Run(new LoginForm());
    }
}
