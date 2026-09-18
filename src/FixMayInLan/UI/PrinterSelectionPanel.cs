using FixMayInLan.Core;
using FixMayInLan.Models;

namespace FixMayInLan.UI;

public sealed class PrinterSelectionPanel : Panel
{
    private static readonly Color Navy =
        Color.FromArgb(20, 31, 49);

    private static readonly Color Blue =
        Color.FromArgb(35, 99, 235);

    private static readonly Color TextSecondary =
        Color.FromArgb(100, 116, 139);

    private readonly PrinterService _printerService;

    private readonly DataGridView _printerGrid = new();
    private readonly Button _refreshButton = new();
    private readonly Label _selectionLabel = new();

    private IReadOnlyList<PrinterInfo> _printers =
        Array.Empty<PrinterInfo>();

    public PrinterSelectionPanel(
        PrinterService printerService)
    {
        _printerService = printerService;

        Dock = DockStyle.Fill;
        BackColor = Color.White;
        Padding = new Padding(24);

        Control titlePanel = CreateTitlePanel();
        Control footerPanel = CreateFooterPanel();

        ConfigurePrinterGrid();

        Controls.Add(_printerGrid);
        Controls.Add(footerPanel);
        Controls.Add(titlePanel);

        _refreshButton.Click += async (_, _) =>
        {
            await LoadPrintersAsync();
        };

        _printerGrid.SelectionChanged += (_, _) =>
        {
            UpdateSelectedPrinter();
        };
    }

    public PrinterInfo? SelectedPrinter { get; private set; }

    public event Action<string>? StatusChanged;

    public event Action<PrinterInfo?>?
        SelectedPrinterChanged;

