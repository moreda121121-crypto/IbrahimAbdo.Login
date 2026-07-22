namespace IbrahimAbdo.Login.Data;

internal sealed class SupplierRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Today;
}

internal static class SupplierStore
{
    private static readonly List<SupplierRecord> Items = [];
    private static readonly string FilePath =
        Path.Combine(AppContext.BaseDirectory, "Data", "suppliers.json");

    public static IReadOnlyList<SupplierRecord> All => Items;
    public static int Total => Items.Count;

    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var loaded = System.Text.Json.JsonSerializer.Deserialize<List<SupplierRecord>>(json);
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

    public static void Add(SupplierRecord supplier)
    {
        Items.Insert(0, supplier);
        Save();
    }

    public static void Remove(string id)
    {
        Items.RemoveAll(s => s.Id == id);
        Save();
    }

    public static SupplierRecord? FindByName(string name) =>
        Items.FirstOrDefault(s => string.Equals(s.Name?.Trim(), name?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<string> Names() =>
        Items.Select(s => s.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();

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
            // ignore persistence errors
        }
    }

    private static void Seed()
    {
        Items.Clear();
        Items.AddRange(
        [
            new SupplierRecord { Name = "شركة النور لقطع الغيار", Phone = "" },
            new SupplierRecord { Name = "موتور تك", Phone = "" },
            new SupplierRecord { Name = "أوتو بارتس مصر", Phone = "" },
            new SupplierRecord { Name = "الوكيل المعتمد", Phone = "" },
            new SupplierRecord { Name = "مورد محلي", Phone = "" },
        ]);
    }
}
