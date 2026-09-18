using FixMayInLan.Core;
using FixMayInLan.Models;

namespace FixMayInLan.UI;

public sealed class DiagnosticPanel : Panel
{
    private static readonly Color Navy =
        Color.FromArgb(20, 31, 49);

    private static readonly Color Blue =
        Color.FromArgb(35, 99, 235);

    private static readonly Color TextSecondary =
        Color.FromArgb(100, 116, 139);

    private readonly DiagnosticsService
        _diagnosticsService;

    private readonly ListView _findingList = new();
    private readonly Button _scanButton = new();
    private readonly Label _summaryLabel = new();

    private PrinterInfo? _currentPrinter;

    public DiagnosticPanel(
        DiagnosticsService diagnosticsService)
    {
        _diagnosticsService =
            diagnosticsService;

        Dock = DockStyle.Fill;
        BackColor = Color.White;
        Padding = new Padding(24);

        ConfigureFindingList();

        Control titlePanel =
            CreateTitlePanel();

        Control footerPanel =
            CreateFooterPanel();

        Controls.Add(_findingList);
        Controls.Add(footerPanel);
        Controls.Add(titlePanel);

        _scanButton.Click += async (_, _) =>
        {
            if (_currentPrinter is not null)
            {
                await ScanAsync(
                    _currentPrinter);
            }
        };

        _findingList.ItemCheck +=
            FindingListOnItemCheck;
    }

    public event Action<string>?
        StatusChanged;

    public IReadOnlyList<DiagnosticFinding>
        SelectedFindings
    {
        get
        {
            return _findingList.CheckedItems
                .Cast<ListViewItem>()
                .Select(item =>
                    item.Tag as DiagnosticFinding)
                .Where(finding =>
                    finding is
                    {
                        CanFixLocally: true
                    })
                .Cast<DiagnosticFinding>()
                .ToList();
        }
    }

