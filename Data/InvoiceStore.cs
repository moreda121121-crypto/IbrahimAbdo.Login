namespace IbrahimAbdo.Login.Data;

internal sealed class InvoiceItemRecord
{
    public string Name { get; set; } = "";
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total => Qty * UnitPrice;
}

internal sealed class InvoiceRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Number { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string PlateLetters { get; set; } = "";
    public string PlateNumber { get; set; } = "";
    public string CarModel { get; set; } = "";
    public string ChassisNumber { get; set; } = "";
    public string Odometer { get; set; } = "";
    public string Technician { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public string DiscountUnit { get; set; } = "%";
    public decimal Tax { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal Paid { get; set; }
    public decimal Remaining { get; set; }
    public List<InvoiceItemRecord> Items { get; set; } = [];
}

internal static class InvoiceStore
{
    private static readonly List<InvoiceRecord> Items = [];
    private static readonly string FilePath =
        Path.Combine(AppContext.BaseDirectory, "Data", "invoices.json");
    private static int _sequence;

    public static IReadOnlyList<InvoiceRecord> All => Items;

    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var loaded = System.Text.Json.JsonSerializer.Deserialize<List<InvoiceRecord>>(json);
                if (loaded is not null)
                {
                    Items.Clear();
                    Items.AddRange(loaded);
                    _sequence = Items
                        .Select(i =>
                        {
                            var parts = i.Number.Split('-');
                            return parts.Length > 1 && int.TryParse(parts[^1], out var n) ? n : 0;
                        })
                        .DefaultIfEmpty(0)
                        .Max();
                    return;
                }
            }
        }
        catch
        {
            // start empty
        }

        Items.Clear();
        _sequence = 0;
    }

    public static string NextNumber()
    {
        _sequence++;
        return $"INV-{_sequence:D4}";
    }

    public static void Add(InvoiceRecord invoice)
    {
        if (string.IsNullOrWhiteSpace(invoice.Number))
        {
            invoice.Number = NextNumber();
        }

        Items.Insert(0, invoice);
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
}
