using FixMayInLan.Core;
using FixMayInLan.Models;

namespace FixMayInLan.UI;

public sealed class ExecutionPanel : Panel
{
    private static readonly Color Navy =
        Color.FromArgb(20, 31, 49);

    private static readonly Color Blue =
        Color.FromArgb(35, 99, 235);

    private static readonly Color TextSecondary =
        Color.FromArgb(100, 116, 139);

    private readonly FixExecutorService
        _fixExecutorService;

    private readonly ListView _planList = new();

    private readonly Button _previewButton = new();
    private readonly Button _executeButton = new();
    private readonly Button _undoButton = new();

    private readonly ProgressBar _progressBar = new();
    private readonly Label _progressLabel = new();

    private readonly System.Windows.Forms.Timer
        _undoTimer = new()
        {
            Interval = 5 * 60 * 1000
        };

    private PrinterInfo? _selectedPrinter;

    private IReadOnlyList<DiagnosticFinding>
        _selectedFindings =
            Array.Empty<DiagnosticFinding>();

    public ExecutionPanel(
        FixExecutorService fixExecutorService)
    {
        _fixExecutorService =
            fixExecutorService;

        Dock = DockStyle.Fill;
        BackColor = Color.White;
        Padding = new Padding(24);

        ConfigurePlanList();

        Control titlePanel =
            CreateTitlePanel();

        Control warningPanel =
            CreateWarningPanel();

        Control progressPanel =
            CreateProgressPanel();

        Control actionPanel =
            CreateActionPanel();

        Controls.Add(_planList);
        Controls.Add(actionPanel);
        Controls.Add(progressPanel);
        Controls.Add(warningPanel);
        Controls.Add(titlePanel);

        _previewButton.Click += (_, _) =>
        {
            ShowPreview();
        };

        _executeButton.Click += async (_, _) =>
        {
            await ExecuteSelectedFixesAsync();
        };

        _undoButton.Click += async (_, _) =>
        {
            await UndoAsync();
        };

        _undoTimer.Tick += (_, _) =>
        {
            _undoTimer.Stop();

            _undoButton.Visible = false;
            _executeButton.Enabled =
                _selectedFindings.Count > 0;

            SetProgress(
                0,
                "Thời hạn hoàn tác nhanh đã kết thúc. " +
                "File backup vẫn còn trong ProgramData.");

            StatusChanged?.Invoke(
                "Thời hạn hoàn tác nhanh đã kết thúc.");
        };
    }

    public event Action<string>? StatusChanged;

    public event Action? ExecutionCompleted;

    /// <summary>
    /// Nhận máy in và danh sách lỗi từ Bước 2.
    /// </summary>
    public void Configure(
        PrinterInfo printer,
        IReadOnlyList<DiagnosticFinding> findings)
    {
        _selectedPrinter = printer;

        _selectedFindings =
            findings
                .Where(finding =>
                    finding.CanFixLocally)
                .ToList();

        _planList.BeginUpdate();

        try
        {
            _planList.Items.Clear();

            int order = 1;

            foreach (DiagnosticFinding finding
                     in _selectedFindings)
            {
                string[] values =
                {
                    order.ToString(),
                    finding.Code,
                    finding.Title,
                    finding.Issue ==
                        PrinterIssue.Error0000011B
                        ? "Registry + restart Spooler"
                        : "Registry người dùng + SetDefaultPrinter"
                };

                ListViewItem item =
                    new(values)
                    {
                        Tag = finding,
                        ToolTipText =
                            finding.SecurityWarning ??
                            finding.Preview
                    };

                if (finding.Issue ==
                    PrinterIssue.Error0000011B)
                {
                    item.ForeColor =
                        Color.FromArgb(173, 103, 0);
                }

                _planList.Items.Add(item);

                order++;
            }
        }
        finally
        {
            _planList.EndUpdate();
        }

        // Không cho chạy phiên mới nếu phiên trước
        // vẫn đang chờ hoàn tác.
        if (!_undoButton.Visible)
        {
            _executeButton.Enabled =
                _selectedFindings.Count > 0;
        }

        _previewButton.Enabled =
            _selectedFindings.Count > 0;

        SetProgress(
            0,
            _selectedFindings.Count == 0
                ? "Chưa chọn lỗi cần sửa."
                : $"Đã chuẩn bị " +
                  $"{_selectedFindings.Count} mục.");
    }

