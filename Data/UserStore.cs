namespace IbrahimAbdo.Login.Data;

internal sealed class UserRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DisplayName { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "موظف";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Today;
}

internal static class UserStore
{
    private static readonly List<UserRecord> Items = [];
    private static readonly string FilePath =
        Path.Combine(AppContext.BaseDirectory, "Data", "users.json");

    public static IReadOnlyList<UserRecord> All => Items;
    public static int TotalUsers => Items.Count;
    public static int ActiveUsers => Items.Count(u => u.IsActive);

    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var loaded = System.Text.Json.JsonSerializer.Deserialize<List<UserRecord>>(json);
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
            // fall through to seed
        }

        Seed();
        Save();
    }

    public static bool Validate(string username, string password)
    {
        return Items.Any(u =>
            u.IsActive &&
            u.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase) &&
            u.Password == password);
    }

    public static bool UsernameExists(string username, string? excludeId = null)
    {
        return Items.Any(u =>
            u.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase) &&
            (excludeId is null || u.Id != excludeId));
    }

    public static void Add(UserRecord user)
    {
        Items.Insert(0, user);
        Save();
    }

    public static void Remove(string id)
    {
        Items.RemoveAll(u => u.Id == id);
        Save();
    }

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
            // ignore persistence errors for now
        }
    }

    public static IEnumerable<UserRecord> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Items;
        }

        var q = query.Trim();
        return Items.Where(u =>
            u.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            u.Username.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            u.Role.Contains(q, StringComparison.OrdinalIgnoreCase));
    }

    private static void Seed()
    {
        Items.Clear();
        Items.Add(new UserRecord
        {
            DisplayName = "مدير النظام",
            Username = "123",
            Password = "123",
            Role = "مدير",
            IsActive = true,
            CreatedAt = DateTime.Today
        });
    }
}