    public async Task ScanAsync(
        PrinterInfo printer)
    {
        _currentPrinter = printer;

        SetBusy(true);

        try
        {
            StatusChanged?.Invoke(
                $"Đang chẩn đoán '{printer.Name}'...");

            IReadOnlyList<DiagnosticFinding> findings =
                await _diagnosticsService.ScanAsync(
                    printer);

            DisplayFindings(findings);

            StatusChanged?.Invoke(
                "Đã hoàn tất chẩn đoán hai nhóm lỗi.");

            UpdateSummary();
        }
        catch (Exception exception)
        {
            _findingList.Items.Clear();

            StatusChanged?.Invoke(
                $"Chẩn đoán thất bại: {exception.Message}");

            MessageBox.Show(
                $"Không thể chẩn đoán máy in.\r\n\r\n" +
                exception.Message,
                "Lỗi chẩn đoán",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ConfigureFindingList()
    {
        _findingList.Dock = DockStyle.Fill;
        _findingList.View = View.Details;

        _findingList.CheckBoxes = true;
        _findingList.FullRowSelect = true;
        _findingList.GridLines = true;

        _findingList.HideSelection = false;
        _findingList.ShowItemToolTips = true;

        _findingList.Columns.Add(
    "Chức năng",
    165);

_findingList.Columns.Add(
    "Mã kỹ thuật",
    120);

_findingList.Columns.Add(
    "Mức độ",
    95);

_findingList.Columns.Add(
    "Xử lý",
    150);

_findingList.Columns.Add(
    "Kết quả chẩn đoán",
    480);
    }

    private void DisplayFindings(
        IReadOnlyList<DiagnosticFinding> findings)
    {
        _findingList.BeginUpdate();

        try
        {
            _findingList.Items.Clear();

            foreach (DiagnosticFinding finding in findings)
            {
               string[] values =
{
    finding.Title,
    finding.Code,
    finding.SeverityDisplay,
    finding.LocalActionDisplay,
    finding.Description
};

                ListViewItem item =
                    new(values)
                    {
                        Tag = finding,

                        Checked =
                            finding.CanFixLocally &&
                            finding.IsRecommended,

                        ToolTipText =
                            finding.SecurityWarning ??
                            finding.Preview
                    };

                item.ForeColor =
                    finding.Severity switch
                    {
                        FindingSeverity.Warning =>
                            Color.FromArgb(
                                173,
                                103,
                                0),

                        FindingSeverity.Error =>
                            Color.Firebrick,

                        _ =>
                            Color.FromArgb(
                                55,
                                65,
                                81)
                    };

                if (!finding.CanFixLocally)
                {
                    item.BackColor =
                        Color.FromArgb(
                            246,
                            248,
                            252);
                }

                _findingList.Items.Add(item);
            }
        }
        finally
        {
            _findingList.EndUpdate();
        }
    }

    private void FindingListOnItemCheck(
        object? sender,
        ItemCheckEventArgs eventArgs)
    {
        if (eventArgs.Index < 0 ||
            eventArgs.Index >=
            _findingList.Items.Count)
        {
            return;
        }

        ListViewItem item =
            _findingList.Items[eventArgs.Index];

        if (item.Tag is not DiagnosticFinding finding)
        {
            return;
        }

        // Không cho tick một mục không thể
        // xử lý trên máy hiện tại.
        if (eventArgs.NewValue ==
                CheckState.Checked &&
            !finding.CanFixLocally)
        {
            eventArgs.NewValue =
                CheckState.Unchecked;

            MessageBox.Show(
                finding.Description,
                "Không thể xử lý tại máy này",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        // ItemCheck xảy ra trước khi trạng thái
        // checked thực tế được cập nhật.
        BeginInvoke(
            new Action(UpdateSummary));
    }

    private void UpdateSummary()
    {
        int selectedCount =
            SelectedFindings.Count;

        _summaryLabel.Text =
            selectedCount == 0
                ? "Chưa chọn lỗi cần sửa."
                : $"Đã chọn {selectedCount} lỗi cần sửa.";
    }

    private Control CreateTitlePanel()
    {
        Panel panel = new()
        {
            Dock = DockStyle.Top,
            Height = 82
        };

        Label title = new()
        {
            Text =
                "Bước 2 — Chẩn đoán đúng phạm vi",

            Dock = DockStyle.Top,
            Height = 42,

            Font =
                new Font(
                    "Segoe UI Semibold",
                    16F),

            ForeColor = Navy
        };

        Label description = new()
        {
            Text =
                "Chỉ kiểm tra 0x0000011B và 0x00000709. " +
                "Các mục không áp dụng sẽ bị khóa.",

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
            Height = 60,
            Padding = new Padding(0, 10, 0, 0),
            FlowDirection =
                FlowDirection.LeftToRight
        };

        _scanButton.Text =
            "Quét lại chẩn đoán";

        _scanButton.AutoSize = true;

        _scanButton.MinimumSize =
            new Size(165, 38);

        _scanButton.FlatStyle =
            FlatStyle.Flat;

        _scanButton.FlatAppearance.BorderSize = 0;

        _scanButton.BackColor = Blue;
        _scanButton.ForeColor = Color.White;
        _scanButton.Cursor = Cursors.Hand;

        _summaryLabel.AutoSize = true;

        _summaryLabel.Padding =
            new Padding(14, 9, 0, 0);

        _summaryLabel.ForeColor =
            TextSecondary;

        _summaryLabel.Text =
            "Chưa thực hiện chẩn đoán.";

        footer.Controls.Add(_scanButton);
        footer.Controls.Add(_summaryLabel);

        return footer;
    }

    private void SetBusy(bool isBusy)
    {
        UseWaitCursor = isBusy;

        _scanButton.Enabled = !isBusy;
        _findingList.Enabled = !isBusy;

        _scanButton.Text = isBusy
            ? "Đang chẩn đoán..."
            : "Quét lại chẩn đoán";
    }
}