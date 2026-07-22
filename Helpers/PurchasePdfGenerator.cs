using System.Diagnostics;
using IbrahimAbdo.Login.Data;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IbrahimAbdo.Login.Helpers;

/// <summary>Purchase-invoice PDF using the same visual template as the sales invoice.</summary>
internal static class PurchasePdfGenerator
{
    public static string GenerateAndOpen(PurchaseInvoiceRecord invoice) =>
        Generate(invoice, openAfter: true);

    public static string Generate(PurchaseInvoiceRecord invoice, bool openAfter = true)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var dir = Path.Combine(AppContext.BaseDirectory, "Invoices");
        Directory.CreateDirectory(dir);
        var safeNumber = string.Join("_", (invoice.Number ?? "purchase").Split(Path.GetInvalidFileNameChars()));
        var filePath = Path.Combine(dir, $"{safeNumber}.pdf");
        var logo = LoadWhiteBackgroundLogoBytes();
        var qr = CreateQrPng($"PUR|{invoice.Number}|{invoice.SupplierName}|{invoice.GrandTotal:0.00}");
        var rows = invoice.Items.Where(i => !string.IsNullOrWhiteSpace(i.Name)).ToList();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(16);
                page.DefaultTextStyle(x => x
                    .FontSize(8)
                    .FontColor(Colors.Black)
                    .FontFamily("Segoe UI"));

                page.Content().Border(1).BorderColor(Colors.Black).Padding(10).Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Element(c => Header(c, invoice, logo, qr));
                    col.Item().Element(c => InfoCard(c, invoice));
                    col.Item().Element(c => ItemsTable(c, rows));
                    col.Item().Element(c => BottomSection(c, invoice));
                    col.Item().Element(ContactBar);
                    col.Item().Element(ThankYouBar);
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

    private static void Header(IContainer container, PurchaseInvoiceRecord invoice, byte[]? logo, byte[] qr)
    {
        container.Row(row =>
        {
            row.ConstantItem(150).Element(c =>
            {
                c.Height(150).Background(Colors.White).Padding(4).Element(inner =>
                {
                    if (logo is { Length: > 0 })
                    {
                        inner.Image(logo).FitArea();
                    }
                    else
                    {
                        inner.AlignCenter().AlignMiddle().Text("IBRAHIM ABDO").Bold().FontSize(11);
                    }
                });
            });

            row.RelativeItem().PaddingHorizontal(8).AlignMiddle().Column(c =>
            {
                c.Item().AlignCenter().Text("PURCHASE INVOICE").Bold().FontSize(22).FontColor(Colors.Black);
                c.Item().PaddingTop(2).AlignCenter().Text("فاتورة شراء").FontSize(13).FontColor(Colors.Black);
                c.Item().PaddingTop(8)
                    .Border(1).BorderColor(Colors.Black)
                    .PaddingVertical(5).PaddingHorizontal(8)
                    .Column(meta =>
                    {
                        meta.Spacing(2);
                        meta.Item().Element(m => MetaLine(m, "INVOICE NO. / رقم الفاتورة", invoice.Number));
                        meta.Item().Element(m => MetaLine(m, "DATE / التاريخ", invoice.CreatedAt.ToString("dd / MM / yyyy")));
                        meta.Item().Element(m => MetaLine(m, "PAGE / الصفحة", "1 OF 1"));
                    });
            });

            row.ConstantItem(100).Element(c =>
            {
                c.Column(col =>
                {
                    col.Item().Height(90).Image(qr).FitArea();
                    col.Item().PaddingTop(4).Background(Colors.Black).PaddingVertical(4)
                        .AlignCenter().Text("SCAN ME").FontColor(Colors.White).FontSize(8).Bold();
                });
            });
        });
    }

    private static void MetaLine(IContainer container, string label, string value)
    {
        container.Row(row =>
        {
            row.RelativeItem().AlignMiddle().Text(label).FontSize(7.5f).FontColor(Colors.Grey.Darken2);
            row.ConstantItem(85).AlignMiddle().AlignRight().Text(value).FontSize(8.5f).SemiBold();
        });
    }

