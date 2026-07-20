using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

/// <summary>
/// Invoice details + reprint using the shared PDF print template.
/// </summary>
internal sealed class InvoiceDetailsDialog : Form
{
    private readonly InvoiceRecord _invoice;

    public InvoiceDetailsDialog(InvoiceRecord invoice)
    {
        _invoice = invoice;
        SuspendLayout();
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = $"فاتورة {_invoice.Number}";
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = InvoiceTheme.Background;
        ForeColor = InvoiceTheme.White;
        Font = InvoiceTheme.BodyFont;
        ClientSize = new Size(920, 640);
        MinimumSize = new Size(760, 520);
        RightToLeft = RightToLeft.No;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = InvoiceTheme.Background,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = $"تفاصيل الفاتورة  •  {_invoice.Number}",
            Font = InvoiceTheme.TitleFont,
            ForeColor = InvoiceTheme.Gold,
            TextAlign = ContentAlignment.MiddleLeft
        };
        header.Controls.Add(title);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        body.Controls.Add(BuildInfoCards(), 0, 0);
        body.Controls.Add(BuildItemsCard(), 0, 1);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0)
        };
        footer.Controls.Add(CreateButton("إعادة طباعة", true, (_, _) => Reprint()));
        footer.Controls.Add(CreateButton("عرض تصميم الطباعة", false, (_, _) => OpenPrintLayout()));
        footer.Controls.Add(CreateButton("إغلاق", false, (_, _) => Close()));

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(body, 0, 1);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);

        // Open the print-layout PDF so the user sees the exact print design
        Shown += (_, _) =>
        {
            try
            {
                InvoicePdfGenerator.Generate(_invoice, openAfter: true);
            }
            catch
            {
                // details UI still available
            }
        };

        ResumeLayout(true);
    }

    private Control BuildInfoCards()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 8)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));

        row.Controls.Add(InfoCard("بيانات الفاتورة",
            $"رقم الفاتورة: {_invoice.Number}",
            $"التاريخ: {_invoice.CreatedAt:dd/MM/yyyy HH:mm}",
            $"الدفع: {_invoice.PaymentMethod}",
            $"الفني: {_invoice.Technician}"), 0, 0);

        row.Controls.Add(InfoCard("بيانات العميل",
            $"الاسم: {_invoice.CustomerName}",
            $"الهاتف: {_invoice.Phone}",
            $"العنوان: {_invoice.Address}"), 1, 0);

        var plate = $"{_invoice.PlateLetters} {_invoice.PlateNumber}".Trim();
        row.Controls.Add(InfoCard("بيانات السيارة",
            $"الموديل: {_invoice.CarModel}",
            $"اللوحة: {(string.IsNullOrWhiteSpace(plate) ? "—" : plate)}",
            $"الهيكل: {_invoice.ChassisNumber}",
            $"العداد: {_invoice.Odometer}"), 2, 0);

        return row;
    }

    private Control BuildItemsCard()
    {
        var card = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Padding = new Padding(10),
            ShadowDecoration = { Enabled = true, Depth = 8, Color = Color.Black, BorderRadius = InvoiceTheme.Radius }
        };

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Text = "أصناف / خدمات الفاتورة",
            ForeColor = InvoiceTheme.Gold,
            Font = InvoiceTheme.SectionFont,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var totals = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 34,
            Text = $"الإجمالي: {_invoice.GrandTotal:N2} ج.م   |   المدفوع: {_invoice.Paid:N2}   |   المتبقي: {_invoice.Remaining:N2}   |   الخصم: {_invoice.Discount:N2}",
            ForeColor = InvoiceTheme.Gold,
            Font = InvoiceTheme.BodyFont,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = InvoiceTheme.Card,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            EnableHeadersVisualStyles = false,
            GridColor = InvoiceTheme.CardBorder,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 32 },
            Font = InvoiceTheme.SmallFont,
            ForeColor = InvoiceTheme.White,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = InvoiceTheme.Card,
                ForeColor = InvoiceTheme.White,
                SelectionBackColor = Color.FromArgb(48, InvoiceTheme.Gold),
                SelectionForeColor = InvoiceTheme.White
            },
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = InvoiceTheme.Gold,
                Font = InvoiceTheme.TableHeaderFont
            },
            ColumnHeadersHeight = 32
        };
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "#", FillWeight = 8 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الصنف / الخدمة", FillWeight = 40 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الكمية", FillWeight = 12 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "السعر", FillWeight = 20 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الإجمالي", FillWeight = 20 });

        var i = 1;
        foreach (var item in _invoice.Items.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
        {
            grid.Rows.Add(i++, item.Name, item.Qty, $"{item.UnitPrice:N2}", $"{item.Total:N2}");
        }

        card.Controls.Add(grid);
        card.Controls.Add(totals);
        card.Controls.Add(title);
        return card;
    }

    private static Control InfoCard(string title, params string[] lines)
    {
        var card = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Margin = new Padding(4),
            Padding = new Padding(10, 8, 10, 8)
        };
        var lblTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = title,
            ForeColor = InvoiceTheme.Gold,
            Font = InvoiceTheme.SectionFont
        };
        var body = new Label
        {
            Dock = DockStyle.Fill,
            Text = string.Join("\r\n", lines),
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.SmallFont
        };
        card.Controls.Add(body);
        card.Controls.Add(lblTitle);
        return card;
    }

    private void OpenPrintLayout()
    {
        try
        {
            InvoicePdfGenerator.Generate(_invoice, openAfter: true);
        }
        catch (Exception ex)
        {
            AppMessageDialog.Error(this, $"تعذر فتح تصميم الطباعة.\r\n{ex.Message}", "عرض");
        }
    }

    private void Reprint()
    {
        try
        {
            InvoicePdfGenerator.GenerateAndOpen(_invoice);
            AppMessageDialog.Success(this, "تم فتح الفاتورة للطباعة بنفس التصميم.", "إعادة طباعة");
        }
        catch (Exception ex)
        {
            AppMessageDialog.Error(this, $"تعذر إعادة الطباعة.\r\n{ex.Message}", "إعادة طباعة");
        }
    }

    private static Guna2Button CreateButton(string text, bool primary, EventHandler onClick)
    {
        var btn = new Guna2Button
        {
            Text = text,
            Font = InvoiceTheme.SmallFont,
            Height = 38,
            MinimumSize = new Size(120, 38),
            BorderRadius = 8,
            Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand,
            ForeColor = primary ? Color.Black : InvoiceTheme.White,
            FillColor = primary ? InvoiceTheme.Gold : InvoiceTheme.Card,
            BorderColor = InvoiceTheme.Gold,
            BorderThickness = primary ? 0 : 1,
            HoverState =
            {
                FillColor = primary ? InvoiceTheme.GoldDark : Color.FromArgb(30, InvoiceTheme.Gold),
                ForeColor = primary ? Color.Black : InvoiceTheme.White
            }
        };
        var w = TextRenderer.MeasureText(text, btn.Font).Width;
        btn.Width = Math.Max(btn.MinimumSize.Width, w + 36);
        btn.Click += onClick;
        return btn;
    }
}
