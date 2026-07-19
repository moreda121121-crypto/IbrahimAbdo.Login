namespace IbrahimAbdo.Login.Helpers;

internal static class AppIcon
{
    private static Icon? _cached;

    public static Icon? Current
    {
        get
        {
            if (_cached is not null)
            {
                return _cached;
            }

            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
                if (File.Exists(path))
                {
                    _cached = new Icon(path);
                    return _cached;
                }

                var exe = Environment.ProcessPath;
                if (!string.IsNullOrWhiteSpace(exe))
                {
                    _cached = Icon.ExtractAssociatedIcon(exe);
                }
            }
            catch
            {
                // keep default
            }

            return _cached;
        }
    }
}