    private static void InfoCard(IContainer container, PurchaseInvoiceRecord invoice)
    {
        container.Border(1).BorderColor(Colors.Black).Column(card =>
        {
            card.Item().Background(Colors.Grey.Darken3).Padding(5)
                .Text("SUPPLIER INFORMATION / بيانات المورد")
                .FontColor(Colors.White).SemiBold().FontSize(8);
            card.Item().Padding(7).Row(row =>
            {
                row.RelativeItem().Column(body =>
                {
                    body.Item().Element(c => Field(c, "SUPPLIER NAME / اسم المورد", invoice.SupplierName));
                    body.Item().Element(c => Field(c, "PHONE NUMBER / رقم الهاتف",
                        string.IsNullOrWhiteSpace(invoice.Phone) ? "—" : invoice.Phone));
                });
                row.ConstantItem(16);
                row.RelativeItem().Column(body =>
                {
                    body.Item().Element(c => Field(c, "PAYMENT METHOD / طريقة الدفع",
                        string.IsNullOrWhiteSpace(invoice.PaymentMethod) ? "—" : invoice.PaymentMethod));
                    body.Item().Element(c => Field(c, "DATE / التاريخ", invoice.CreatedAt.ToString("dd/MM/yyyy HH:mm")));
                });
            });
        });
    }

    private static void Field(IContainer container, string label, string value)
    {
        container.PaddingVertical(1.8f).Row(row =>
        {
            row.RelativeItem(2.4f).Text(label).FontSize(7).FontColor(Colors.Grey.Darken2);
            row.RelativeItem(2.2f).AlignRight().Text(value).FontSize(8.5f).SemiBold();
        });
    }

