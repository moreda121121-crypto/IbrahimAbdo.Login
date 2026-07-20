using Guna.UI2.WinForms;
using IbrahimAbdo.Login.Data;
using IbrahimAbdo.Login.Helpers;
using IbrahimAbdo.Login.Theme;

namespace IbrahimAbdo.Login.Forms;

/// <summary>Purchase invoice — same visual system as sales invoice; increases product stock on save.</summary>
internal sealed class PurchaseInvoiceForm : Form
{
    private readonly System.Windows.Forms.Timer _clockTimer = new() { Interval = 1000 };
    private readonly bool _embedded;
    private readonly List<PurchaseLine> _lines = [];

    private Label _lblTime = null!;
    private Label _lblDate = null!;
    private Label _lblSubtotal = null!;
    private Label _lblGrandTotal = null!;
    private Label _lblRemaining = null!;
    private Guna2TextBox _txtSupplier = null!;
    private Guna2TextBox _txtPhone = null!;
    private Guna2TextBox _txtNotes = null!;
    private Guna2TextBox _txtDiscount = null!;
    private Guna2TextBox _txtPaid = null!;
    private Guna2ComboBox _cmbDiscountUnit = null!;
    private Guna2ComboBox _cmbPayment = null!;
    private DataGridView _grid = null!;
    private Bitmap _editIcon = null!;
    private Bitmap _deleteIcon = null!;

    private sealed record PurchaseLine(int Id, string Name, int Qty, decimal UnitPrice, string? ProductId = null)
    {
        public decimal Total => Qty * UnitPrice;
    }

    public PurchaseInvoiceForm(bool embedded = false)
    {
        _embedded = embedded;
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = InvoiceTheme.Background;
        ClientSize = new Size(1280, 720);
        DoubleBuffered = true;
        Font = InvoiceTheme.BodyFont;
        ForeColor = InvoiceTheme.White;
        FormBorderStyle = _embedded ? FormBorderStyle.None : FormBorderStyle.Sizable;
        MaximizeBox = !_embedded;
        MinimizeBox = !_embedded;
        MinimumSize = _embedded ? Size.Empty : new Size(1100, 650);
        Name = "PurchaseInvoiceForm";
        RightToLeft = RightToLeft.No;
        ShowIcon = !_embedded;
        ShowInTaskbar = !_embedded;
        StartPosition = FormStartPosition.Manual;
        Text = _embedded ? string.Empty : "فاتورة شراء - Ibrahim Abdo Auto Service";

        ProductStore.Load();
        PurchaseInvoiceStore.Load();
        _editIcon = GlyphHelper.Create("\uE70F", InvoiceTheme.Gold, 14);
        _deleteIcon = GlyphHelper.Create("\uE74D", InvoiceTheme.Danger, 14);

        var main = BuildMain();
        main.Dock = DockStyle.Fill;
        Controls.Add(main);

        LoadGrid();
        RecalculateTotals();
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();
        ResumeLayout(true);
    }

