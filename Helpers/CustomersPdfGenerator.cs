using System.Diagnostics;
using IbrahimAbdo.Login.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IbrahimAbdo.Login.Helpers;

internal static class CustomersPdfGenerator
{
    public static string GenerateAndOpen(IReadOnlyList<CustomerRecord> customers) =>
        Generate(customers, openAfter: true);

    public static string Generate(IReadOnlyList<CustomerRecord> customers, bool openAfter = true)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var dir = Path.Combine(AppContext.BaseDirectory, "Reports");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"customers-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");

        var totalCars = customers.Sum(c => c.CarsCount);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x
                    .FontSize(9)
                    .FontColor(Colors.Black)
                    .FontFamily("Segoe UI"));

                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Element(TitleBar);
                    col.Item().Element(c => Summary(c, customers.Count, totalCars));
                    col.Item().Element(c => Table(c, customers));
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Ibrahim Abdo Auto Service  •  ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    t.Span($"طُبع في {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf(filePath);

        if (openAfter)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }

        return filePath;
    }

    private static void TitleBar(IContainer container)
    {
        container.Background(Colors.Grey.Darken3).Padding(10).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().AlignRight().Text("قائمة العملاء").FontColor(Colors.White).Bold().FontSize(16);
                col.Item().AlignRight().Text("CUSTOMERS LIST").FontColor(Colors.Grey.Lighten2).FontSize(9);
            });
        });
    }

    private static void Summary(IContainer container, int customerCount, int carCount)
    {
        container.Row(row =>
        {
            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Medium).Padding(6).Column(col =>
            {
                col.Item().AlignRight().Text("إجمالي العملاء").FontSize(8).FontColor(Colors.Grey.Darken1);
                col.Item().AlignRight().Text(customerCount.ToString()).Bold().FontSize(13);
            });
            row.ConstantItem(8);
            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Medium).Padding(6).Column(col =>
            {
                col.Item().AlignRight().Text("إجمالي السيارات").FontSize(8).FontColor(Colors.Grey.Darken1);
                col.Item().AlignRight().Text(carCount.ToString()).Bold().FontSize(13);
            });
        });
    }

    private static void Table(IContainer container, IReadOnlyList<CustomerRecord> customers)
    {
        container.Border(1).BorderColor(Colors.Black).Table(table =>
        {
            // RTL: rightmost = #, then name, phone, plate, cars, total, then date on the left
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(70);  // تاريخ التسجيل
                c.ConstantColumn(70);  // إجمالي الفواتير
                c.ConstantColumn(45);  // عدد السيارات
                c.RelativeColumn(2);   // رقم اللوحة
                c.RelativeColumn(2);   // الهاتف
                c.RelativeColumn(3);   // الاسم
                c.ConstantColumn(24);  // #
            });

            table.Header(h =>
            {
                h.Cell().Element(HeaderCell).AlignCenter().Text("REG. DATE\nتاريخ التسجيل").FontColor(Colors.White).Bold().FontSize(7);
                h.Cell().Element(HeaderCell).AlignCenter().Text("TOTAL\nإجمالي الفواتير").FontColor(Colors.White).Bold().FontSize(7);
                h.Cell().Element(HeaderCell).AlignCenter().Text("CARS\nالسيارات").FontColor(Colors.White).Bold().FontSize(7);
                h.Cell().Element(HeaderCell).AlignCenter().Text("PLATE\nرقم اللوحة").FontColor(Colors.White).Bold().FontSize(7);
                h.Cell().Element(HeaderCell).AlignCenter().Text("PHONE\nرقم الهاتف").FontColor(Colors.White).Bold().FontSize(7);
                h.Cell().Element(HeaderCell).AlignCenter().Text("NAME\nاسم العميل").FontColor(Colors.White).Bold().FontSize(8);
                h.Cell().Element(HeaderCell).AlignCenter().Text("#").FontColor(Colors.White).Bold().FontSize(8);
            });

            for (var i = 0; i < customers.Count; i++)
            {
                var c = customers[i];
                var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten3;
                var total = CustomerTotal(c);

                table.Cell().Element(x => BodyCell(x, bg)).AlignCenter()
                    .Text(c.RegisteredAt.ToString("dd/MM/yyyy")).FontSize(8);
                table.Cell().Element(x => BodyCell(x, bg)).AlignLeft()
                    .Text($"{total:N2}").FontSize(8);
                table.Cell().Element(x => BodyCell(x, bg)).AlignCenter()
                    .Text(c.CarsCount.ToString()).FontSize(8);
                table.Cell().Element(x => BodyCell(x, bg)).AlignRight()
                    .Text(c.PrimaryPlate).FontSize(8);
                table.Cell().Element(x => BodyCell(x, bg)).AlignRight()
                    .Text(string.IsNullOrWhiteSpace(c.Phone) ? "—" : c.Phone).FontSize(8);
                table.Cell().Element(x => BodyCell(x, bg)).AlignRight()
                    .Text(c.Name).FontSize(8);
                table.Cell().Element(IndexCell).AlignCenter()
                    .Text((i + 1).ToString()).FontColor(Colors.White).FontSize(7);
            }

            if (customers.Count == 0)
            {
                table.Cell().ColumnSpan(7).Element(x => BodyCell(x, Colors.White))
                    .AlignCenter().Text("لا يوجد عملاء").FontSize(9).FontColor(Colors.Grey.Darken1);
            }
        });

        static IContainer HeaderCell(IContainer c) =>
            c.Background(Colors.Grey.Darken3).Border(0.4f).BorderColor(Colors.Black)
                .PaddingVertical(4).PaddingHorizontal(3).MinHeight(20);

        static IContainer IndexCell(IContainer c) =>
            c.Background(Colors.Grey.Darken2).Border(0.35f).BorderColor(Colors.Grey.Lighten1)
                .PaddingVertical(3).PaddingHorizontal(2).MinHeight(16);

        static IContainer BodyCell(IContainer c, string bg) =>
            c.Background(bg).Border(0.35f).BorderColor(Colors.Grey.Lighten1)
                .PaddingVertical(3).PaddingHorizontal(4).MinHeight(16);
    }

    private static decimal CustomerTotal(CustomerRecord c)
    {
        var fromStore = InvoiceStore.All
            .Where(i => (!string.IsNullOrWhiteSpace(c.Phone) && i.Phone.Trim() == c.Phone.Trim()) ||
                        (!string.IsNullOrWhiteSpace(c.Name) && i.CustomerName.Trim() == c.Name.Trim()))
            .Sum(i => i.GrandTotal);

        return fromStore > 0 ? fromStore : c.TotalInvoices;
    }
}
