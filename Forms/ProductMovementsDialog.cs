using System.Runtime.InteropServices;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

/// <summary>Shows the recent stock movements of a single product.</summary>
internal sealed class ProductMovementsDialog : Form
{
    private static readonly Color InGreen = Color.FromArgb(80, 200, 120);

    private readonly ProductRecord _product;

    public ProductMovementsDialog(ProductRecord product)
    {
        _product = product;
        SuspendLayout();
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = $"حركات الصنف - {_product.Name}";
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = InvoiceTheme.Background;
        ForeColor = InvoiceTheme.White;
        Font = InvoiceTheme.BodyFont;
        ClientSize = new Size(720, 560);
        MinimumSize = new Size(560, 420);
        RightToLeft = RightToLeft.No;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = InvoiceTheme.Background,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = $"حركات المخزون  •  {_product.Name}",
            Font = InvoiceTheme.TitleFont,
            ForeColor = InvoiceTheme.Gold,
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.Yes
        };

        var subtitle = new Label
        {
            Dock = DockStyle.Fill,
            Text = $"الكود: {_product.Code}   |   الكمية الحالية: {_product.Quantity}   |   الوحدة: {_product.Unit}",
            Font = InvoiceTheme.SmallFont,
            ForeColor = InvoiceTheme.Muted,
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.Yes
        };

        var grid = BuildGrid();

        root.Controls.Add(title, 0, 0);
        root.Controls.Add(subtitle, 0, 1);
        root.Controls.Add(grid, 0, 2);
        Controls.Add(root);

        ResumeLayout(true);
    }

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_CAPTION_COLOR = 35;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            var useDark = 1;
            DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));

            var bg = InvoiceTheme.Background;
            var colorRef = bg.R | (bg.G << 8) | (bg.B << 16);
            DwmSetWindowAttribute(Handle, DWMWA_CAPTION_COLOR, ref colorRef, sizeof(int));
        }
        catch
        {
            // title-bar theming not supported on this OS; ignore
        }
    }

    private DataGridView BuildGrid()
    {
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
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 32 },
            Font = InvoiceTheme.BodyFont,
            ForeColor = InvoiceTheme.White,
            RightToLeft = RightToLeft.Yes,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = InvoiceTheme.Card,
                ForeColor = InvoiceTheme.White,
                SelectionBackColor = Color.FromArgb(18, 18, 18),
                SelectionForeColor = InvoiceTheme.White,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = InvoiceTheme.RowAlt,
                ForeColor = InvoiceTheme.White,
                SelectionBackColor = Color.FromArgb(18, 18, 18),
                SelectionForeColor = InvoiceTheme.White,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = InvoiceTheme.Gold,
                ForeColor = Color.Black,
                Font = InvoiceTheme.TableHeaderFont,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            ColumnHeadersHeight = 34
        };

        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIndex", HeaderText = "#", FillWeight = 8 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType", HeaderText = "النوع", FillWeight = 18 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty", HeaderText = "الكمية", FillWeight = 14 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "التاريخ", FillWeight = 26 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNote", HeaderText = "البيان", FillWeight = 34 });

        var i = 1;
        foreach (var m in ProductStore.MovementsFor(_product.Id))
        {
            var isIn = m.Type == "Stock In";
            var invoice = ResolveInvoice(m);

            var detail = string.IsNullOrWhiteSpace(m.Note) ? "—" : m.Note;
            if (invoice is not null)
            {
                detail = $"{m.Note} — {invoice.Number}";
            }

            var r = grid.Rows.Add(
                i++,
                isIn ? "دخول" : "خروج",
                m.Quantity,
                m.At.ToString("dd/MM/yyyy HH:mm"),
                detail);
            grid.Rows[r].Cells["colType"].Style.ForeColor = isIn ? InGreen : InvoiceTheme.Danger;
            grid.Rows[r].Cells["colType"].Style.SelectionForeColor = isIn ? InGreen : InvoiceTheme.Danger;

            if (invoice is not null)
            {
                grid.Rows[r].Tag = invoice.Id;
                grid.Rows[r].Cells["colNote"].Style.ForeColor = InvoiceTheme.Gold;
                grid.Rows[r].Cells["colNote"].Style.SelectionForeColor = InvoiceTheme.Gold;
            }
        }

        if (grid.Rows.Count == 0)
        {
            grid.Enabled = false;
        }

        grid.CellClick += OnMovementRowClick;
        grid.ClearSelection();
        grid.CurrentCell = null;
        return grid;
    }

    private void OnMovementRowClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (sender is not DataGridView grid || e.RowIndex < 0)
        {
            return;
        }

        if (grid.Rows[e.RowIndex].Tag is not string invoiceId)
        {
            return;
        }

        var invoice = InvoiceStore.Find(invoiceId);
        if (invoice is null)
        {
            return;
        }

        using var dlg = new InvoiceDetailsDialog(invoice);
        dlg.ShowDialog(this);
    }

    private static InvoiceRecord? ResolveInvoice(StockMovementRecord m)
    {
        if (!string.IsNullOrWhiteSpace(m.InvoiceNumber))
        {
            var byNumber = InvoiceStore.FindByNumber(m.InvoiceNumber);
            if (byNumber is not null)
            {
                return byNumber;
            }
        }

        // Fallback for older movements without a stored invoice number:
        // match a sales invoice that contains this product around the same time.
        if (m.Type != "Stock Out" || !m.Note.Contains("فاتورة"))
        {
            return null;
        }

        return InvoiceStore.All
            .Where(inv => inv.Items.Any(it =>
                string.Equals(it.Name?.Trim(), m.ProductName.Trim(), StringComparison.OrdinalIgnoreCase)))
            .Where(inv => Math.Abs((inv.CreatedAt - m.At).TotalMinutes) <= 5)
            .OrderBy(inv => Math.Abs((inv.CreatedAt - m.At).TotalMinutes))
            .FirstOrDefault();
    }
}
