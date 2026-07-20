namespace IbrahimAbdo.Login.Data;

internal sealed class TechnicianRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Today;
}

internal static class TechnicianStore
{
    private static readonly List<TechnicianRecord> Items = [];
    private static readonly string FilePath =
        Path.Combine(AppContext.BaseDirectory, "Data", "technicians.json");

    public static IReadOnlyList<TechnicianRecord> All => Items;
    public static int Total => Items.Count;

    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var loaded = System.Text.Json.JsonSerializer.Deserialize<List<TechnicianRecord>>(json);
                if (loaded is { Count: > 0 })
                {
                    Items.Clear();
                    Items.AddRange(loaded);
                    return;
                }
            }
        }
        catch
        {
            // seed below
        }

        Seed();
        Save();
    }

    public static IEnumerable<TechnicianRecord> Search(string query)
    {
        IEnumerable<TechnicianRecord> q = Items.OrderBy(t => t.Name);
        if (string.IsNullOrWhiteSpace(query))
        {
            return q;
        }

        query = query.Trim();
        return q.Where(t =>
            t.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            t.Phone.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            t.Address.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    public static void Add(TechnicianRecord tech)
    {
        Items.Insert(0, tech);
        Save();
    }

    public static void Remove(string id)
    {
        Items.RemoveAll(t => t.Id == id);
        Save();
    }

    public static IReadOnlyList<string> Names() =>
        Items.Select(t => t.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();

    public static void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = System.Text.Json.JsonSerializer.Serialize(Items, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // ignore
        }
    }

    private static void Seed()
    {
        Items.Clear();
        Items.AddRange(
        [
            new TechnicianRecord { Name = "أحمد محمد", Phone = "01001234567", Address = "القاهرة" },
            new TechnicianRecord { Name = "محمد علي", Phone = "01009876543", Address = "الجيزة" },
            new TechnicianRecord { Name = "إبراهيم حسن", Phone = "01112345678", Address = "الشروق" },
        ]);
    }
}
