using IbrahimAbdo.Login.Data;

namespace IbrahimAbdo.Login.Services;

internal sealed class ReportFilter
{
    public DateTime From { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime To { get; set; } = DateTime.Today;
    public string ReportType { get; set; } = "الكل";
    public string Category { get; set; } = "الكل";
    public string Technician { get; set; } = "الكل";
}

internal sealed class ReportKpis
{
    public decimal TotalSales { get; set; }
    public int InvoiceCount { get; set; }
    public decimal AvgInvoice { get; set; }
    public decimal TotalProfit { get; set; }
    public int NewCustomers { get; set; }
    public decimal SalesTrendPct { get; set; }
    public decimal InvoiceTrendPct { get; set; }
    public decimal AvgTrendPct { get; set; }
    public decimal ProfitTrendPct { get; set; }
    public decimal CustomerTrendPct { get; set; }
}

internal sealed class DailySalesPoint
{
    public DateTime Day { get; set; }
    public decimal Amount { get; set; }
}

internal sealed class CategorySalesSlice
{
    public string Name { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal Percent { get; set; }
    public Color Color { get; set; }
}

internal sealed class RankedSaleRow
{
    public string Name { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Total { get; set; }
}

internal sealed class ReportSnapshot
{
    public ReportKpis Kpis { get; set; } = new();
    public List<DailySalesPoint> DailySales { get; set; } = [];
    public List<CategorySalesSlice> CategorySales { get; set; } = [];
    public List<RankedSaleRow> TopServices { get; set; } = [];
    public List<RankedSaleRow> TopProducts { get; set; } = [];
    public List<InvoiceRecord> FilteredInvoices { get; set; } = [];
    public List<PurchaseInvoiceRecord> FilteredPurchases { get; set; } = [];
    public decimal PurchaseTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal InventoryValue { get; set; }
}

/// <summary>
/// Aggregates live data from existing JSON stores (app backend) for the Reports dashboard.
/// </summary>
internal static class ReportsService
{
    private static readonly Color[] CategoryPalette =
    [
        Color.FromArgb(70, 140, 220),
        Color.FromArgb(80, 180, 120),
        Color.FromArgb(230, 170, 60),
        Color.FromArgb(160, 110, 200),
        Color.FromArgb(140, 150, 160),
        Color.FromArgb(220, 100, 100),
        Color.FromArgb(100, 190, 190),
        Color.FromArgb(200, 140, 90)
    ];

    public static void EnsureLoaded()
    {
        InvoiceStore.Load();
        PurchaseInvoiceStore.Load();
        ProductStore.Load();
        CustomerStore.Load();
        TechnicianStore.Load();
    }

    public static ReportSnapshot Build(ReportFilter filter)
    {
        EnsureLoaded();
        var from = filter.From.Date;
        var to = filter.To.Date.AddDays(1).AddTicks(-1);
        var span = to - from;
        if (span < TimeSpan.Zero)
        {
            span = TimeSpan.Zero;
        }

        var prevTo = from.AddTicks(-1);
        var prevFrom = prevTo - span;

        var invoices = FilterInvoices(from, to, filter);
        var prevInvoices = FilterInvoices(prevFrom, prevTo, filter);
        var purchases = PurchaseInvoiceStore.All
            .Where(p => p.CreatedAt >= from && p.CreatedAt <= to)
            .ToList();

        var sales = invoices.Sum(i => i.GrandTotal);
        var prevSales = prevInvoices.Sum(i => i.GrandTotal);
        var profit = CalcProfit(invoices);
        var prevProfit = CalcProfit(prevInvoices);
        var avg = invoices.Count > 0 ? sales / invoices.Count : 0m;
        var prevAvg = prevInvoices.Count > 0 ? prevSales / prevInvoices.Count : 0m;

        var newCustomers = CustomerStore.All.Count(c => c.RegisteredAt.Date >= from.Date && c.RegisteredAt.Date <= to.Date);
        var prevCustomers = CustomerStore.All.Count(c =>
            c.RegisteredAt.Date >= prevFrom.Date && c.RegisteredAt.Date <= prevTo.Date);

        var snap = new ReportSnapshot
        {
            FilteredInvoices = invoices,
            FilteredPurchases = purchases,
            PurchaseTotal = purchases.Sum(p => p.GrandTotal),
            TaxTotal = invoices.Sum(i => i.Tax),
            InventoryValue = ProductStore.InventoryValue,
            Kpis = new ReportKpis
            {
                TotalSales = sales,
                InvoiceCount = invoices.Count,
                AvgInvoice = avg,
                TotalProfit = profit,
                NewCustomers = newCustomers,
                SalesTrendPct = Trend(sales, prevSales),
                InvoiceTrendPct = Trend(invoices.Count, prevInvoices.Count),
                AvgTrendPct = Trend(avg, prevAvg),
                ProfitTrendPct = Trend(profit, prevProfit),
                CustomerTrendPct = Trend(newCustomers, prevCustomers)
            },
            DailySales = BuildDaily(invoices, from.Date, filter.To.Date),
            CategorySales = BuildCategories(invoices),
            TopServices = BuildTopServices(invoices),
            TopProducts = BuildTopProducts(invoices)
        };
        return snap;
    }

