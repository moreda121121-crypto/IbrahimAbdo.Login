namespace IbrahimAbdo.Login.Data;

internal sealed class PurchaseInvoiceItemRecord
{
    public string? ProductId { get; set; }
    public string Name { get; set; } = "";
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total => Qty * UnitPrice;
}

internal sealed class PurchaseInvoiceRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Number { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string SupplierName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Notes { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal Paid { get; set; }
    public List<PurchaseInvoiceItemRecord> Items { get; set; } = [];
}

internal static class PurchaseInvoiceStore
{
    private static readonly List<PurchaseInvoiceRecord> Items = [];
    private static readonly string FilePath =
        Path.Combine(AppContext.BaseDirectory, "Data", "purchase-invoices.json");
    private static int _sequence;

    public static IReadOnlyList<PurchaseInvoiceRecord> All => Items;

    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var loaded = System.Text.Json.JsonSerializer.Deserialize<List<PurchaseInvoiceRecord>>(json);
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
            // empty
        }

        Items.Clear();
        _sequence = 0;
    }

    public static string NextNumber()
    {
        _sequence++;
        return $"PUR-{_sequence:D4}";
    }

    public static void Add(PurchaseInvoiceRecord invoice)
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

            File.WriteAllText(FilePath, System.Text.Json.JsonSerializer.Serialize(Items,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // ignore
        }
    }
}
