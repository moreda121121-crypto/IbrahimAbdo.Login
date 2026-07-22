namespace IbrahimAbdo.Login.Data;

internal sealed class ProductRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Barcode { get; set; } = "";
    public string Category { get; set; } = "";
    public string Supplier { get; set; } = "";
    public string Unit { get; set; } = "قطعة";
    public int Quantity { get; set; }
    public int MinStock { get; set; } = 5;
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public string? ImagePath { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public decimal TotalSoldQty { get; set; }

    public decimal ProfitPerItem => SellingPrice - PurchasePrice;
    public decimal TotalProfit => ProfitPerItem * Quantity;
    public decimal InventoryValue => PurchasePrice * Quantity;
    public decimal PotentialSales => SellingPrice * Quantity;
    public bool IsLowStock => Quantity <= MinStock;
}

internal sealed class StockMovementRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Type { get; set; } = "Stock In"; // Stock In | Stock Out
    public int Quantity { get; set; }
    public DateTime At { get; set; } = DateTime.Now;
    public string Note { get; set; } = "";
    public string InvoiceNumber { get; set; } = "";
}

internal static class ProductStore
{
    private static readonly List<ProductRecord> Items = [];
    private static readonly List<StockMovementRecord> Movements = [];

    private static readonly string ProductsPath =
        Path.Combine(AppContext.BaseDirectory, "Data", "products.json");
    private static readonly string MovementsPath =
        Path.Combine(AppContext.BaseDirectory, "Data", "stock-movements.json");

    public static IReadOnlyList<ProductRecord> All => Items;
    public static IReadOnlyList<StockMovementRecord> AllMovements => Movements;

    public static readonly string[] Categories =
    [
        "زيوت", "فلاتر", "فرامل", "كهرباء", "تعليق", "تبريد", "إكسسوارات", "أخرى"
    ];

    public static readonly string[] Suppliers =
    [
        "شركة النور لقطع الغيار", "موتور تك", "أوتو بارتس مصر", "الوكيل المعتمد", "مورد محلي"
    ];

    public static readonly string[] Units = ["قطعة", "علبة", "لتر", "متر", "طقم"];