    private Control BuildMain()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = InvoiceTheme.Background,
            Padding = new Padding(14, 8, 14, 10)
        };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        main.Controls.Add(BuildTopBar(), 0, 0);
        main.Controls.Add(BuildSupplierCard(), 0, 1);
        main.Controls.Add(BuildCenter(), 0, 2);
        main.Controls.Add(BuildBottomActions(), 0, 3);
        return main;
    }

    private Control BuildTopBar()
    {
        var bar = new Panel { Dock = DockStyle.Fill, BackColor = InvoiceTheme.Background };
        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "فاتورة شراء",
            Font = new Font(InvoiceTheme.Family.FontFamily, 20F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = InvoiceTheme.White,
            TextAlign = ContentAlignment.MiddleCenter
        };
        var right = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 12, 0, 0)
        };
        _lblDate = new Label { AutoSize = true, ForeColor = InvoiceTheme.Muted, Font = InvoiceTheme.SmallFont, Margin = new Padding(8, 8, 12, 0) };
        _lblTime = new Label { AutoSize = true, ForeColor = InvoiceTheme.Muted, Font = InvoiceTheme.SmallFont, Margin = new Padding(8, 8, 8, 0) };
        right.Controls.Add(CreateChrome("\uE7E7"));
        right.Controls.Add(_lblDate);
        right.Controls.Add(CreateChrome("\uE121"));
        right.Controls.Add(_lblTime);
        bar.Controls.Add(title);
        bar.Controls.Add(right);
        return bar;
    }

    private Control BuildSupplierCard()
    {
        var card = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Margin = new Padding(4),
            Padding = new Padding(14, 10, 14, 12),
            ShadowDecoration = { Enabled = true, Depth = 10, Color = Color.Black, BorderRadius = InvoiceTheme.Radius }
        };

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Text = "بيانات المورد",
            ForeColor = InvoiceTheme.Gold,
            Font = InvoiceTheme.SectionFont,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0)
        };
        for (var i = 0; i < 4; i++)
        {
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        }

        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        _txtSupplier = CreateInput("اسم المورد");
        _txtPhone = CreateInput("رقم الهاتف");
        _cmbPayment = CreateCombo("نقدي", "تحويل", "آجل");
        _txtNotes = CreateInput("ملاحظات");

        var supplierCombo = CreateCombo(ProductStore.Suppliers);
        supplierCombo.SelectedIndexChanged += (_, _) =>
        {
            if (supplierCombo.SelectedItem is string s && !string.IsNullOrWhiteSpace(s))
            {
                _txtSupplier.Text = s;
            }
        };

        grid.Controls.Add(WrapField("اسم المورد", _txtSupplier), 0, 0);
        grid.Controls.Add(WrapField("الهاتف", _txtPhone), 1, 0);
        grid.Controls.Add(WrapField("طريقة الدفع", _cmbPayment), 2, 0);
        grid.Controls.Add(WrapField("ملاحظات", _txtNotes), 3, 0);
        grid.Controls.Add(WrapField("اختيار مورد سريع", supplierCombo), 0, 1);

        card.Controls.Add(grid);
        card.Controls.Add(title);
        return card;
    }

    private Control BuildCenter()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
        row.Controls.Add(BuildGridCard(), 0, 0);
        row.Controls.Add(BuildSummary(), 1, 0);
        return row;
    }

    private Control BuildGridCard()
    {
        var card = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Margin = new Padding(4),
            Padding = new Padding(10),
            ShadowDecoration = { Enabled = true, Depth = 10, Color = Color.Black, BorderRadius = InvoiceTheme.Radius }
        };

        var header = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Text = "أصناف الشراء",
            ForeColor = InvoiceTheme.Gold,
            Font = InvoiceTheme.SectionFont
        };

        _grid = new DataGridView
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
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 34 },
            Font = InvoiceTheme.SmallFont,
            ForeColor = InvoiceTheme.White,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = InvoiceTheme.Card,
                ForeColor = InvoiceTheme.White,
                SelectionBackColor = Color.FromArgb(48, InvoiceTheme.Gold),
                SelectionForeColor = InvoiceTheme.White
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = InvoiceTheme.RowAlt,
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
            ColumnHeadersHeight = 34
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIndex", HeaderText = "#", FillWeight = 6 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "الصنف", FillWeight = 34 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty", HeaderText = "الكمية", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice", HeaderText = "سعر الشراء", FillWeight = 16 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal", HeaderText = "الإجمالي", FillWeight = 16 });
        _grid.Columns.Add(new DataGridViewImageColumn { Name = "colEdit", HeaderText = "", FillWeight = 8, Image = _editIcon, ImageLayout = DataGridViewImageCellLayout.Zoom });
        _grid.Columns.Add(new DataGridViewImageColumn { Name = "colDelete", HeaderText = "", FillWeight = 8, Image = _deleteIcon, ImageLayout = DataGridViewImageCellLayout.Zoom });
        _grid.CellClick += OnGridClick;

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 6, 0, 0)
        };
        actions.Controls.Add(CreateOutline("+ إضافة صنف", (_, _) => AddLine()));
        actions.Controls.Add(CreateOutline("حذف الكل", (_, _) =>
        {
            _lines.Clear();
            LoadGrid();
            RecalculateTotals();
        }, InvoiceTheme.Danger));

        card.Controls.Add(_grid);
        card.Controls.Add(actions);
        card.Controls.Add(header);
        return card;
    }

    private Control BuildSummary()
    {
        var card = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = InvoiceTheme.Card,
            BorderColor = InvoiceTheme.CardBorder,
            BorderThickness = 1,
            BorderRadius = InvoiceTheme.Radius,
            Margin = new Padding(4),
            Padding = new Padding(16, 12, 16, 12),
            ShadowDecoration = { Enabled = true, Depth = 10, Color = Color.Black, BorderRadius = InvoiceTheme.Radius }
        };

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            Text = "ملخص فاتورة الشراء",
            Font = InvoiceTheme.SectionFont,
            ForeColor = InvoiceTheme.Gold,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            BackColor = Color.Transparent
        };
        for (var i = 0; i < 8; i++)
        {
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        }

        _lblSubtotal = SummaryValue();
        _lblGrandTotal = SummaryValue(true);
        _lblRemaining = SummaryValue();
        _txtDiscount = CreateInput("0");
        _txtDiscount.TextChanged += (_, _) => RecalculateTotals();
        _cmbDiscountUnit = CreateCombo("%", "ج.م");
        _cmbDiscountUnit.SelectedIndexChanged += (_, _) => RecalculateTotals();
        _txtPaid = CreateInput("0");
        _txtPaid.TextChanged += (_, _) => RecalculateTotals();

        body.Controls.Add(SummaryRow("الإجمالي الفرعي", _lblSubtotal), 0, 0);
        var discRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        discRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        discRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
        discRow.Controls.Add(_txtDiscount, 0, 0);
        discRow.Controls.Add(_cmbDiscountUnit, 1, 0);
        body.Controls.Add(Labeled("الخصم", discRow), 0, 1);
        body.Controls.Add(SummaryRow("الإجمالي", _lblGrandTotal), 0, 2);
        body.Controls.Add(Labeled("المدفوع", _txtPaid), 0, 3);
        body.Controls.Add(SummaryRow("المتبقي", _lblRemaining), 0, 4);

        card.Controls.Add(body);
        card.Controls.Add(title);
        return card;
    }

    private Control BuildBottomActions()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        for (var i = 0; i < 3; i++)
        {
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        }

        bar.Controls.Add(WrapBottom(CreateBottom("جديد", "\uE710", (_, _) => ClearForm())), 0, 0);
        bar.Controls.Add(WrapBottom(CreateBottom("حفظ الفاتورة", "\uE74E", (_, _) => SavePurchase())), 1, 0);
        bar.Controls.Add(WrapBottom(CreateBottom("حفظ وزيادة المخزون", "\uE8F1", (_, _) => SavePurchase())), 2, 0);
        return bar;
    }

    private void AddLine()
    {
        using var dlg = new SelectProductDialog(forPurchase: true);
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK || dlg.Selected is null)
        {
            return;
        }

        var product = dlg.Selected;
        var existing = _lines.FindIndex(l =>
            (!string.IsNullOrEmpty(l.ProductId) && l.ProductId == product.Id) ||
            l.Name.Equals(product.Name, StringComparison.OrdinalIgnoreCase));
        if (existing >= 0)
        {
            var line = _lines[existing];
            _lines[existing] = line with { Qty = line.Qty + 1 };
        }
        else
        {
            _lines.Add(new PurchaseLine(
                _lines.Count + 1,
                product.Name,
                1,
                product.PurchasePrice,
                product.Id));
        }

        LoadGrid();
        RecalculateTotals();
    }

    private void OnGridClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        var col = _grid.Columns[e.ColumnIndex].Name;
        if (col == "colDelete")
        {
            _lines.RemoveAt(e.RowIndex);
            LoadGrid();
            RecalculateTotals();
        }
        else if (col == "colEdit")
        {
            var line = _lines[e.RowIndex];
            using var qtyDlg = new Form
            {
                Text = "تعديل الكمية",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(320, 140),
                BackColor = InvoiceTheme.Background,
                ForeColor = InvoiceTheme.White,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false
            };
            var lbl = new Label
            {
                Text = $"كمية «{line.Name}»",
                Dock = DockStyle.Top,
                Height = 28,
                ForeColor = InvoiceTheme.Gold,
                Font = InvoiceTheme.BodyFont,
                Padding = new Padding(12, 8, 12, 0)
            };
            var txt = CreateInput(line.Qty.ToString());
            txt.Text = line.Qty.ToString();
            txt.Dock = DockStyle.Top;
            txt.Height = 36;
            txt.Margin = new Padding(12);
            var ok = CreateOutline("موافق", (_, _) =>
            {
                if (int.TryParse(txt.Text.Trim(), out var qty) && qty > 0)
                {
                    _lines[e.RowIndex] = line with { Qty = qty };
                    LoadGrid();
                    RecalculateTotals();
                    qtyDlg.DialogResult = DialogResult.OK;
                    qtyDlg.Close();
                }
                else
                {
                    AppMessageDialog.Warning(qtyDlg, "أدخل كمية صحيحة");
                }
            });
            ok.Dock = DockStyle.Bottom;
            ok.Height = 36;
            qtyDlg.Controls.Add(ok);
            qtyDlg.Controls.Add(txt);
            qtyDlg.Controls.Add(lbl);
            qtyDlg.Padding = new Padding(12);
            qtyDlg.ShowDialog(FindForm());
        }
    }

    private void SavePurchase()
    {
        if (string.IsNullOrWhiteSpace(_txtSupplier.Text))
        {
            AppMessageDialog.Warning(this, "برجاء إدخال اسم المورد");
            return;
        }

        if (_lines.Count == 0)
        {
            AppMessageDialog.Warning(this, "أضف صنفاً واحداً على الأقل");
            return;
        }

        RecalculateTotals();
        _ = decimal.TryParse(_txtDiscount.Text.Trim(), out var discountValue);
        var discountUnit = _cmbDiscountUnit.SelectedItem?.ToString() ?? "%";
        var subtotal = _lines.Sum(l => l.Total);
        var discount = discountUnit == "%" ? subtotal * discountValue / 100m : discountValue;
        var grand = Math.Max(0, subtotal - discount);
        _ = decimal.TryParse(_txtPaid.Text.Trim(), out var paid);
        if (paid > grand)
        {
            paid = grand;
        }

        // Increase stock for each product line
        foreach (var line in _lines)
        {
            if (!ProductStore.ApplyPurchase(line.ProductId, line.Name, line.Qty, line.UnitPrice, out var err))
            {
                AppMessageDialog.Error(this, err ?? "تعذر تحديث المخزون", "فاتورة شراء");
                return;
            }
        }

        var invoice = new PurchaseInvoiceRecord
        {
            Number = PurchaseInvoiceStore.NextNumber(),
            CreatedAt = DateTime.Now,
            SupplierName = _txtSupplier.Text.Trim(),
            Phone = _txtPhone.Text.Trim(),
            Notes = _txtNotes.Text.Trim(),
            PaymentMethod = _cmbPayment.Text.Trim(),
            Subtotal = subtotal,
            Discount = discount,
            GrandTotal = grand,
            Paid = paid,
            Items = _lines.Select(l => new PurchaseInvoiceItemRecord
            {
                ProductId = l.ProductId,
                Name = l.Name,
                Qty = l.Qty,
                UnitPrice = l.UnitPrice
            }).ToList()
        };
        PurchaseInvoiceStore.Add(invoice);

        AppMessageDialog.Success(this,
            $"تم حفظ فاتورة الشراء {invoice.Number}\r\nوتم زيادة كميات الأصناف في المخزون.",
            "فاتورة شراء");
        ClearForm();
    }

    private void ClearForm()
    {
        _lines.Clear();
        _txtSupplier.Text = "";
        _txtPhone.Text = "";
        _txtNotes.Text = "";
        _txtDiscount.Text = "0";
        _txtPaid.Text = "0";
        _cmbPayment.SelectedIndex = -1;
        LoadGrid();
        RecalculateTotals();
    }

    private void LoadGrid()
    {
        _grid.Rows.Clear();
        var i = 1;
        foreach (var line in _lines)
        {
            _grid.Rows.Add(i++, line.Name, line.Qty, $"{line.UnitPrice:N2}", $"{line.Total:N2}", _editIcon, _deleteIcon);
        }
    }

    private void RecalculateTotals()
    {
        var subtotal = _lines.Sum(l => l.Total);
        _ = decimal.TryParse(_txtDiscount?.Text, out var discountValue);
        var discount = _cmbDiscountUnit?.SelectedItem?.ToString() == "%"
            ? subtotal * discountValue / 100m
            : discountValue;
        var grand = Math.Max(0, subtotal - discount);
        _ = decimal.TryParse(_txtPaid?.Text, out var paid);
        if (paid > grand)
        {
            paid = grand;
        }

        if (_lblSubtotal is not null)
        {
            _lblSubtotal.Text = $"{subtotal:N2} ج.م";
        }

        if (_lblGrandTotal is not null)
        {
            _lblGrandTotal.Text = $"{grand:N2} ج.م";
        }

        if (_lblRemaining is not null)
        {
            _lblRemaining.Text = $"{Math.Max(0, grand - paid):N2} ج.م";
        }
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        _lblTime.Text = now.ToString("hh:mm tt");
        _lblDate.Text = now.ToString("dd MMM yyyy");
    }

    private static Guna2TextBox CreateInput(string placeholder) =>
        new()
        {
            Dock = DockStyle.Fill,
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(90, InvoiceTheme.Gold),
            FillColor = InvoiceTheme.InputFill,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.BodyFont,
            PlaceholderText = placeholder,
            PlaceholderForeColor = InvoiceTheme.Muted,
            FocusedState = { BorderColor = InvoiceTheme.Gold }
        };

    private static Guna2ComboBox CreateCombo(params string[] items)
    {
        var c = new Guna2ComboBox
        {
            Dock = DockStyle.Fill,
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = Color.FromArgb(90, InvoiceTheme.Gold),
            FillColor = InvoiceTheme.InputFill,
            ForeColor = InvoiceTheme.White,
            Font = InvoiceTheme.BodyFont,
            FocusedState = { BorderColor = InvoiceTheme.Gold }
        };
        foreach (var item in items)
        {
            c.Items.Add(item);
        }

        return c;
    }

    private static Control WrapField(string label, Control input)
    {
        var host = new Panel { Dock = DockStyle.Fill, Margin = new Padding(4), BackColor = Color.Transparent };
        var lbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = label,
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont
        };
        input.Dock = DockStyle.Fill;
        host.Controls.Add(input);
        host.Controls.Add(lbl);
        return host;
    }

    private static Label SummaryValue(bool highlight = false) =>
        new()
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = InvoiceTheme.Gold,
            Font = highlight ? InvoiceTheme.TotalFont : InvoiceTheme.SectionFont,
            Text = "0.00 ج.م"
        };

    private static Control SummaryRow(string label, Label value)
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        row.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = label,
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        row.Controls.Add(value, 1, 0);
        return row;
    }

    private static Control Labeled(string label, Control control)
    {
        var host = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var lbl = new Label
        {
            Dock = DockStyle.Top,
            Height = 18,
            Text = label,
            ForeColor = InvoiceTheme.Muted,
            Font = InvoiceTheme.SmallFont
        };
        control.Dock = DockStyle.Fill;
        host.Controls.Add(control);
        host.Controls.Add(lbl);
        return host;
    }

    private static Guna2Button CreateOutline(string text, EventHandler onClick, Color? border = null)
    {
        var color = border ?? InvoiceTheme.Gold;
        var btn = new Guna2Button
        {
            Text = text,
            Font = InvoiceTheme.SmallFont,
            Height = 34,
            AutoSize = true,
            BorderRadius = 8,
            BorderThickness = 1,
            BorderColor = color,
            FillColor = Color.Transparent,
            ForeColor = color,
            Margin = new Padding(0, 0, 8, 0),
            Cursor = Cursors.Hand,
            HoverState = { FillColor = Color.FromArgb(30, color), ForeColor = color }
        };
        btn.Click += onClick;
        return btn;
    }

    private static Guna2Button CreateBottom(string text, string glyph, EventHandler onClick)
    {
        var btn = new Guna2Button
        {
            Text = text,
            Font = InvoiceTheme.MenuFont,
            Dock = DockStyle.Fill,
            BorderRadius = 10,
            FillColor = InvoiceTheme.Gold,
            ForeColor = Color.Black,
            Cursor = Cursors.Hand,
            Image = GlyphHelper.Create(glyph, Color.Black, 16),
            ImageSize = new Size(16, 16),
            ImageAlign = HorizontalAlignment.Left,
            HoverState = { FillColor = InvoiceTheme.GoldDark, ForeColor = Color.Black }
        };
        btn.Click += onClick;
        return btn;
    }

    private static Control WrapBottom(Control btn)
    {
        var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 8, 6, 4), BackColor = Color.Transparent };
        btn.Dock = DockStyle.Fill;
        p.Controls.Add(btn);
        return p;
    }

    private static Guna2Button CreateChrome(string glyph) =>
        new()
        {
            Size = new Size(34, 34),
            BorderRadius = 8,
            FillColor = Color.Transparent,
            Font = InvoiceTheme.IconFont,
            Text = glyph,
            ForeColor = InvoiceTheme.Muted,
            Margin = new Padding(2, 0, 2, 0)
        };

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _clockTimer.Stop();
        _clockTimer.Dispose();
        _editIcon.Dispose();
        _deleteIcon.Dispose();
        base.OnFormClosed(e);
    }
}