    public static IEnumerable<string> ExportLines(ReportSnapshot snap, ReportFilter filter, string? focus = null)
    {
        var lines = new List<string>
        {
            "تقرير النظام - Ibrahim Abdo Auto Service",
            $"من {filter.From:dd/MM/yyyy} إلى {filter.To:dd/MM/yyyy}",
            $"نوع التقرير: {filter.ReportType} | التصنيف: {filter.Category} | الفني: {filter.Technician}",
            $"إجمالي المبيعات: {snap.Kpis.TotalSales:0.00}",
            $"إجمالي الفواتير: {snap.Kpis.InvoiceCount}",
            $"متوسط قيمة الفاتورة: {snap.Kpis.AvgInvoice:0.00}",
            $"إجمالي الأرباح: {snap.Kpis.TotalProfit:0.00}",
            $"عملاء جدد: {snap.Kpis.NewCustomers}",
            $"إجمالي المشتريات: {snap.PurchaseTotal:0.00}",
            $"إجمالي الضرائب: {snap.TaxTotal:0.00}",
            $"قيمة المخزون: {snap.InventoryValue:0.00}",
            ""
        };

        focus = focus ?? filter.ReportType;
        if (focus is "الكل" or "المبيعات" or "فواتير البيع")
        {
            lines.Add("=== المبيعات ===");
            lines.Add("رقم الفاتورة,التاريخ,العميل,الفني,الإجمالي,الضريبة");
            lines.AddRange(snap.FilteredInvoices.Select(i =>
                $"{i.Number},{i.CreatedAt:dd/MM/yyyy},{i.CustomerName},{i.Technician},{i.GrandTotal:0.00},{i.Tax:0.00}"));
            lines.Add("");
        }

        if (focus is "الكل" or "المشتريات" or "فواتير الشراء")
        {
            lines.Add("=== المشتريات ===");
            lines.Add("رقم الفاتورة,التاريخ,المورد,الإجمالي");
            lines.AddRange(snap.FilteredPurchases.Select(p =>
                $"{p.Number},{p.CreatedAt:dd/MM/yyyy},{p.SupplierName},{p.GrandTotal:0.00}"));
            lines.Add("");
        }

        if (focus is "الكل" or "الخدمات")
        {
            lines.Add("=== أكثر الخدمات مبيعاً ===");
            lines.Add("#,الخدمة,العدد,الإجمالي");
            var i = 1;
            lines.AddRange(snap.TopServices.Select(r => $"{i++},{r.Name},{r.Quantity:0},{r.Total:0.00}"));
            lines.Add("");
        }

        if (focus is "الكل" or "الأصناف" or "المخزون")
        {
            lines.Add("=== أكثر الأصناف مبيعاً ===");
            lines.Add("#,الصنف,الكمية,الإجمالي");
            var i = 1;
            lines.AddRange(snap.TopProducts.Select(r => $"{i++},{r.Name},{r.Quantity:0},{r.Total:0.00}"));
            lines.Add("");
            lines.Add("=== المخزون الحالي ===");
            lines.Add("الكود,الاسم,التصنيف,الكمية,الشراء,البيع");
            lines.AddRange(ProductStore.All.Select(p =>
                $"{p.Code},{p.Name},{p.Category},{p.Quantity},{p.PurchasePrice:0.00},{p.SellingPrice:0.00}"));
            lines.Add("");
        }

        if (focus is "الكل" or "العملاء")
        {
            lines.Add("=== العملاء ===");
            lines.Add("الاسم,الهاتف,تاريخ التسجيل,إجمالي الفواتير");
            lines.AddRange(CustomerStore.All.Select(c =>
                $"{c.Name},{c.Phone},{c.RegisteredAt:dd/MM/yyyy},{c.TotalInvoices:0.00}"));
            lines.Add("");
        }

        if (focus is "الكل" or "الفنيين")
        {
            lines.Add("=== أداء الفنيين ===");
            lines.Add("الفني,عدد الفواتير,إجمالي المبيعات");
            lines.AddRange(snap.FilteredInvoices
                .GroupBy(i => string.IsNullOrWhiteSpace(i.Technician) ? "—" : i.Technician)
                .Select(g => $"{g.Key},{g.Count()},{g.Sum(x => x.GrandTotal):0.00}"));
            lines.Add("");
        }

        if (focus is "الكل" or "الأرباح والخسائر")
        {
            lines.Add("=== الأرباح والخسائر ===");
            lines.Add($"المبيعات,{snap.Kpis.TotalSales:0.00}");
            lines.Add($"تكلفة المشتريات,{snap.PurchaseTotal:0.00}");
            lines.Add($"الأرباح المقدرة,{snap.Kpis.TotalProfit:0.00}");
            lines.Add($"الضرائب,{snap.TaxTotal:0.00}");
            lines.Add("");
        }

        if (focus is "الكل" or "الضرائب")
        {
            lines.Add("=== الضرائب ===");
            lines.Add("رقم الفاتورة,التاريخ,الضريبة,الإجمالي");
            lines.AddRange(snap.FilteredInvoices.Select(i =>
                $"{i.Number},{i.CreatedAt:dd/MM/yyyy},{i.Tax:0.00},{i.GrandTotal:0.00}"));
        }

        return lines;
    }