    private void ConfigurePlanList()
    {
        _planList.Dock = DockStyle.Fill;
        _planList.View = View.Details;
        _planList.FullRowSelect = true;
        _planList.GridLines = true;
        _planList.HideSelection = false;
        _planList.ShowItemToolTips = true;

        _planList.Columns.Add(
            "Thứ tự",
            70);

        _planList.Columns.Add(
            "Mã lỗi",
            125);

        _planList.Columns.Add(
            "Nội dung",
            290);

        _planList.Columns.Add(
            "Thao tác",
            390);
    }

    private void ShowPreview()
    {
        if (_selectedPrinter is null ||
            _selectedFindings.Count == 0)
        {
            MessageBox.Show(
                "Chưa có kế hoạch sửa lỗi.",
                "Không có dữ liệu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        List<string> sections = new()
        {
            "MÁY IN ĐƯỢC CHỌN",
            $"Tên: {_selectedPrinter.Name}",
            $"Vai trò: " +
            $"{_selectedPrinter.RoleDisplay}",
            $"Cổng: {_selectedPrinter.PortName}",
            string.Empty,
            "CÁC THAY ĐỔI DỰ KIẾN"
        };

        int order = 1;

        foreach (DiagnosticFinding finding
                 in _selectedFindings)
        {
            sections.Add(string.Empty);

            sections.Add(
                $"{order}. {finding.Code} — " +
                $"{finding.Title}");

            sections.Add(finding.Preview);

            if (!string.IsNullOrWhiteSpace(
                    finding.SecurityWarning))
            {
                sections.Add(string.Empty);

                sections.Add(
                    "CẢNH BÁO BẢO MẬT:");

                sections.Add(
                    finding.SecurityWarning);
            }

            order++;
        }

        sections.Add(string.Empty);
        sections.Add("BACKUP");
        sections.Add(
            @"Registry snapshot sẽ được lưu tại:
C:\ProgramData\FixMayInLan\backups\");

        string preview =
            string.Join(
                Environment.NewLine,
                sections);

        using PreviewDialog dialog =
            new(preview);

        dialog.ShowDialog(
            FindForm());
    }

    private async Task ExecuteSelectedFixesAsync()
    {
        if (_selectedPrinter is null ||
            _selectedFindings.Count == 0)
        {
            return;
        }

        DiagnosticFinding? error11BFinding =
            _selectedFindings.FirstOrDefault(
                finding =>
                    finding.Issue ==
                    PrinterIssue.Error0000011B);

        if (error11BFinding is not null)
        {
            DialogResult riskConfirmation =
                MessageBox.Show(
                    error11BFinding.SecurityWarning +
                    "\r\n\r\n" +
                    "Bạn xác nhận đây là mạng LAN tin cậy " +
                    "và vẫn muốn tiếp tục?",
                    "Xác nhận rủi ro 0x0000011B",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

            if (riskConfirmation !=
                DialogResult.Yes)
            {
                StatusChanged?.Invoke(
                    "Người dùng đã hủy fix 0x0000011B.");

                return;
            }
        }

        DialogResult finalConfirmation =
            MessageBox.Show(
                $"Ứng dụng sẽ thực hiện " +
                $"{_selectedFindings.Count} thay đổi " +
                $"cho máy in:\r\n\r\n" +
                $"{_selectedPrinter.Name}\r\n\r\n" +
                "Bạn muốn tiếp tục?",
                "Xác nhận thực hiện",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

        if (finalConfirmation !=
            DialogResult.Yes)
        {
            return;
        }

        SetBusy(true);

        Progress<FixProgress> progress =
            new(progressInfo =>
            {
                SetProgress(
                    progressInfo.Percent,
                    progressInfo.Status);

                StatusChanged?.Invoke(
                    progressInfo.Status);
            });

        try
        {
            IReadOnlyList<FixResult> results =
                await _fixExecutorService.ExecuteAsync(
                    _selectedPrinter,
                    _selectedFindings,
                    progress);

            SetProgress(
                100,
                $"Hoàn tất {results.Count}/" +
                $"{_selectedFindings.Count} mục.");

            StatusChanged?.Invoke(
                $"Đã sửa thành công " +
                $"{results.Count} mục.");

            _undoButton.Visible = true;

            // Khóa nút chạy lại để bảo vệ
            // phiên backup hiện tại.
            _executeButton.Enabled = false;

            _undoTimer.Stop();
            _undoTimer.Start();

            ExecutionCompleted?.Invoke();

            string backupFolder =
                _fixExecutorService.LastBackupFolder
                ?? "Không xác định";

            MessageBox.Show(
                "Đã hoàn tất sửa lỗi.\r\n\r\n" +
                "Nút Hoàn tác có hiệu lực trong 5 phút." +
                "\r\n\r\n" +
                $"Backup:\r\n{backupFolder}",
                "Hoàn tất",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (AggregateException exception)
        {
            string details =
                string.Join(
                    Environment.NewLine,
                    exception.InnerExceptions
                        .Select(inner =>
                            "• " + inner.Message));

            SetProgress(
                0,
                "Sửa lỗi và rollback đều gặp lỗi.");

            StatusChanged?.Invoke(
                "Sửa lỗi và rollback đều gặp lỗi.");

            MessageBox.Show(
                "Đã xảy ra lỗi nghiêm trọng:\r\n\r\n" +
                details,
                "Không thể hoàn tất",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception exception)
        {
            SetProgress(
                0,
                "Sửa lỗi thất bại. Các thay đổi " +
                "đã được tự động hoàn tác.");

            StatusChanged?.Invoke(
                $"Sửa lỗi thất bại: " +
                $"{exception.Message}");

            MessageBox.Show(
                "Không thể hoàn tất sửa lỗi.\r\n\r\n" +
                exception.Message +
                "\r\n\r\n" +
                "Các thay đổi Registry đã được " +
                "tự động hoàn tác.",
                "Sửa lỗi thất bại",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task UndoAsync()
    {
        DialogResult confirmation =
            MessageBox.Show(
                "Bạn muốn hoàn tác phiên thay đổi gần nhất?",
                "Xác nhận hoàn tác",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        SetBusy(true);

        try
        {
            SetProgress(
                20,
                "Đang khôi phục Registry...");

            StatusChanged?.Invoke(
                "Đang hoàn tác thay đổi...");

            await _fixExecutorService.UndoAsync();

            _undoTimer.Stop();
            _undoButton.Visible = false;

            _executeButton.Enabled =
                _selectedFindings.Count > 0;

            SetProgress(
                100,
                "Đã hoàn tác thay đổi.");

            StatusChanged?.Invoke(
                "Đã hoàn tác phiên thay đổi.");

            ExecutionCompleted?.Invoke();

            MessageBox.Show(
                "Đã hoàn tác các thay đổi của " +
                "phiên gần nhất.",
                "Hoàn tác thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            SetProgress(
                0,
                "Không thể hoàn tác.");

            StatusChanged?.Invoke(
                $"Hoàn tác thất bại: " +
                $"{exception.Message}");

            MessageBox.Show(
                "Không thể hoàn tác.\r\n\r\n" +
                exception.Message +
                "\r\n\r\n" +
                "Không xóa thư mục backup trong " +
                "C:\\ProgramData\\FixMayInLan\\backups.",
                "Hoàn tác thất bại",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private Control CreateTitlePanel()
    {
        Panel panel = new()
        {
            Dock = DockStyle.Top,
            Height = 80
        };

        Label title = new()
        {
            Text =
                "Bước 3 — Xem trước và thực hiện",

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
                "Kiểm tra kế hoạch, backup Registry " +
                "và thực hiện từng thay đổi.",

            Dock = DockStyle.Fill,
            ForeColor = TextSecondary
        };

        panel.Controls.Add(description);
        panel.Controls.Add(title);

        return panel;
    }

    private static Control CreateWarningPanel()
    {
        Label warning = new()
        {
            Dock = DockStyle.Top,
            Height = 82,
            Padding = new Padding(14),
            BackColor =
                Color.FromArgb(255, 247, 226),
            ForeColor =
                Color.FromArgb(124, 76, 0),

            Text =
                "LƯU Ý AN TOÀN\r\n" +
                "Fix 0x0000011B chỉ chạy trên Printer Host. " +
                "RpcAuthnLevelPrivacyEnabled=0 làm giảm " +
                "bảo vệ RPC và cần được hoàn tác khi không " +
                "còn cần thiết."
        };

        return warning;
    }

    private Control CreateProgressPanel()
    {
        TableLayoutPanel panel = new()
        {
            Dock = DockStyle.Bottom,
            Height = 62,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0, 8, 0, 4)
        };

        panel.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                26));

        panel.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                20));

        _progressLabel.Text = "Sẵn sàng";
        _progressLabel.Dock = DockStyle.Fill;
        _progressLabel.ForeColor =
            TextSecondary;

        _progressBar.Dock = DockStyle.Fill;
        _progressBar.Minimum = 0;
        _progressBar.Maximum = 100;
        _progressBar.Style =
            ProgressBarStyle.Continuous;

        panel.Controls.Add(
            _progressLabel,
            0,
            0);

        panel.Controls.Add(
            _progressBar,
            0,
            1);

        return panel;
    }

    private Control CreateActionPanel()
    {
        FlowLayoutPanel panel = new()
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            Padding = new Padding(0, 10, 0, 0),
            FlowDirection =
                FlowDirection.LeftToRight
        };

        ConfigureButton(
            _previewButton,
            "Xem trước thay đổi",
            Color.White,
            Navy,
            hasBorder: true);

        ConfigureButton(
            _executeButton,
            "Bắt đầu sửa",
            Blue,
            Color.White,
            hasBorder: false);

        ConfigureButton(
            _undoButton,
            "Hoàn tác",
            Color.FromArgb(190, 40, 40),
            Color.White,
            hasBorder: false);

        _previewButton.Enabled = false;
        _executeButton.Enabled = false;
        _undoButton.Visible = false;

        panel.Controls.Add(_previewButton);
        panel.Controls.Add(_executeButton);
        panel.Controls.Add(_undoButton);

        return panel;
    }

    private static void ConfigureButton(
        Button button,
        string text,
        Color background,
        Color foreground,
        bool hasBorder)
    {
        button.Text = text;
        button.AutoSize = true;

        button.MinimumSize =
            new Size(150, 38);

        button.FlatStyle = FlatStyle.Flat;

        button.FlatAppearance.BorderSize =
            hasBorder ? 1 : 0;

        button.FlatAppearance.BorderColor =
            Color.FromArgb(203, 213, 225);

        button.BackColor = background;
        button.ForeColor = foreground;
        button.Cursor = Cursors.Hand;
        button.Margin =
            new Padding(0, 0, 10, 0);
    }

    private void SetProgress(
        int percent,
        string status)
    {
        _progressBar.Value =
            Math.Clamp(percent, 0, 100);

        _progressLabel.Text = status;
    }

    private void SetBusy(bool isBusy)
    {
        UseWaitCursor = isBusy;

        _previewButton.Enabled =
            !isBusy &&
            _selectedFindings.Count > 0;

        _executeButton.Enabled =
            !isBusy &&
            _selectedFindings.Count > 0 &&
            !_undoButton.Visible;

        _undoButton.Enabled = !isBusy;
        _planList.Enabled = !isBusy;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _undoTimer.Dispose();
        }

        base.Dispose(disposing);
    }
}