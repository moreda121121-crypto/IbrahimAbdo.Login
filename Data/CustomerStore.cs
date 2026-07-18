namespace IbrahimAbdo.Login.Data;

internal sealed class CarRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string PlateNumber { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Model { get; set; } = "";
    public int Year { get; set; } = DateTime.Now.Year;
    public string Color { get; set; } = "";
    public string Vin { get; set; } = "";
    public int Mileage { get; set; }
    public string FuelType { get; set; } = "بنزين";
    public string Transmission { get; set; } = "أوتوماتيك";

    public string DisplayName => $"{Brand} {Model}".Trim();
}

internal sealed class InvoiceSummary
{
    public string Number { get; set; } = "";
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "مدفوعة";
}

internal sealed class ServiceSummary
{
    public DateTime Date { get; set; }
    public string Service { get; set; } = "";
    public string Technician { get; set; } = "";
    public string Status { get; set; } = "مكتمل";
}

internal sealed class CustomerRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Address { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime RegisteredAt { get; set; } = DateTime.Today;
    public bool IsVip { get; set; }
    public List<CarRecord> Cars { get; set; } = [];
    public List<InvoiceSummary> Invoices { get; set; } = [];
    public List<ServiceSummary> Services { get; set; } = [];

    public CarRecord? PrimaryCar => Cars.FirstOrDefault();
    public string PrimaryPlate => PrimaryCar?.PlateNumber ?? "—";
    public string PrimaryCarName => PrimaryCar?.DisplayName ?? "—";
    public int CarsCount => Cars.Count;
    public DateTime? LastVisit => Invoices.Count > 0
        ? Invoices.Max(i => i.Date)
        : Services.Count > 0 ? Services.Max(s => s.Date) : null;
    public decimal TotalInvoices => Invoices.Sum(i => i.Amount);
}

internal static class CustomerStore
{
    private static readonly List<CustomerRecord> Items = [];
    private static readonly string FilePath =
        Path.Combine(AppContext.BaseDirectory, "Data", "customers.json");

    public static IReadOnlyList<CustomerRecord> All => Items;

    public static int TotalCustomers => Items.Count;
    public static int TotalCars => Items.Sum(c => c.Cars.Count);
    public static decimal TotalSales => Items.Sum(c => c.TotalInvoices);
    public static int VipCount => Items.Count(c => c.IsVip);

    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var loaded = System.Text.Json.JsonSerializer.Deserialize<List<CustomerRecord>>(json);
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

    public static void Add(CustomerRecord customer)
    {
        Items.Insert(0, customer);
        Save();
    }

    public static void Update(CustomerRecord customer)
    {
        var idx = Items.FindIndex(c => c.Id == customer.Id);
        if (idx >= 0)
        {
            Items[idx] = customer;
            Save();
        }
    }

    public static void Remove(string id)
    {
        Items.RemoveAll(c => c.Id == id);
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

    public static IEnumerable<CustomerRecord> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Items;
        }