    private static List<InvoiceRecord> FilterInvoices(DateTime from, DateTime to, ReportFilter filter)
    {
        IEnumerable<InvoiceRecord> q = InvoiceStore.All
            .Where(i => i.CreatedAt >= from && i.CreatedAt <= to);

        if (!string.IsNullOrWhiteSpace(filter.Technician) && filter.Technician != "الكل")
        {
            q = q.Where(i => string.Equals(i.Technician?.Trim(), filter.Technician.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.Category) && filter.Category != "الكل")
        {
            q = q.Where(i => i.Items.Any(line =>
            {
                var p = MatchProduct(line.Name);
                return p is not null && string.Equals(p.Category, filter.Category, StringComparison.OrdinalIgnoreCase);
            }) || i.Items.Any(line => line.Name.Contains(filter.Category, StringComparison.OrdinalIgnoreCase)));
        }

        // Report type narrows which invoice sets contribute to KPI focus (sales always uses sales invoices)
        return q.OrderByDescending(i => i.CreatedAt).ToList();
    }

    private static decimal CalcProfit(IEnumerable<InvoiceRecord> invoices)
    {
        decimal profit = 0;
        foreach (var inv in invoices)
        {
            foreach (var line in inv.Items)
            {
                var p = MatchProduct(line.Name);
                if (p is not null)
                {
                    profit += p.ProfitPerItem * line.Qty;
                }
                else
                {
                    // Labor/services without product: treat ~40% of line as contribution margin estimate from labor fee share
                    profit += line.Total * 0.35m;
                }
            }

            profit += inv.LaborFee * 0.7m;
        }

        return profit;
    }

    private static ProductRecord? MatchProduct(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return ProductStore.All.FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) ||
            name.Contains(p.Name, StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    private static List<DailySalesPoint> BuildDaily(List<InvoiceRecord> invoices, DateTime from, DateTime to)
    {
        var days = new List<DailySalesPoint>();
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
        {
            days.Add(new DailySalesPoint
            {
                Day = d,
                Amount = invoices.Where(i => i.CreatedAt.Date == d).Sum(i => i.GrandTotal)
            });
        }

        return days;
    }

    private static List<CategorySalesSlice> BuildCategories(List<InvoiceRecord> invoices)
    {
        var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var inv in invoices)
        {
            if (inv.Items.Count == 0)
            {
                map["خدمات"] = map.GetValueOrDefault("خدمات") + inv.GrandTotal;
                continue;
            }

            foreach (var line in inv.Items)
            {
                var p = MatchProduct(line.Name);
                var cat = p?.Category;
                if (string.IsNullOrWhiteSpace(cat))
                {
                    cat = LooksLikeService(line.Name) ? "خدمات" : "أخرى";
                }

                map[cat!] = map.GetValueOrDefault(cat!) + line.Total;
            }

            if (inv.LaborFee > 0)
            {
                map["خدمات"] = map.GetValueOrDefault("خدمات") + inv.LaborFee;
            }
        }

        var total = map.Values.Sum();
        if (total <= 0)
        {
            return [];
        }

        return map.OrderByDescending(kv => kv.Value)
            .Select((kv, i) => new CategorySalesSlice
            {
                Name = kv.Key,
                Amount = kv.Value,
                Percent = Math.Round(kv.Value / total * 100m, 1),
                Color = CategoryPalette[i % CategoryPalette.Length]
            })
            .ToList();
    }

    private static List<RankedSaleRow> BuildTopServices(List<InvoiceRecord> invoices)
    {
        return invoices.SelectMany(i => i.Items)
            .Where(l => LooksLikeService(l.Name) || MatchProduct(l.Name) is null)
            .GroupBy(l => l.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new RankedSaleRow
            {
                Name = g.Key,
                Quantity = g.Sum(x => x.Qty),
                Total = g.Sum(x => x.Total)
            })
            .OrderByDescending(r => r.Total)
            .Take(20)
            .ToList();
    }

    private static List<RankedSaleRow> BuildTopProducts(List<InvoiceRecord> invoices)
    {
        return invoices.SelectMany(i => i.Items)
            .Where(l => MatchProduct(l.Name) is not null && !LooksLikeService(l.Name))
            .GroupBy(l => l.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new RankedSaleRow
            {
                Name = g.Key,
                Quantity = g.Sum(x => x.Qty),
                Total = g.Sum(x => x.Total)
            })
            .OrderByDescending(r => r.Total)
            .Take(20)
            .ToList();
    }

    private static bool LooksLikeService(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        string[] keys = ["صيانة", "تغيير", "خدمة", "فحص", "غسيل", "تركيب", "معايرة", "تشخيص"];
        return keys.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private static decimal Trend(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            return current > 0 ? 100m : 0m;
        }

        return Math.Round((current - previous) / Math.Abs(previous) * 100m, 1);
    }
}