    public async Task LoadPrintersAsync()
    {
        SetBusy(true);

        try
        {
            StatusChanged?.Invoke(
                "Đang quét danh sách máy in...");

            _printers =
                await _printerService.GetPrintersAsync();

            _printerGrid.DataSource =
                _printers.ToList();

            if (_printers.Count == 0)
            {
                StatusChanged?.Invoke(
                    "Không tìm thấy máy in nào.");

                _selectionLabel.Text =
                    "Windows không trả về máy in nào.";
            }
            else
            {
                StatusChanged?.Invoke(
                    $"Tìm thấy {_printers.Count} máy in.");

                UpdateSelectedPrinter();
            }
        }
        catch (Exception exception)
        {
            SelectedPrinter = null;

            _printerGrid.DataSource = null;

            StatusChanged?.Invoke(
                $"Không thể quét máy in: {exception.Message}");

            MessageBox.Show(
                $"Không thể đọc danh sách máy in.\r\n\r\n" +
                exception.Message,
                "Lỗi quét máy in",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ConfigurePrinterGrid()
    {
        _printerGrid.Dock = DockStyle.Fill;

        _printerGrid.BackgroundColor = Color.White;
        _printerGrid.BorderStyle = BorderStyle.None;

        _printerGrid.ReadOnly = true;
        _printerGrid.MultiSelect = false;

        _printerGrid.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect;

        _printerGrid.AutoGenerateColumns = false;

        _printerGrid.AllowUserToAddRows = false;
        _printerGrid.AllowUserToDeleteRows = false;
        _printerGrid.AllowUserToResizeRows = false;

        _printerGrid.RowHeadersVisible = false;

        _printerGrid.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;

        _printerGrid.RowTemplate.Height = 34;

        _printerGrid.ColumnHeadersHeight = 40;

        _printerGrid.EnableHeadersVisualStyles = false;

        _printerGrid.ColumnHeadersDefaultCellStyle.BackColor =
            Color.FromArgb(237, 242, 250);

        _printerGrid.ColumnHeadersDefaultCellStyle.ForeColor =
            Navy;

        _printerGrid.ColumnHeadersDefaultCellStyle.Font =
            new Font("Segoe UI Semibold", 9.5F);

        AddTextColumn(
            "Tên máy in",
            nameof(PrinterInfo.Name),
            170);

        AddTextColumn(
            "Cổng",
            nameof(PrinterInfo.PortName),
            80);

        AddTextColumn(
            "Trạng thái",
            nameof(PrinterInfo.Status),
            75);

        AddTextColumn(
            "Vai trò",
            nameof(PrinterInfo.RoleDisplay),
            90);

        AddTextColumn(
            "Mặc định",
            nameof(PrinterInfo.DefaultDisplay),
            55);

        AddTextColumn(
            "Chia sẻ",
            nameof(PrinterInfo.SharedDisplay),
            75);

        _printerGrid.CellFormatting +=
            PrinterGridOnCellFormatting;
    }

    private void AddTextColumn(
        string headerText,
        string propertyName,
        float fillWeight)
    {
        DataGridViewTextBoxColumn column = new()
        {
            HeaderText = headerText,
            DataPropertyName = propertyName,
            FillWeight = fillWeight,
            SortMode =
                DataGridViewColumnSortMode.Automatic
        };

        _printerGrid.Columns.Add(column);
    }

private void PrinterGridOnCellFormatting(
    object? sender,
    DataGridViewCellFormattingEventArgs eventArgs)
{
    // Bảo vệ trường hợp index không hợp lệ.
    if (eventArgs.ColumnIndex < 0 ||
        eventArgs.ColumnIndex >=
        _printerGrid.Columns.Count)
    {
        return;
    }

    DataGridViewColumn column =
        _printerGrid.Columns[
            eventArgs.ColumnIndex];

    if (column.DataPropertyName !=
        nameof(PrinterInfo.Status))
    {
        return;
    }

    // CellStyle có thể được đánh dấu nullable
    // trong metadata của WinForms.
    DataGridViewCellStyle? cellStyle =
        eventArgs.CellStyle;

    if (cellStyle is null)
    {
        return;
    }

    string status =
        eventArgs.Value?.ToString()
        ?? string.Empty;

    cellStyle.ForeColor =
        status switch
        {
            "Sẵn sàng" =>
                Color.SeaGreen,

            "Đang in" =>
                Blue,

            "Ngoại tuyến" =>
                Color.Firebrick,

            "Đã dừng" =>
                Color.Firebrick,

            _ =>
                TextSecondary
        };

    cellStyle.Font =
        new Font(
            "Segoe UI Semibold",
            9F);
}

    private void UpdateSelectedPrinter()
    {
        SelectedPrinter =
            _printerGrid.CurrentRow?.DataBoundItem
                as PrinterInfo;

        if (SelectedPrinter is null)
        {
            _selectionLabel.Text =
                "Chưa chọn máy in.";

            SelectedPrinterChanged?.Invoke(null);
            return;
        }

        _selectionLabel.Text =
            $"Đã chọn: {SelectedPrinter.Name}" +
            $"  •  {SelectedPrinter.RoleDisplay}";

        SelectedPrinterChanged?.Invoke(
            SelectedPrinter);
    }

    private Control CreateTitlePanel()
    {
        Panel panel = new()
        {
            Dock = DockStyle.Top,
            Height = 78
        };

        Label title = new()
        {
            Text = "Bước 1 — Chọn máy in",
            Dock = DockStyle.Top,
            Height = 42,
            Font =
                new Font("Segoe UI Semibold", 16F),
            ForeColor = Navy
        };

        Label description = new()
        {
            Text =
                "Chọn máy in cần chẩn đoán. " +
                "Vai trò Host/Client được phát hiện tự động.",
            Dock = DockStyle.Fill,
            ForeColor = TextSecondary
        };

        panel.Controls.Add(description);
        panel.Controls.Add(title);

        return panel;
    }

    private Control CreateFooterPanel()
    {
        FlowLayoutPanel footer = new()
        {
            Dock = DockStyle.Bottom,
            Height = 58,
            Padding = new Padding(0, 10, 0, 0),
            FlowDirection = FlowDirection.LeftToRight
        };

        _refreshButton.Text = "Quét lại";
        _refreshButton.AutoSize = true;
        _refreshButton.MinimumSize = new Size(120, 38);

        _refreshButton.FlatStyle = FlatStyle.Flat;

        _refreshButton.FlatAppearance.BorderColor =
            Color.FromArgb(203, 213, 225);

        _refreshButton.BackColor = Color.White;
        _refreshButton.ForeColor = Navy;
        _refreshButton.Cursor = Cursors.Hand;

        _selectionLabel.AutoSize = true;
        _selectionLabel.Padding =
            new Padding(14, 9, 0, 0);

        _selectionLabel.ForeColor =
            TextSecondary;

        _selectionLabel.Text =
            "Chưa chọn máy in.";

        footer.Controls.Add(_refreshButton);
        footer.Controls.Add(_selectionLabel);

        return footer;
    }

    private void SetBusy(bool isBusy)
    {
        UseWaitCursor = isBusy;

        _refreshButton.Enabled = !isBusy;
        _printerGrid.Enabled = !isBusy;

        _refreshButton.Text =
            isBusy ? "Đang quét..." : "Quét lại";
    }
}