    public static void Load()
    {
        try
        {
            if (File.Exists(ProductsPath))
            {
                var json = File.ReadAllText(ProductsPath);
                var loaded = System.Text.Json.JsonSerializer.Deserialize<List<ProductRecord>>(json);
                if (loaded is { Count: > 0 })
                {
                    Items.Clear();
                    Items.AddRange(loaded);
                    LoadMovements();
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
        SaveMovements();
    }

    private static void LoadMovements()
    {
        try
        {
            if (File.Exists(MovementsPath))
            {
                var json = File.ReadAllText(MovementsPath);
                var loaded = System.Text.Json.JsonSerializer.Deserialize<List<StockMovementRecord>>(json);
                if (loaded is not null)
                {
                    Movements.Clear();
                    Movements.AddRange(loaded);
                    return;
                }
            }
        }
        catch
        {
            // empty movements
        }

        Movements.Clear();
    }

    public static IEnumerable<ProductRecord> Search(string query, string? category, string? supplier)
    {
        IEnumerable<ProductRecord> q = Items;
        if (!string.IsNullOrWhiteSpace(category) && category != "الكل")
        {
            q = q.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(supplier) && supplier != "الكل")
        {
            q = q.Where(p => p.Supplier.Equals(supplier, StringComparison.OrdinalIgnoreCase));
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return q.OrderByDescending(p => p.UpdatedAt);
        }

        query = query.Trim();
        return q.Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Code.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Barcode.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.UpdatedAt);
    }

    // Net Profit = Total Sales Price − Total Purchase Cost (on sold units)
    public static decimal TotalProductSales => Items.Sum(p => p.SellingPrice * Math.Max(p.TotalSoldQty, 0));
    public static decimal TotalPurchaseCost => Items.Sum(p => p.PurchasePrice * Math.Max(p.TotalSoldQty, 0));
    public static decimal NetProfit => TotalProductSales - TotalPurchaseCost;
    public static decimal InventoryValue => Items.Sum(p => p.InventoryValue);
    public static int ProductCount => Items.Count;
    public static int LowStockCount => Items.Count(p => p.IsLowStock);

    public static IEnumerable<ProductRecord> LowStockProducts() =>
        Items.Where(p => p.IsLowStock).OrderBy(p => p.Quantity);

    public static IEnumerable<StockMovementRecord> RecentMovements(int take = 12) =>
        Movements.OrderByDescending(m => m.At).Take(take);

    public static IEnumerable<StockMovementRecord> MovementsFor(string productId) =>
        Movements.Where(m => m.ProductId == productId).OrderByDescending(m => m.At);

    public static bool CodeExists(string code, string? excludeId = null) =>
        Items.Any(p => p.Code.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase) &&
                       (excludeId is null || p.Id != excludeId));

    public static void Add(ProductRecord product)
    {
        product.UpdatedAt = DateTime.Now;
        Items.Insert(0, product);
        if (product.Quantity > 0)
        {
            AddMovement(product, "Stock In", product.Quantity, "إضافة صنف جديد");
        }

        Save();
        SaveMovements();
    }

    public static void Update(ProductRecord product, int previousQty)
    {
        product.UpdatedAt = DateTime.Now;
        var delta = product.Quantity - previousQty;
        if (delta > 0)
        {
            AddMovement(product, "Stock In", delta, "تحديث كمية");
        }
        else if (delta < 0)
        {
            AddMovement(product, "Stock Out", Math.Abs(delta), "تحديث كمية");
        }

        Save();
        SaveMovements();
    }

    public static void Remove(string id)
    {
        var p = Items.FirstOrDefault(x => x.Id == id);
        if (p is null)
        {
            return;
        }

        Items.RemoveAll(x => x.Id == id);
        Save();
    }

    public static ProductRecord? Find(string id) => Items.FirstOrDefault(p => p.Id == id);

    public static ProductRecord? FindByCodeOrName(string codeOrName)
    {
        if (string.IsNullOrWhiteSpace(codeOrName))
        {
            return null;
        }

        return Items.FirstOrDefault(p =>
            p.Code.Equals(codeOrName, StringComparison.OrdinalIgnoreCase) ||
            p.Name.Equals(codeOrName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Deduct stock and accumulate sold qty after an invoice is saved.</summary>
    public static bool ApplySale(string? productId, string productName, int qty, out string? error, string invoiceNumber = "")
    {
        error = null;
        if (qty <= 0)
        {
            return true;
        }

        var product = (!string.IsNullOrWhiteSpace(productId) ? Find(productId) : null)
                      ?? FindByCodeOrName(productName);
        if (product is null)
        {
            // Free-text invoice line (not from catalog) — skip stock
            return true;
        }

        if (product.Quantity < qty)
        {
            error = $"الكمية غير كافية للصنف «{product.Name}» (المتاح: {product.Quantity})";
            return false;
        }

        product.Quantity -= qty;
        product.TotalSoldQty += qty;
        product.UpdatedAt = DateTime.Now;
        AddMovement(product, "Stock Out", qty, "بيع من فاتورة", invoiceNumber);
        Save();
        SaveMovements();
        return true;
    }

    /// <summary>Increase stock after a purchase invoice is saved.</summary>
    public static bool ApplyPurchase(string? productId, string productName, int qty, decimal unitCost, out string? error)
    {
        error = null;
        if (qty <= 0)
        {
            return true;
        }

        var product = (!string.IsNullOrWhiteSpace(productId) ? Find(productId) : null)
                      ?? FindByCodeOrName(productName);
        if (product is null)
        {
            error = $"الصنف «{productName}» غير موجود في المخزون";
            return false;
        }

        product.Quantity += qty;
        if (unitCost > 0)
        {
            product.PurchasePrice = unitCost;
        }

        product.UpdatedAt = DateTime.Now;
        AddMovement(product, "Stock In", qty, "شراء من فاتورة شراء");
        Save();
        SaveMovements();
        return true;
    }

    private static void AddMovement(ProductRecord product, string type, int qty, string note, string invoiceNumber = "")
    {
        Movements.Insert(0, new StockMovementRecord
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Type = type,
            Quantity = qty,
            At = DateTime.Now,
            Note = note,
            InvoiceNumber = invoiceNumber
        });
    }

    public static void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(ProductsPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = System.Text.Json.JsonSerializer.Serialize(Items, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(ProductsPath, json);
        }
        catch
        {
            // ignore
        }
    }

    private static void SaveMovements()
    {
        try
        {
            var dir = Path.GetDirectoryName(MovementsPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = System.Text.Json.JsonSerializer.Serialize(Movements, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(MovementsPath, json);
        }
        catch
        {
            // ignore
        }
    }

    private static void Seed()
    {
        Items.Clear();
        Movements.Clear();

        ProductRecord P(string code, string name, string cat, string supplier, int qty, int min, decimal buy, decimal sell, decimal sold) =>
            new()
            {
                Code = code,
                Name = name,
                Barcode = $"62{code.PadLeft(10, '0')}",
                Category = cat,
                Supplier = supplier,
                Unit = cat == "زيوت" ? "لتر" : "قطعة",
                Quantity = qty,
                MinStock = min,
                PurchasePrice = buy,
                SellingPrice = sell,
                TotalSoldQty = sold,
                UpdatedAt = DateTime.Now.AddDays(-Random.Shared.Next(0, 20))
            };

        Items.AddRange(
        [
            P("OIL-5W30", "زيت موتور 5W-30", "زيوت", "شركة النور لقطع الغيار", 48, 10, 280, 450, 120),
            P("FLT-OIL", "فلتر زيت", "فلاتر", "موتور تك", 8, 15, 45, 120, 80),
            P("FLT-AIR", "فلتر هواء", "فلاتر", "موتور تك", 22, 8, 60, 180, 55),
            P("BRK-PAD-F", "تيل فرامل أمامي", "فرامل", "أوتو بارتس مصر", 4, 6, 420, 950, 30),
            P("BRK-DISC", "диск فرامل", "فرامل", "أوتو بارتس مصر", 12, 4, 650, 1200, 18),
            P("SPK-NGK", "بوجيهات NGK", "كهرباء", "الوكيل المعتمد", 36, 12, 55, 85, 90),
            P("BAT-70A", "بطارية 70 أمبير", "كهرباء", "الوكيل المعتمد", 3, 5, 1800, 2600, 14),
            P("RAD-COOL", "مياه رادياتير", "تبريد", "مورد محلي", 60, 20, 35, 75, 200),
            P("ALT-BELT", "سير دينامو", "تعليق", "موتور تك", 15, 5, 90, 220, 40),
            P("WSH-KIT", "طقم مساحات", "إكسسوارات", "مورد محلي", 2, 8, 70, 160, 25),
            P("OIL-0W20", "زيت موتور 0W-20", "زيوت", "شركة النور لقطع الغيار", 30, 10, 320, 520, 70),
            P("CAB-FILT", "فلتر مكيف", "فلاتر", "أوتو بارتس مصر", 18, 6, 80, 190, 45),
        ]);

        foreach (var p in Items.Take(6))
        {
            AddMovement(p, "Stock In", Math.Max(1, p.Quantity / 2), "رصيد افتتاحي");
        }

        AddMovement(Items[1], "Stock Out", 5, "صرف لصيانة");
        AddMovement(Items[3], "Stock Out", 2, "بيع نقدي");
    }
}