        var q = query.Trim();
        return Items.Where(c =>
            c.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            c.Phone.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            c.Cars.Any(car => car.PlateNumber.Contains(q, StringComparison.OrdinalIgnoreCase)));
    }

    private static void Seed()
    {
        Items.Clear();
        Items.AddRange(
        [
            new CustomerRecord
            {
                Name = "كريم أحمد محمد",
                Phone = "01012345678",
                Email = "karim@mail.com",
                Address = "المعادي، القاهرة",
                Notes = "عميل منتظم",
                RegisteredAt = new DateTime(2024, 3, 12),
                IsVip = true,
                Cars =
                [
                    new CarRecord { PlateNumber = "أ ب ج 1234", Brand = "Toyota", Model = "Corolla", Year = 2020, Color = "أسود", Mileage = 45000 },
                    new CarRecord { PlateNumber = "س ص ع 5678", Brand = "Hyundai", Model = "Elantra", Year = 2022, Color = "أبيض", Mileage = 18000 }
                ],
                Invoices =
                [
                    new InvoiceSummary { Number = "INV-1042", Date = new DateTime(2026, 7, 10), Amount = 3190m, Status = "مدفوعة" },
                    new InvoiceSummary { Number = "INV-0988", Date = new DateTime(2026, 5, 22), Amount = 1850m, Status = "مدفوعة" },
                    new InvoiceSummary { Number = "INV-0911", Date = new DateTime(2026, 3, 8), Amount = 920m, Status = "جزئي" }
                ],
                Services =
                [
                    new ServiceSummary { Date = new DateTime(2026, 7, 10), Service = "تغيير زيت", Technician = "محمود علي", Status = "مكتمل" },
                    new ServiceSummary { Date = new DateTime(2026, 5, 22), Service = "فحص كمبيوتر", Technician = "أحمد حسن", Status = "مكتمل" }
                ]
            },
            new CustomerRecord
            {
                Name = "سارة محمود",
                Phone = "01198765432",
                Address = "مدينة نصر",
                RegisteredAt = new DateTime(2025, 1, 5),
                Cars =
                [
                    new CarRecord { PlateNumber = "ن م ل 9900", Brand = "BMW", Model = "320i", Year = 2019, Color = "رمادي", Mileage = 62000 }
                ],
                Invoices =
                [
                    new InvoiceSummary { Number = "INV-1020", Date = new DateTime(2026, 6, 2), Amount = 5400m, Status = "مدفوعة" }
                ],
                Services =
                [
                    new ServiceSummary { Date = new DateTime(2026, 6, 2), Service = "فرامل أمامية", Technician = "محمود علي", Status = "مكتمل" }
                ]
            },
            new CustomerRecord
            {
                Name = "يوسف عبد الله",
                Phone = "01234567890",
                Address = "الجيزة",
                RegisteredAt = new DateTime(2025, 8, 18),
                IsVip = true,
                Cars =
                [
                    new CarRecord { PlateNumber = "ك و ي 1122", Brand = "Mercedes", Model = "C200", Year = 2021, Color = "أسود", Mileage = 30000 }
                ],
                Invoices =
                [
                    new InvoiceSummary { Number = "INV-1035", Date = new DateTime(2026, 7, 1), Amount = 8900m, Status = "مدفوعة" },
                    new InvoiceSummary { Number = "INV-1001", Date = new DateTime(2026, 4, 14), Amount = 2100m, Status = "مدفوعة" }
                ],
                Services =
                [
                    new ServiceSummary { Date = new DateTime(2026, 7, 1), Service = "صيانة دورية", Technician = "أحمد حسن", Status = "مكتمل" }
                ]
            },
            new CustomerRecord
            {
                Name = "منى إبراهيم",
                Phone = "01555551234",
                Address = "الشروق",
                RegisteredAt = new DateTime(2026, 2, 1),
                Cars =
                [
                    new CarRecord { PlateNumber = "ط ظ ع 3344", Brand = "Kia", Model = "Sportage", Year = 2023, Color = "أزرق", Mileage = 12000 }
                ],
                Invoices =
                [
                    new InvoiceSummary { Number = "INV-1030", Date = new DateTime(2026, 6, 28), Amount = 1500m, Status = "معلقة" }
                ],
                Services =
                [
                    new ServiceSummary { Date = new DateTime(2026, 6, 28), Service = "غسيل كامل", Technician = "محمود علي", Status = "قيد التنفيذ" }
                ]
            },
            new CustomerRecord
            {
                Name = "حسام الدين",
                Phone = "01009876543",
                Address = "6 أكتوبر",
                RegisteredAt = new DateTime(2024, 11, 20),
                Cars =
                [
                    new CarRecord { PlateNumber = "ف ق ر 7788", Brand = "Nissan", Model = "Sunny", Year = 2018, Color = "فضي", Mileage = 88000 }
                ],
                Invoices =
                [
                    new InvoiceSummary { Number = "INV-0970", Date = new DateTime(2026, 2, 11), Amount = 760m, Status = "مدفوعة" }
                ],
                Services =
                [
                    new ServiceSummary { Date = new DateTime(2026, 2, 11), Service = "فلتر هواء", Technician = "أحمد حسن", Status = "مكتمل" }
                ]
            }
        ]);
    }
}
