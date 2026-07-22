using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

/// <summary>Purchase invoice details + print using the shared purchase PDF template.</summary>
internal sealed class PurchaseInvoiceDetailsDialog : Form
{
    private readonly PurchaseInvoiceRecord _invoice;

    public PurchaseInvoiceDetailsDialog(PurchaseInvoiceRecord invoice)
    {
        _invoice = invoice;
        SuspendLayout();
        WindowTheme.Attach(this);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = $"فاتورة شراء {_invoice.Number}";
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = InvoiceTheme.Background;
        ForeColor = InvoiceTheme.White;
        Font = InvoiceTheme.BodyFont;
        ClientSize = new Size(880, 600);
        MinimumSize = new Size(720, 500);
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
            Text = $"تفاصيل فاتورة الشراء  •  {_invoice.Number}",
            Font = InvoiceTheme.TitleFont,
            ForeColor = InvoiceTheme.Gold,
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.Yes
        };
        header.Controls.Add(title);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        body.Controls.Add(BuildInfoCard(), 0, 0);
        body.Controls.Add(BuildItemsCard(), 0, 1);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0)
        };
        footer.Controls.Add(CreateButton("طباعة", true, (_, _) => Print()));

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(body, 0, 1);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);

        ResumeLayout(true);
    }

    private Control BuildInfoCard()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 8)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        row.Controls.Add(InfoCard("بيانات الفاتورة",
            $"رقم الفاتورة: {_invoice.Number}",
            $"التاريخ: {_invoice.CreatedAt:dd/MM/yyyy HH:mm}",
            $"طريقة الدفع: {_invoice.PaymentMethod}"), 0, 0);

        row.Controls.Add(InfoCard("بيانات المورد",
            $"اسم المورد: {_invoice.SupplierName}",
            $"الهاتف: {_invoice.Phone}",
            $"ملاحظات: {_invoice.Notes}"), 1, 0);

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
            Text = "أصناف الفاتورة",
            ForeColor = InvoiceTheme.Gold,
            Font = InvoiceTheme.SectionFont,
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.Yes
        };

        var remaining = Math.Max(0, _invoice.GrandTotal - _invoice.Paid);
        var totals = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 34,
            Text = $"الإجمالي: {_invoice.GrandTotal:N2} ج.م   |   المدفوع: {_invoice.Paid:N2}   |   المتبقي: {remaining:N2}   |   الخصم: {_invoice.Discount:N2}",
            ForeColor = InvoiceTheme.Gold,
            Font = InvoiceTheme.BodyFont,
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.Yes
        };

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = InvoiceTheme.Card,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.Single,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            EnableHeadersVisualStyles = false,
            GridColor = InvoiceTheme.CardBorder,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 32 },
            Font = InvoiceTheme.SmallFont,
            ForeColor = InvoiceTheme.White,
            RightToLeft = RightToLeft.Yes,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = InvoiceTheme.Card,
                ForeColor = InvoiceTheme.White,
                SelectionBackColor = InvoiceTheme.Card,
                SelectionForeColor = InvoiceTheme.White,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = InvoiceTheme.Gold,
                Font = InvoiceTheme.TableHeaderFont,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            ColumnHeadersHeight = 32
        };
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "#", FillWeight = 8 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الصنف", FillWeight = 40 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الكمية", FillWeight = 12 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "سعر الشراء", FillWeight = 20 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الإجمالي", FillWeight = 20 });

        var i = 1;
        foreach (var item in _invoice.Items.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
        {
            grid.Rows.Add(i++, item.Name, item.Qty, $"{item.UnitPrice:N2}", $"{item.Total:N2}");
        }

        grid.ClearSelection();
        grid.CurrentCell = null;

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
            Height = 26,
            Text = title,
            ForeColor = InvoiceTheme.Gold,
            Font = InvoiceTheme.SectionFont,
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.Yes
        };

        var list = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = lines.Length,
            BackColor = Color.Transparent,
            RightToLeft = RightToLeft.Yes,
            Padding = new Padding(0, 4, 0, 0)
        };
        list.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        list.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        var labelColor = Color.FromArgb(170, 170, 170);
        for (var r = 0; r < lines.Length; r++)
        {
            list.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

            var raw = lines[r] ?? string.Empty;
            var sep = raw.IndexOf(':');
            var label = sep >= 0 ? raw.Substring(0, sep).Trim() : raw.Trim();
            var value = sep >= 0 ? raw.Substring(sep + 1).Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(value)) value = "—";

            var lblLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = label,
                ForeColor = labelColor,
                Font = InvoiceTheme.SmallFont,
                TextAlign = ContentAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes,
                Margin = new Padding(0)
            };
            var lblValue = new Label
            {
                Dock = DockStyle.Fill,
                Text = value,
                ForeColor = InvoiceTheme.White,
                Font = InvoiceTheme.BodyFont,
                TextAlign = ContentAlignment.MiddleLeft,
                RightToLeft = RightToLeft.No,
                Margin = new Padding(0)
            };

            list.Controls.Add(lblLabel, 1, r);
            list.Controls.Add(lblValue, 0, r);
        }

        card.Controls.Add(list);
        card.Controls.Add(lblTitle);
        return card;
    }

    private void Print()
    {
        try
        {
            PurchasePdfGenerator.GenerateAndOpen(_invoice);
            AppMessageDialog.Success(this, "تم طباعة الفاتورة.", "طباعة");
        }
        catch (Exception ex)
        {
            AppMessageDialog.Error(this, $"تعذر الطباعة.\r\n{ex.Message}", "طباعة");
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