    private static void ItemsTable(IContainer container, List<PurchaseInvoiceItemRecord> rows)
    {
        container.Border(1).BorderColor(Colors.Black).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(62);  // TOTAL
                c.ConstantColumn(62);  // UNIT PRICE
                c.ConstantColumn(48);  // QTY
                c.RelativeColumn(5);   // ITEM
                c.ConstantColumn(22);  // #
            });

            table.Header(h =>
            {
                h.Cell().Element(HeaderCell).AlignCenter().Text("TOTAL\nالإجمالي").FontColor(Colors.White).Bold().FontSize(7);
                h.Cell().Element(HeaderCell).AlignCenter().Text("UNIT PRICE\nسعر الوحدة").FontColor(Colors.White).Bold().FontSize(7);
                h.Cell().Element(HeaderCell).AlignCenter().Text("QTY.\nالكمية").FontColor(Colors.White).Bold().FontSize(7);
                h.Cell().Element(HeaderCell).AlignCenter().Text("ITEM / الصنف").FontColor(Colors.White).Bold().FontSize(8);
                h.Cell().Element(HeaderCell).AlignCenter().Text("#").FontColor(Colors.White).Bold().FontSize(8);
            });

            for (var i = 0; i < rows.Count; i++)
            {
                var item = rows[i];
                var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten3;

                table.Cell().Element(c => BodyCell(c, bg)).AlignLeft().Text(item.Total.ToString("N2")).FontSize(8);
                table.Cell().Element(c => BodyCell(c, bg)).AlignLeft().Text(item.UnitPrice.ToString("N2")).FontSize(8);
                table.Cell().Element(c => BodyCell(c, bg)).AlignCenter().Text(item.Qty.ToString()).FontSize(8);
                table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.Name).FontSize(8);
                table.Cell().Element(IndexCell).AlignCenter().Text((i + 1).ToString()).FontColor(Colors.White).FontSize(7);
            }

            if (rows.Count == 0)
            {
                table.Cell().ColumnSpan(5).Element(c => BodyCell(c, Colors.White))
                    .AlignCenter().Text("لا توجد أصناف").FontSize(8).FontColor(Colors.Grey.Darken1);
            }
        });

        static IContainer HeaderCell(IContainer c) =>
            c.Background(Colors.Grey.Darken3).Border(0.4f).BorderColor(Colors.Black)
                .PaddingVertical(3).PaddingHorizontal(2).MinHeight(18);

        static IContainer IndexCell(IContainer c) =>
            c.Background(Colors.Grey.Darken2).Border(0.35f).BorderColor(Colors.Grey.Lighten1)
                .PaddingVertical(2).PaddingHorizontal(2).MinHeight(14);

        static IContainer BodyCell(IContainer c, string bg) =>
            c.Background(bg).Border(0.35f).BorderColor(Colors.Grey.Lighten1)
                .PaddingVertical(2).PaddingHorizontal(3).MinHeight(14);
    }

    private static void BottomSection(IContainer container, PurchaseInvoiceRecord invoice)
    {
        var remaining = Math.Max(0, invoice.GrandTotal - invoice.Paid);
        container.Row(row =>
        {
            row.RelativeItem(2).Border(1).BorderColor(Colors.Black).Column(card =>
            {
                card.Item().Background(Colors.Grey.Darken3).Padding(4)
                    .Text("NOTES / ملاحظات").FontColor(Colors.White).SemiBold().FontSize(8);
                card.Item().Padding(6).Column(body =>
                {
                    var notes = string.IsNullOrWhiteSpace(invoice.Notes) ? "" : invoice.Notes.Trim();
                    if (string.IsNullOrWhiteSpace(notes))
                    {
                        for (var i = 0; i < 4; i++)
                        {
                            body.Item().PaddingTop(7).BorderBottom(0.7f).BorderColor(Colors.Grey.Lighten1).Height(9);
                        }
                    }
                    else
                    {
                        body.Item().Text(notes).FontSize(8).FontColor(Colors.Black);
                    }
                });
            });

            row.ConstantItem(6);

            row.RelativeItem(2.5f).Border(1).BorderColor(Colors.Black).Column(card =>
            {
                card.Item().Element(c => Total(c, "SUBTOTAL / الإجمالي الفرعي", invoice.Subtotal.ToString("N2"), false));
                if (invoice.Discount > 0)
                {
                    card.Item().Element(c => Total(c, "DISCOUNT / الخصم", invoice.Discount.ToString("N2"), false));
                }

                card.Item().Element(c => Total(c, "TOTAL AMOUNT / المجموع الكلي", invoice.GrandTotal.ToString("N2"), true));
                card.Item().Element(c => Total(c, "PAID AMOUNT / المبلغ المدفوع", invoice.Paid.ToString("N2"), false));
                card.Item().Element(c => Total(c, "DUE AMOUNT / المبلغ المتبقي", remaining.ToString("N2"), false));
            });
        });
    }

    private static void Total(IContainer container, string label, string value, bool highlight)
    {
        var cell = container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1)
            .PaddingVertical(3.5f).PaddingHorizontal(6);
        if (highlight)
        {
            cell = cell.Background(Colors.Black);
        }

        cell.Row(row =>
        {
            row.RelativeItem().Text(label).FontSize(7)
                .FontColor(highlight ? Colors.White : Colors.Black).SemiBold();
            row.ConstantItem(55).AlignRight().Text(value).FontSize(8)
                .FontColor(highlight ? Colors.White : Colors.Black).Bold();
        });
    }

    private static void ContactBar(IContainer container)
    {
        container.BorderTop(0.8f).BorderColor(Colors.Grey.Lighten1).PaddingTop(5).Row(row =>
        {
            row.RelativeItem().AlignCenter().Text("010 1234 5678").FontSize(6.5f);
            row.RelativeItem().AlignCenter().Text("Cairo, Egypt").FontSize(6.5f);
            row.RelativeItem().AlignCenter().Text("info@ibrahimabdoautoservice.com").FontSize(6.5f);
            row.RelativeItem().AlignCenter().Text("www.ibrahimabdoautoservice.com").FontSize(6.5f);
        });
    }

    private static void ThankYouBar(IContainer container)
    {
        container.Background(Colors.Black).PaddingVertical(7).PaddingHorizontal(12).Row(row =>
        {
            row.RelativeItem().AlignMiddle().Text("THANK YOU").FontColor(Colors.White).Bold().FontSize(11);
            row.RelativeItem().AlignMiddle().AlignRight().Text("شكراً لتعاملكم معنا").FontColor(Colors.White).Bold().FontSize(11);
        });
    }

    private static byte[]? LoadWhiteBackgroundLogoBytes()
    {
        var assets = Path.Combine(AppContext.BaseDirectory, "Assets");
        var invoiceLogoPath = Path.Combine(assets, "logo-invoice.png");
        var colorPath = Path.Combine(assets, "logo-ibrahim.png");

        try
        {
            if (File.Exists(invoiceLogoPath))
            {
                return File.ReadAllBytes(invoiceLogoPath);
            }

            return File.Exists(colorPath) ? File.ReadAllBytes(colorPath) : null;
        }
        catch
        {
            return null;
        }
    }

    private static byte[] CreateQrPng(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(data).GetGraphic(5);
    }
}
