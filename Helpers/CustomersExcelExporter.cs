using System.Diagnostics;
using ClosedXML.Excel;
using IbrahimAbdo.Login.Data;

namespace IbrahimAbdo.Login.Helpers;

internal static class CustomersExcelExporter
{
    public static string Export(IReadOnlyList<CustomerRecord> customers, bool openAfter = true)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Reports");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"customers-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx");

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("العملاء");
        ws.RightToLeft = true;

        string[] headers =
        [
            "م",
            "اسم العميل",
            "رقم الهاتف",
            "البريد الإلكتروني",
            "العنوان",
            "رقم اللوحة",
            "عدد السيارات",
            "إجمالي الفواتير",
            "تاريخ التسجيل",
            "ملاحظات"
        ];

        for (var c = 0; c < headers.Length; c++)
        {
            ws.Cell(1, c + 1).Value = headers[c];
        }

        var headerRange = ws.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(48, 48, 48);
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        var row = 2;
        foreach (var customer in customers)
        {
            ws.Cell(row, 1).Value = row - 1;
            ws.Cell(row, 2).Value = customer.Name;
            ws.Cell(row, 3).Value = string.IsNullOrWhiteSpace(customer.Phone) ? "—" : customer.Phone;
            ws.Cell(row, 4).Value = string.IsNullOrWhiteSpace(customer.Email) ? "—" : customer.Email;
            ws.Cell(row, 5).Value = string.IsNullOrWhiteSpace(customer.Address) ? "—" : customer.Address;
            ws.Cell(row, 6).Value = customer.PrimaryPlate;
            ws.Cell(row, 7).Value = customer.CarsCount;
            ws.Cell(row, 8).Value = CustomerTotal(customer);
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 9).Value = customer.RegisteredAt;
            ws.Cell(row, 9).Style.DateFormat.Format = "dd/MM/yyyy";
            ws.Cell(row, 10).Value = string.IsNullOrWhiteSpace(customer.Notes) ? "—" : customer.Notes;
            row++;
        }

        var used = ws.Range(1, 1, Math.Max(1, row - 1), headers.Length);
        used.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        used.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        used.Style.Border.OutsideBorderColor = XLColor.FromArgb(200, 200, 200);
        used.Style.Border.InsideBorderColor = XLColor.FromArgb(220, 220, 220);

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);

        workbook.SaveAs(filePath);

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

    private static decimal CustomerTotal(CustomerRecord c)
    {
        var fromStore = InvoiceStore.All
            .Where(i => (!string.IsNullOrWhiteSpace(c.Phone) && i.Phone.Trim() == c.Phone.Trim()) ||
                        (!string.IsNullOrWhiteSpace(c.Name) && i.CustomerName.Trim() == c.Name.Trim()))
            .Sum(i => i.GrandTotal);

        return fromStore > 0 ? fromStore : c.TotalInvoices;
    }
}
