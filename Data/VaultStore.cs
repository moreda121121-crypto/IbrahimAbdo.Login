namespace IbrahimAbdo.Login.Data;

internal enum VaultMovementType
{
    Income,
    Expense,
    Transfer,
    Withdraw,
    Deposit
}

internal sealed class VaultMovementRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime At { get; set; } = DateTime.Now;
    public VaultMovementType Type { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string By { get; set; } = "admin";
}

internal sealed class VaultState
{
    public decimal OpeningBalance { get; set; } = 5000m;
    public List<VaultMovementRecord> Movements { get; set; } = [];
}

internal static class VaultStore
{
    private static readonly string FilePath =
        Path.Combine(AppContext.BaseDirectory, "Data", "vault.json");

    private static VaultState _state = new();

    public static decimal OpeningBalance => _state.OpeningBalance;
    public static IReadOnlyList<VaultMovementRecord> Movements => _state.Movements;

    public static decimal TotalIncome =>
        _state.Movements.Where(m => IsIncomeLike(m.Type)).Sum(m => m.Amount);

    public static decimal TotalExpense =>
        _state.Movements.Where(m => IsExpenseLike(m.Type)).Sum(m => m.Amount);

    public static decimal CurrentBalance => OpeningBalance + TotalIncome - TotalExpense;

    public static int MovementCount => _state.Movements.Count;

    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var loaded = System.Text.Json.JsonSerializer.Deserialize<VaultState>(json);
                if (loaded is not null && loaded.Movements.Count > 0)
                {
                    _state = loaded;
                    return;
                }
            }
        }
        catch
        {
            // seed
        }

        Seed();
        Save();
    }

    public static IEnumerable<VaultMovementRecord> Paged(int page, int pageSize) =>
        _state.Movements.OrderByDescending(m => m.At).Skip(page * pageSize).Take(pageSize);

    public static void Add(VaultMovementType type, string description, decimal amount, string by = "admin")
    {
        if (amount <= 0 || string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        var balance = CurrentBalance + (IsExpenseLike(type) ? -amount : amount);
        _state.Movements.Insert(0, new VaultMovementRecord
        {
            Type = type,
            Description = description.Trim(),
            Amount = amount,
            BalanceAfter = balance,
            By = by,
            At = DateTime.Now
        });
        Save();
    }

    public static string TypeLabel(VaultMovementType type) => type switch
    {
        VaultMovementType.Income => "إيراد",
        VaultMovementType.Expense => "مصروف",
        VaultMovementType.Transfer => "تحويل",
        VaultMovementType.Withdraw => "سحب",
        VaultMovementType.Deposit => "إيداع",
        _ => type.ToString()
    };

    public static bool IsIncomeLike(VaultMovementType type) =>
        type is VaultMovementType.Income or VaultMovementType.Deposit;

    public static bool IsExpenseLike(VaultMovementType type) =>
        type is VaultMovementType.Expense or VaultMovementType.Withdraw or VaultMovementType.Transfer;

    public static void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(FilePath, System.Text.Json.JsonSerializer.Serialize(_state,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // ignore
        }
    }

    private static void Seed()
    {
        // Screenshot KPIs: opening 5,000 | income 240,400 | expense 221,950 | balance 18,450 | ~156 rows
        _state = new VaultState { OpeningBalance = 5000m };
        var list = new List<(DateTime at, VaultMovementType type, string desc, decimal amount, string by)>();

        // Recent visible rows
        list.Add((DateTime.Today.AddHours(14).AddMinutes(44), VaultMovementType.Income, "تحصيل فاتورة #5858", 950m, "admin"));
        list.Add((DateTime.Today.AddHours(10), VaultMovementType.Expense, "شراء قطع غيار", 3200m, "admin"));
        list.Add((DateTime.Today.AddDays(-1).AddHours(12), VaultMovementType.Income, "تحصيل فاتورة #5841", 1800m, "sara"));
        list.Add((DateTime.Today.AddDays(-2).AddHours(17), VaultMovementType.Withdraw, "سحب مصروفات ورشة", 1500m, "admin"));
        list.Add((DateTime.Today.AddDays(-2).AddHours(14), VaultMovementType.Income, "تحصيل فاتورة #5802", 2400m, "mohamed"));
        list.Add((DateTime.Today.AddDays(-3).AddHours(16), VaultMovementType.Expense, "فاتورة كهرباء", 890m, "admin"));
        list.Add((DateTime.Today.AddDays(-3).AddHours(11), VaultMovementType.Income, "تحصيل فاتورة #5788", 1250m, "sara"));
        list.Add((DateTime.Today.AddDays(-4).AddHours(13), VaultMovementType.Transfer, "تحويل لصندوق الفرع", 2000m, "admin"));

        var usedIncome = list.Where(x => IsIncomeLike(x.type)).Sum(x => x.amount);
        var usedExpense = list.Where(x => IsExpenseLike(x.type)).Sum(x => x.amount);
        var needIncome = 240400m - usedIncome;
        var needExpense = 221950m - usedExpense;

        // 148 more rows (~156 total)
        for (var i = 0; i < 148; i++)
        {
            var income = i % 5 != 0;
            var at = DateTime.Today.AddDays(-(5 + i / 3)).AddHours(9 + (i % 8));
            if (income && needIncome > 0)
            {
                var amt = Math.Min(1200m + (i % 20) * 35m, needIncome);
                needIncome -= amt;
                list.Add((at, VaultMovementType.Income, $"تحصيل فاتورة #{5700 - i}", amt, i % 2 == 0 ? "admin" : "sara"));
            }
            else if (needExpense > 0)
            {
                var amt = Math.Min(900m + (i % 15) * 40m, needExpense);
                needExpense -= amt;
                list.Add((at, VaultMovementType.Expense, $"مصروف تشغيلي #{200 + i}", amt, i % 2 == 0 ? "admin" : "mohamed"));
            }
        }

        if (needIncome > 0)
        {
            list.Add((DateTime.Today.AddDays(-60), VaultMovementType.Income, "تسوية إيرادات سابقة", needIncome, "admin"));
        }

        if (needExpense > 0)
        {
            list.Add((DateTime.Today.AddDays(-60), VaultMovementType.Expense, "تسوية مصروفات سابقة", needExpense, "admin"));
        }

        var bal = _state.OpeningBalance;
        var records = new List<VaultMovementRecord>();
        foreach (var row in list.OrderBy(x => x.at))
        {
            bal += IsExpenseLike(row.type) ? -row.amount : row.amount;
            records.Add(new VaultMovementRecord
            {
                At = row.at,
                Type = row.type,
                Description = row.desc,
                Amount = row.amount,
                BalanceAfter = bal,
                By = row.by
            });
        }

        _state.Movements = records.OrderByDescending(m => m.At).ToList();
    }
}
