using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using IbrahimAbdo.Login.Data;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SdColor = System.Drawing.Color;
using SdImageFormat = System.Drawing.Imaging.ImageFormat;

namespace IbrahimAbdo.Login.Helpers;

internal static class InvoicePdfGenerator
{
    public static string GenerateAndOpen(InvoiceRecord invoice)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var dir = Path.Combine(AppContext.BaseDirectory, "Invoices");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"{invoice.Number}.pdf");
        var logo = LoadBlackAndWhiteLogoBytes();
        var qr = CreateQrPng($"INV|{invoice.Number}|{invoice.CustomerName}|{invoice.GrandTotal:0.00}");
        var rows = invoice.Items
            .Where(i => !string.IsNullOrWhiteSpace(i.Name))
            .Select(i => new InvoiceItemRecord
            {
                Name = i.Name,
                Qty = i.Qty,
                UnitPrice = i.UnitPrice
            })
            .ToList();

        // Keep a readable table height even if few items
        while (rows.Count < 12)
        {
            rows.Add(new InvoiceItemRecord());
        }

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
                    col.Item().Element(c => InfoCards(c, invoice));
                    col.Item().Element(c => ItemsTable(c, rows));
                    col.Item().Element(c => BottomSection(c, invoice));
                    col.Item().Element(TrustBadges);
                    col.Item().Element(ContactBar);
                    col.Item().Element(ThankYouBar);
                });
            });
        }).GeneratePdf(filePath);

        Process.Start(new ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true
        });

        return filePath;
    }

    private static void Header(IContainer container, InvoiceRecord invoice, byte[]? logo, byte[] qr)
    {
        container.Row(row =>
        {
            // Logo
            row.ConstantItem(105).AlignMiddle().AlignLeft().Element(c =>
            {
                if (logo is { Length: > 0 })
                {
                    c.Width(100).Height(100).Image(logo).FitArea();
                }
                else
                {
                    c.Width(100).Height(100).Border(1).AlignCenter().AlignMiddle()
                        .Text("IBRAHIM ABDO").Bold().FontSize(11);
                }
            });

            // Title + meta
            row.RelativeItem().PaddingHorizontal(10).AlignMiddle().Column(c =>
            {
                c.Item().AlignCenter().Text("INVOICE")
                    .Bold()
                    .FontSize(26)
                    .FontColor(Colors.Black);

                c.Item().PaddingTop(2).AlignCenter().Text("فاتورة")
                    .FontSize(13)
                    .FontColor(Colors.Black);

                c.Item().PaddingTop(8).AlignCenter().Width(260)
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

            // QR
            row.ConstantItem(105).AlignMiddle().AlignRight().Column(c =>
            {
                c.Item().AlignRight().Width(90).Height(90).Image(qr).FitArea();
                c.Item().PaddingTop(4).AlignRight().Width(90)
                    .Background(Colors.Black)
                    .PaddingVertical(4)
                    .AlignCenter()
                    .Text("SCAN ME")
                    .FontColor(Colors.White)
                    .FontSize(8)
                    .Bold();
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

    private static void InfoCards(IContainer container, InvoiceRecord invoice)
    {
        container.Row(row =>
        {
            row.RelativeItem().Border(1).BorderColor(Colors.Black).Column(card =>
            {
                card.Item().Background(Colors.Grey.Darken3).Padding(5)
                    .Text("CUSTOMER INFORMATION / بيانات العميل")
                    .FontColor(Colors.White).SemiBold().FontSize(8);
                card.Item().Padding(7).Column(body =>
                {
                    body.Item().Element(c => Field(c, "CUSTOMER NAME / اسم العميل", invoice.CustomerName));
                    body.Item().Element(c => Field(c, "PHONE NUMBER / رقم الهاتف", invoice.Phone));
                    body.Item().Element(c => Field(c, "ADDRESS / العنوان", invoice.Address));
                });
            });

            row.ConstantItem(8);

            row.RelativeItem().Border(1).BorderColor(Colors.Black).Column(card =>
            {
                card.Item().Background(Colors.Grey.Darken3).Padding(5)
                    .Text("VEHICLE INFORMATION / بيانات السيارة")
                    .FontColor(Colors.White).SemiBold().FontSize(8);
                card.Item().Padding(7).Column(body =>
                {
                    body.Item().Element(c => Field(c, "LICENSE PLATE NO. / رقم اللوحة", invoice.PlateNumber));
                    body.Item().Element(c => Field(c, "PLATE LETTERS / حروف اللوحة", invoice.PlateLetters));
                    body.Item().Element(c => Field(c, "CHASSIS NUMBER / رقم الشاسيه",
                        string.IsNullOrWhiteSpace(invoice.ChassisNumber) ? "—" : invoice.ChassisNumber));
                    body.Item().Element(c => Field(c, "ODOMETER READING / قراءة العداد", invoice.Odometer));
                    body.Item().Element(c => Field(c, "CAR BRAND / MODEL / الماركة / الموديل", invoice.CarModel));
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

    private static void ItemsTable(IContainer container, List<InvoiceItemRecord> rows)
    {
        container.Border(1).BorderColor(Colors.Black).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(22);
                c.RelativeColumn(5);
                c.ConstantColumn(48);
                c.ConstantColumn(62);
                c.ConstantColumn(62);
            });

            table.Header(h =>
            {
                h.Cell().Element(HeaderCell).AlignCenter().Text("#").FontColor(Colors.White).Bold().FontSize(8);
                h.Cell().Element(HeaderCell).AlignCenter().Text("ITEM / SERVICE / الصنف / الخدمة").FontColor(Colors.White).Bold().FontSize(8);
                h.Cell().Element(HeaderCell).AlignCenter().Text("QTY.\nالكمية").FontColor(Colors.White).Bold().FontSize(7);
                h.Cell().Element(HeaderCell).AlignCenter().Text("UNIT PRICE\nسعر الوحدة").FontColor(Colors.White).Bold().FontSize(7);
                h.Cell().Element(HeaderCell).AlignCenter().Text("TOTAL\nالإجمالي").FontColor(Colors.White).Bold().FontSize(7);
            });

            for (var i = 0; i < rows.Count; i++)
            {
                var item = rows[i];
                var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten3;
                var hasItem = !string.IsNullOrWhiteSpace(item.Name);

                table.Cell().Element(c => IndexCell(c)).AlignCenter()
                    .Text(hasItem || i < rows.Count ? (i + 1).ToString() : "").FontColor(Colors.White).FontSize(7);
                table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.Name).FontSize(8);
                table.Cell().Element(c => BodyCell(c, bg)).AlignCenter()
                    .Text(item.Qty > 0 ? item.Qty.ToString() : "").FontSize(8);
                table.Cell().Element(c => BodyCell(c, bg)).AlignCenter()
                    .Text(item.UnitPrice > 0 ? item.UnitPrice.ToString("N2") : "").FontSize(8);
                table.Cell().Element(c => BodyCell(c, bg)).AlignCenter()
                    .Text(item.Qty > 0 ? item.Total.ToString("N2") : "").FontSize(8);
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

    private static void BottomSection(IContainer container, InvoiceRecord invoice)
    {
        container.Row(row =>
        {
            row.RelativeItem(2).Border(1).BorderColor(Colors.Black).Column(card =>
            {
                card.Item().Background(Colors.Grey.Darken3).Padding(4)
                    .Text("PAYMENT METHOD / طريقة الدفع").FontColor(Colors.White).SemiBold().FontSize(8);
                card.Item().Padding(6).Column(body =>
                {
                    body.Item().Element(c => Pay(c, "CASH / نقدي", IsPay(invoice, "نقدي")));
                    body.Item().Element(c => Pay(c, "CARD / فيزا", IsPay(invoice, "فيزا")));
                    body.Item().Element(c => Pay(c, "TRANSFER / انستا باي", IsPay(invoice, "انستا")));
                    body.Item().Element(c => Pay(c, "OTHER / أخرى", false));
                });
            });

            row.ConstantItem(6);

            row.RelativeItem(2).Border(1).BorderColor(Colors.Black).Column(card =>
            {
                card.Item().Background(Colors.Grey.Darken3).Padding(4)
                    .Text("NOTES / ملاحظات").FontColor(Colors.White).SemiBold().FontSize(8);
                card.Item().Padding(6).Column(body =>
                {
                    for (var i = 0; i < 5; i++)
                    {
                        body.Item().PaddingTop(7).BorderBottom(0.7f).BorderColor(Colors.Grey.Lighten1).Height(9);
                    }
                });
            });

            row.ConstantItem(6);

            row.RelativeItem(2.5f).Border(1).BorderColor(Colors.Black).Column(card =>
            {
                card.Item().Element(c => Total(c, "SUBTOTAL / الإجمالي الفرعي", invoice.Subtotal.ToString("N2"), false));
                card.Item().Element(c => Total(c, "DISCOUNT / الخصم", invoice.Discount.ToString("N2"), false));
                card.Item().Element(c => Total(c, "TAX (0%) / الضريبة", invoice.Tax.ToString("N2"), false));
                card.Item().Element(c => Total(c, "TOTAL AMOUNT / المجموع الكلي", invoice.GrandTotal.ToString("N2"), true));
                card.Item().Element(c => Total(c, "PAID AMOUNT / المبلغ المدفوع", invoice.Paid.ToString("N2"), false));
                card.Item().Element(c => Total(c, "DUE AMOUNT / المبلغ المتبقي", invoice.Remaining.ToString("N2"), false));
            });
        });
    }

    private static void Pay(IContainer container, string label, bool selected)
    {
        container.PaddingBottom(3).Row(row =>
        {
            row.RelativeItem().Text(label).FontSize(7.5f);
            row.ConstantItem(14).Height(12).Border(1).BorderColor(Colors.Black)
                .Background(selected ? Colors.Black : Colors.White)
                .AlignCenter().AlignMiddle()
                .Text(selected ? "✓" : "").FontColor(Colors.White).FontSize(7);
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
            row.RelativeItem().Text(label)
                .FontSize(7)
                .FontColor(highlight ? Colors.White : Colors.Black)
                .SemiBold();
            row.ConstantItem(55).AlignRight().Text(value)
                .FontSize(8)
                .FontColor(highlight ? Colors.White : Colors.Black)
                .Bold();
        });
    }

    private static void TrustBadges(IContainer container)
    {
        container.PaddingTop(2).Row(row =>
        {
            Badge(row, "QUALITY YOU CAN TRUST", "Genuine Parts");
            Badge(row, "EXPERT SERVICE", "Skilled Technicians");
            Badge(row, "POWER IN EVERY PART", "Built for Performance");
        });

        static void Badge(RowDescriptor row, string title, string sub)
        {
            row.RelativeItem().AlignCenter().Column(c =>
            {
                c.Item().AlignCenter().Width(24).Height(24).Border(1).BorderColor(Colors.Black)
                    .AlignCenter().AlignMiddle().Text("★").FontSize(9);
                c.Item().AlignCenter().Text(title).FontSize(6.5f).SemiBold();
                c.Item().AlignCenter().Text(sub).FontSize(6).FontColor(Colors.Grey.Darken2);
            });
        }
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
            row.RelativeItem().AlignMiddle().AlignRight().Text("شكراً لثقتكم بنا").FontColor(Colors.White).Bold().FontSize(11);
        });
    }

    private static bool IsPay(InvoiceRecord invoice, string key) =>
        invoice.PaymentMethod.Contains(key, StringComparison.OrdinalIgnoreCase);

    private static byte[]? LoadBlackAndWhiteLogoBytes()
    {
        var bwPath = Path.Combine(AppContext.BaseDirectory, "Assets", "logo-ibrahim-bw.png");
        var colorPath = Path.Combine(AppContext.BaseDirectory, "Assets", "logo-ibrahim.png");

        try
        {
            if (File.Exists(bwPath))
            {
                return File.ReadAllBytes(bwPath);
            }

            if (!File.Exists(colorPath))
            {
                return null;
            }

            using var src = new Bitmap(colorPath);
            using var bw = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            for (var y = 0; y < src.Height; y++)
            {
                for (var x = 0; x < src.Width; x++)
                {
                    var c = src.GetPixel(x, y);
                    if (c.A < 20)
                    {
                        bw.SetPixel(x, y, SdColor.Transparent);
                        continue;
                    }

                    var g = (int)(0.299 * c.R + 0.587 * c.G + 0.114 * c.B);
                    var v = g > 150 ? 255 : (int)(g * 0.45);
                    bw.SetPixel(x, y, SdColor.FromArgb(c.A, v, v, v));
                }
            }

            using var ms = new MemoryStream();
            bw.Save(ms, SdImageFormat.Png);
            var bytes = ms.ToArray();
            File.WriteAllBytes(bwPath, bytes);
            return bytes;
        }
        catch
        {
            return File.Exists(colorPath) ? File.ReadAllBytes(colorPath) : null;
        }
    }

    private static byte[] CreateQrPng(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(data).GetGraphic(5);
    }
}
