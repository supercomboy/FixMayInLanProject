using FixMayInLan.Core;
using FixMayInLan.Models;

namespace FixMayInLan.UI;

public sealed class MainForm : Form
{
    // =========================================================
    // MÀU SẮC
    // =========================================================

    private static readonly Color Navy =
        Color.FromArgb(20, 31, 49);

    private static readonly Color Blue =
        Color.FromArgb(35, 99, 235);

    private static readonly Color Canvas =
        Color.FromArgb(242, 245, 250);

    private static readonly Color TextSecondary =
        Color.FromArgb(100, 116, 139);

    // =========================================================
    // SERVICES
    // =========================================================

    private readonly IAppLogger _logger;

    private readonly SystemInfoService
        _systemInfoService = new();

    private readonly AppSettingsService
        _settingsService = new();

    // =========================================================
    // HEADER
    // =========================================================

    private readonly Panel _headerPanel = new();
    private readonly PictureBox _logoPictureBox = new();

    private readonly Label
        _computerInfoLabel = new();

    private readonly Label
        _environmentBadgeLabel = new();

    private readonly Button
        _systemToolsButton = new();

 private readonly Button _clientToolsButton = new();
private readonly Button _themeButton = new();

    // =========================================================
    // WIZARD
    // =========================================================

    private readonly Label
        _stepIndicator = new();

    private readonly Label
        _statusLabel = new();

    private readonly RichTextBox
        _logBox = new();

    private readonly Button
        _backButton = new();

    private readonly Button
        _nextButton = new();

    private readonly PrinterSelectionPanel
        _printerSelectionPanel;

    private readonly DiagnosticPanel
        _diagnosticPanel;

    private readonly ExecutionPanel
        _executionPanel;

    private readonly Panel[] _stepPanels;

    private AppTheme _currentTheme;

    private int _currentStep;

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public MainForm(IAppLogger logger)
    {
        _logger = logger;

        AppSettings settings =
            _settingsService.Load();

        _currentTheme = settings.Theme;

        _logger.EntryWritten +=
            LoggerOnEntryWritten;

        Text = "Tool Fix In LAN";

        StartPosition =
            FormStartPosition.CenterScreen;

        MinimumSize =
            new Size(1050, 720);

        Size =
            new Size(1180, 800);

        BackColor = Canvas;

        Font =
            new Font("Segoe UI", 10F);

        // -----------------------------------------------------
        // HEADER
        // -----------------------------------------------------

        Control header =
            CreateHeader();

        Control stepBar =
            CreateStepIndicator();

        // -----------------------------------------------------
        // PRINTER SERVICE
        // -----------------------------------------------------

        PrinterService printerService =
            new();

        _printerSelectionPanel =
            new PrinterSelectionPanel(
                printerService);

        _printerSelectionPanel.StatusChanged +=
            message =>
            {
                _statusLabel.Text = message;
                _logger.Info(message);
            };

        // -----------------------------------------------------
        // DIAGNOSTIC SERVICE
        // -----------------------------------------------------

        DiagnosticsService diagnosticsService =
            new();

        _diagnosticPanel =
            new DiagnosticPanel(
                diagnosticsService);

        _diagnosticPanel.StatusChanged +=
            message =>
            {
                _statusLabel.Text = message;
                _logger.Info(message);
            };

        // -----------------------------------------------------
        // FIX SERVICES
        // -----------------------------------------------------

        RegistryBackupService backupService =
            new();

        SpoolerService spoolerService =
            new(_logger);

        FixExecutorService fixExecutorService =
            new(
                backupService,
                spoolerService,
                printerService,
                _logger);

        _executionPanel =
            new ExecutionPanel(
                fixExecutorService);

        _executionPanel.StatusChanged +=
            message =>
            {
                _statusLabel.Text = message;
                _logger.Info(message);
            };

        _executionPanel.ExecutionCompleted +=
            async () =>
            {
                await _printerSelectionPanel
                    .LoadPrintersAsync();
            };

        // -----------------------------------------------------
        // BA BƯỚC WIZARD
        // -----------------------------------------------------

        Panel step1 =
            _printerSelectionPanel;

        Panel step2 =
            _diagnosticPanel;

        Panel step3 =
            _executionPanel;

        _stepPanels =
        [
            step1,
            step2,
            step3
        ];

        Panel contentPanel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Canvas,
            Padding =
                new Padding(22, 14, 22, 14)
        };

        contentPanel.Controls.Add(step3);
        contentPanel.Controls.Add(step2);
        contentPanel.Controls.Add(step1);

        // -----------------------------------------------------
        // ROOT LAYOUT
        // -----------------------------------------------------

        TableLayoutPanel rootLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = Canvas
        };

        rootLayout.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                100F));

        rootLayout.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                84F));

        rootLayout.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                68F));

        rootLayout.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100F));

        rootLayout.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                58F));

        rootLayout.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                52F));

        rootLayout.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                140F));

        rootLayout.Controls.Add(
            header,
            0,
            0);

        rootLayout.Controls.Add(
            stepBar,
            0,
            1);

        rootLayout.Controls.Add(
            contentPanel,
            0,
            2);

        rootLayout.Controls.Add(
            CreateNavigationPanel(),
            0,
            3);

        rootLayout.Controls.Add(
            CreateStatusPanel(),
            0,
            4);

        rootLayout.Controls.Add(
            CreateLogPanel(),
            0,
            5);

        Controls.Add(rootLayout);

        // -----------------------------------------------------
        // KHỞI ĐỘNG GIAO DIỆN
        // -----------------------------------------------------

        ApplyCurrentTheme();

        Shown += async (_, _) =>
        {
            await LoadEnvironmentInfoAsync();

            await _printerSelectionPanel
                .LoadPrintersAsync();
        };

        FormClosed += (_, _) =>
        {
            _logger.EntryWritten -=
                LoggerOnEntryWritten;
        };

        ShowStep(0);

        _logger.Success(
            "Giao diện đã khởi động thành công.");
    }

    // =========================================================
    // HEADER
    // =========================================================

    private Control CreateHeader()
    {
        _headerPanel.Dock =
            DockStyle.Fill;

        _headerPanel.BackColor = Navy;

        _headerPanel.Padding =
            new Padding(24, 0, 24, 0);
            // -----------------------------------------------------
// LOGO ỨNG DỤNG
// -----------------------------------------------------

_logoPictureBox.Size = new Size(44, 44);
_logoPictureBox.Location = new Point(24, 20);
_logoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
_logoPictureBox.BackColor = Color.Transparent;
_logoPictureBox.TabStop = false;

string logoPath = Path.Combine(
    AppContext.BaseDirectory,
    "Assets",
    "logo.png");

try
{
    if (File.Exists(logoPath))
    {
        // Tạo bản sao để không khóa file logo.png.
        using Image sourceLogo =
            Image.FromFile(logoPath);

        _logoPictureBox.Image =
            new Bitmap(sourceLogo);
    }
    else
    {
        _logger.Warning(
            $"Không tìm thấy logo: {logoPath}");
    }
}
catch (Exception exception)
{
    _logger.Warning(
        $"Không thể tải logo ứng dụng: " +
        $"{exception.Message}");
}

   Label appName = new()
{
    Text = "FIX IN LAN BY",
    AutoSize = true,
    ForeColor = Color.White,
    Font = new Font(
        "Segoe UI Semibold",
        20F),
    };
const int headerHeight = 84;
const int leftMargin = 24;
const int brandSpacing = 10;

// Lấy kích thước thực tế của tên ứng dụng.
Size appNameSize =
    appName.GetPreferredSize(Size.Empty);

// Căn giữa tên ứng dụng theo chiều dọc.
appName.Location = new Point(
    leftMargin,
    (headerHeight - appNameSize.Height) / 2);

// Đặt logo bên phải tên ứng dụng.
_logoPictureBox.Location = new Point(
    leftMargin + appNameSize.Width + brandSpacing,
    (headerHeight - _logoPictureBox.Height) / 2 + 3);

        // -----------------------------------------------------
        // THÔNG TIN MÁY
        // -----------------------------------------------------

        _computerInfoLabel.Text =
            "Đang đọc thông tin máy tính...";

        _computerInfoLabel.ForeColor =
            Color.FromArgb(200, 214, 235);

        _computerInfoLabel.Font =
            new Font("Segoe UI", 9.5F);

        _computerInfoLabel.TextAlign =
            ContentAlignment.MiddleRight;

        _computerInfoLabel.AutoEllipsis = true;

        // -----------------------------------------------------
        // BADGE WORKGROUP / DOMAIN / SERVER
        // -----------------------------------------------------

        _environmentBadgeLabel.Text =
            string.Empty;

        _environmentBadgeLabel.AutoSize = true;

        _environmentBadgeLabel.Padding =
            new Padding(8, 3, 8, 3);

        _environmentBadgeLabel.Font =
            new Font(
                "Segoe UI Semibold",
                8.5F);

        _environmentBadgeLabel.ForeColor =
            Color.White;

        _environmentBadgeLabel.Visible = false;

        // -----------------------------------------------------
        // NÚT CÔNG CỤ HỆ THỐNG
        // -----------------------------------------------------

        _clientToolsButton.Text = "Máy Trạm";
_clientToolsButton.Size = new Size(110, 36);
_clientToolsButton.FlatStyle = FlatStyle.Flat;
_clientToolsButton.FlatAppearance.BorderSize = 1;
_clientToolsButton.FlatAppearance.BorderColor =
    Color.FromArgb(14, 165, 233);
_clientToolsButton.BackColor =
    Color.FromArgb(15, 23, 42);
_clientToolsButton.ForeColor =
    Color.FromArgb(125, 211, 252);
_clientToolsButton.Font =
    new Font("Segoe UI Semibold", 9F);
_clientToolsButton.Cursor = Cursors.Hand;
_clientToolsButton.Anchor =
    AnchorStyles.Top | AnchorStyles.Right;

_clientToolsButton.Click += async (_, _) =>
{
    using ClientPrinterDialog dialog = new();

    dialog.ShowDialog(this);

    // Làm mới danh sách máy in trên màn hình chính
    // sau khi đóng cửa sổ Máy Trạm.
    await _printerSelectionPanel.LoadPrintersAsync();
};
       _systemToolsButton.Text = "Máy Chủ";
_systemToolsButton.Size = new Size(110, 36);
_systemToolsButton.FlatStyle = FlatStyle.Flat;
_systemToolsButton.FlatAppearance.BorderSize = 1;
_systemToolsButton.FlatAppearance.BorderColor =
    Color.FromArgb(14, 165, 233);
_systemToolsButton.BackColor =
    Color.FromArgb(15, 23, 42);
_systemToolsButton.ForeColor =
    Color.FromArgb(125, 211, 252);
_systemToolsButton.Font =
    new Font("Segoe UI Semibold", 9F);
_systemToolsButton.Cursor = Cursors.Hand;
_systemToolsButton.Anchor =
    AnchorStyles.Top | AnchorStyles.Right;

        _systemToolsButton.TabStop = false;

        _systemToolsButton.Click +=
            async (_, _) =>
            {
                ComputerNameService
                    computerNameService =
                        new(_logger);

                using SystemToolsDialog dialog =
                    new(
                        computerNameService,
                        _logger);

                dialog.ShowDialog(this);

                await LoadEnvironmentInfoAsync();
            };

        // -----------------------------------------------------
        // NÚT LIGHT / DARK
        // -----------------------------------------------------

        _themeButton.AutoSize = false;

        _themeButton.Text =
            _currentTheme == AppTheme.Dark
                ? "☀ Light"
                : "☾ Dark";

        _themeButton.Size =
            new Size(82, 32);

        _themeButton.Font =
            new Font(
                "Segoe UI Semibold",
                9F);

        _themeButton.TextAlign =
            ContentAlignment.MiddleCenter;

        _themeButton.FlatStyle =
            FlatStyle.Flat;

        _themeButton
            .FlatAppearance
            .BorderSize = 1;

        _themeButton
            .FlatAppearance
            .BorderColor =
                Color.FromArgb(79, 96, 122);

        _themeButton
            .UseVisualStyleBackColor = false;

        _themeButton.BackColor =
            Color.FromArgb(45, 60, 82);

        _themeButton.ForeColor =
            Color.White;

        _themeButton.Cursor =
            Cursors.Hand;

        _themeButton.TabStop = false;

        _themeButton.Click += (_, _) =>
        {
            ToggleTheme();
        };

        // -----------------------------------------------------
        // THÊM CONTROLS
        // -----------------------------------------------------
        _headerPanel.Controls.Add(_logoPictureBox);
        _headerPanel.Controls.Add(appName);

        _headerPanel.Controls.Add(
            _computerInfoLabel);

        _headerPanel.Controls.Add(
            _environmentBadgeLabel);

        _headerPanel.Controls.Add(_clientToolsButton);    

        _headerPanel.Controls.Add(
            _systemToolsButton);

        _headerPanel.Controls.Add(
            _themeButton);

        _systemToolsButton.BringToFront();
        _themeButton.BringToFront();
        _clientToolsButton.BringToFront();
        _environmentBadgeLabel.BringToFront();

        _logoPictureBox.BringToFront();
        appName.BringToFront();

        _headerPanel.Resize += (_, _) =>
        {
            PositionHeaderControls();
        };

        PositionHeaderControls();

        return _headerPanel;
    }
private void PositionHeaderControls()
{
    const int rightMargin = 24;
    const int spacing = 10;
    const int buttonTop = 8;

    int headerWidth = _headerPanel.ClientSize.Width;

    if (headerWidth <= 0)
    {
        return;
    }

    // Dark/Light nằm ngoài cùng bên phải.
    _themeButton.Location = new Point(
        headerWidth -
        rightMargin -
        _themeButton.Width,
        buttonTop + 2);

    // WORKGROUP nằm bên trái Dark/Light.
    int badgeTop = buttonTop +
        (_systemToolsButton.Height -
         _environmentBadgeLabel.Height) / 2;

    _environmentBadgeLabel.Location = new Point(
        _themeButton.Left -
        spacing -
        _environmentBadgeLabel.Width,
        badgeTop);

    // Máy Trạm nằm bên trái WORKGROUP.
    _clientToolsButton.Location = new Point(
        _environmentBadgeLabel.Left -
        spacing -
        _clientToolsButton.Width,
        buttonTop);

    // Máy Chủ nằm bên trái Máy Trạm.
    _systemToolsButton.Location = new Point(
        _clientToolsButton.Left -
        spacing -
        _systemToolsButton.Width,
        buttonTop);

    // Dòng thông tin hệ thống nằm ở hàng dưới, căn phải.
    const int computerInfoLeft = 260;
    const int computerInfoTop = 48;

    int computerInfoRight =
        headerWidth - rightMargin;

    int computerInfoWidth = Math.Max(
        100,
        computerInfoRight - computerInfoLeft);

    _computerInfoLabel.Location = new Point(
        computerInfoLeft,
        computerInfoTop);

    _computerInfoLabel.Size = new Size(
        computerInfoWidth,
        24);

    _computerInfoLabel.TextAlign =
        ContentAlignment.MiddleRight;

    _computerInfoLabel.BringToFront();
    _systemToolsButton.BringToFront();
    _clientToolsButton.BringToFront();
    _environmentBadgeLabel.BringToFront();
    _themeButton.BringToFront();
}

    // =========================================================
    // ĐỌC THÔNG TIN WINDOWS
    // =========================================================

    private async Task LoadEnvironmentInfoAsync()
    {
        try
        {
            _logger.Info(
                "Đang đọc thông tin hệ thống.");

            ComputerEnvironmentInfo info =
                await _systemInfoService
                    .GetEnvironmentInfoAsync();

            _computerInfoLabel.Text =
                info.EnvironmentDisplay;

            if (info.IsWindowsServer)
            {
                _environmentBadgeLabel.Text =
                    "⚠ WINDOWS SERVER";

                _environmentBadgeLabel.BackColor =
                    Color.FromArgb(190, 40, 40);

                _environmentBadgeLabel.Visible = true;

                _logger.Warning(
                    "Ứng dụng đang chạy trên Windows Server. " +
                    "Thao tác Print Spooler có thể ảnh hưởng " +
                    "đến nhiều máy Client.");
            }
            else if (info.IsDomainJoined)
            {
                _environmentBadgeLabel.Text =
                    "DOMAIN-JOINED";

                _environmentBadgeLabel.BackColor =
                    Color.FromArgb(173, 103, 0);

                _environmentBadgeLabel.Visible = true;

                _logger.Warning(
                    $"Máy tính thuộc Domain: " +
                    $"{info.DomainName}.");
            }
            else
            {
                _environmentBadgeLabel.Text =
                    "WORKGROUP";

                _environmentBadgeLabel.BackColor =
                    Color.FromArgb(24, 128, 82);

                _environmentBadgeLabel.Visible = true;

                _logger.Info(
                    "Máy tính đang hoạt động trong Workgroup.");
            }

            PositionHeaderControls();

            _logger.Success(
                $"Đã đọc thông tin hệ thống: " +
                $"{info.WindowsName} " +
                $"{info.WindowsVersion}; " +
                $"IPv4={info.IPv4Address}.");
        }
        catch (Exception exception)
        {
            _computerInfoLabel.Text =
                $"{Environment.MachineName}  •  " +
                "Không thể đọc đầy đủ thông tin Windows";

            _environmentBadgeLabel.Text =
                "KHÔNG XÁC ĐỊNH";

            _environmentBadgeLabel.BackColor =
                Color.FromArgb(100, 116, 139);

            _environmentBadgeLabel.Visible = true;

            PositionHeaderControls();

            _logger.Warning(
                "Không thể đọc đầy đủ thông tin hệ thống: " +
                exception.Message);
        }
    }

    // =========================================================
    // THANH HIỂN THỊ CÁC BƯỚC
    // =========================================================

    private Control CreateStepIndicator()
    {
        Panel panel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White
        };

        _stepIndicator.Dock =
            DockStyle.Fill;

        _stepIndicator.TextAlign =
            ContentAlignment.MiddleCenter;

        _stepIndicator.Font =
            new Font(
                "Segoe UI Semibold",
                12F);

        _stepIndicator.ForeColor =
            Blue;

        panel.Controls.Add(
            _stepIndicator);

        return panel;
    }

    // =========================================================
    // ĐIỀU HƯỚNG
    // =========================================================

    private Control CreateNavigationPanel()
    {
        FlowLayoutPanel navigation = new()
        {
            Dock = DockStyle.Fill,

            FlowDirection =
                FlowDirection.RightToLeft,

            BackColor = Canvas,

            Padding =
                new Padding(0, 8, 22, 0)
        };

        _nextButton.Text =
            "Tiếp tục  ›";

        StylePrimaryButton(
            _nextButton);

        _nextButton.Click +=
            async (_, _) =>
            {
                if (_currentStep == 0)
                {
                    if (_printerSelectionPanel
                            .SelectedPrinter
                        is not PrinterInfo
                            selectedPrinter)
                    {
                        MessageBox.Show(
                            "Bạn cần chọn một máy in " +
                            "trước khi tiếp tục.",
                            "Chưa chọn máy in",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        return;
                    }

                    ShowStep(1);

                    await _diagnosticPanel
                        .ScanAsync(
                            selectedPrinter);

                    return;
                }

                if (_currentStep == 1)
                {
                    if (_diagnosticPanel
                            .SelectedFindings.Count == 0)
                    {
                        MessageBox.Show(
                            "Bạn cần chọn ít nhất một mục " +
                            "có thể xử lý.",
                            "Chưa chọn lỗi",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        return;
                    }

                    if (_printerSelectionPanel
                            .SelectedPrinter
                        is not PrinterInfo
                            selectedPrinter)
                    {
                        MessageBox.Show(
                            "Không còn thông tin " +
                            "máy in được chọn.",
                            "Thiếu dữ liệu",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);

                        ShowStep(0);

                        return;
                    }

                    _executionPanel.Configure(
                        selectedPrinter,
                        _diagnosticPanel
                            .SelectedFindings);

                    ShowStep(2);
                }
            };

        _backButton.Text =
            "‹  Quay lại";

        StyleSecondaryButton(
            _backButton);

        _backButton.Click += (_, _) =>
        {
            if (_currentStep > 0)
            {
                ShowStep(
                    _currentStep - 1);
            }
        };

        navigation.Controls.Add(
            _nextButton);

        navigation.Controls.Add(
            _backButton);

        return navigation;
    }

    private void ShowStep(int step)
    {
        _currentStep =
            Math.Clamp(
                step,
                0,
                _stepPanels.Length - 1);

        for (int index = 0;
             index < _stepPanels.Length;
             index++)
        {
            _stepPanels[index].Visible =
                index == _currentStep;
        }

        _stepPanels[_currentStep]
            .BringToFront();

        _backButton.Visible =
            _currentStep > 0;

        _nextButton.Visible =
            _currentStep <
            _stepPanels.Length - 1;

        _stepIndicator.Text =
            _currentStep switch
            {
                0 =>
                    "❶ Chọn máy in     " +
                    "② Chẩn đoán     " +
                    "③ Thực hiện sửa",

                1 =>
                    "① Chọn máy in     " +
                    "❷ Chẩn đoán     " +
                    "③ Thực hiện sửa",

                2 =>
                    "① Chọn máy in     " +
                    "② Chẩn đoán     " +
                    "❸ Thực hiện sửa",

                _ => string.Empty
            };

        _statusLabel.Text =
            $"Đang ở bước " +
            $"{_currentStep + 1}/3";

        _logger.Info(
            $"Chuyển sang bước " +
            $"{_currentStep + 1}.");
    }

    // =========================================================
    // STATUS
    // =========================================================

    private Control CreateStatusPanel()
    {
        Panel panel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding =
                new Padding(22, 8, 22, 8)
        };

        _statusLabel.Text =
            "Sẵn sàng";

        _statusLabel.Dock =
            DockStyle.Fill;

        _statusLabel.TextAlign =
            ContentAlignment.MiddleLeft;

        _statusLabel.ForeColor =
            TextSecondary;

        panel.Controls.Add(
            _statusLabel);

        return panel;
    }

    // =========================================================
    // LOG CONSOLE
    // =========================================================

    private Control CreateLogPanel()
    {
        Panel panel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Navy,
            Padding =
                new Padding(16, 10, 16, 10)
        };

        _logBox.Dock =
            DockStyle.Fill;

        _logBox.ReadOnly = true;

        _logBox.BackColor =
            Navy;

        _logBox.ForeColor =
            Color.LightGray;

        _logBox.BorderStyle =
            BorderStyle.None;

        _logBox.Font =
            new Font(
                "Consolas",
                9.5F);

        panel.Controls.Add(
            _logBox);

        return panel;
    }

    private void LoggerOnEntryWritten(
        object? sender,
        LogEntry entry)
    {
        if (InvokeRequired)
        {
            BeginInvoke(
                new Action(
                    () =>
                        LoggerOnEntryWritten(
                            sender,
                            entry)));

            return;
        }

        string levelText =
            entry.Level switch
            {
                LogLevel.Information =>
                    "INFO",

                LogLevel.Warning =>
                    "WARN",

                LogLevel.Error =>
                    "ERROR",

                LogLevel.Success =>
                    "OK",

                _ => "LOG"
            };

        Color logColor =
            entry.Level switch
            {
                LogLevel.Warning =>
                    Color.Gold,

                LogLevel.Error =>
                    Color.FromArgb(
                        255,
                        112,
                        112),

                LogLevel.Success =>
                    Color.FromArgb(
                        92,
                        219,
                        149),

                _ =>
                    Color.FromArgb(
                        210,
                        221,
                        238)
            };

        WriteLog(
            levelText,
            entry.Message,
            logColor);
    }

    private void WriteLog(
        string level,
        string message,
        Color color)
    {
        string line =
            $"[{DateTime.Now:HH:mm:ss}] " +
            $"{level,-7} " +
            $"{message}";

        _logBox.SelectionStart =
            _logBox.TextLength;

        _logBox.SelectionColor =
            color;

        _logBox.AppendText(
            line +
            Environment.NewLine);

        _logBox.ScrollToCaret();
    }

    // =========================================================
    // LIGHT / DARK MODE
    // =========================================================

    private void ToggleTheme()
    {
        _currentTheme =
            _currentTheme == AppTheme.Light
                ? AppTheme.Dark
                : AppTheme.Light;

        ApplyCurrentTheme();

        _settingsService.Save(
            new AppSettings
            {
                Theme =
                    _currentTheme
            });

        _logger.Info(
            _currentTheme == AppTheme.Dark
                ? "Đã chuyển sang Dark Mode."
                : "Đã chuyển sang Light Mode.");
    }

    private void ApplyCurrentTheme()
    {
        ThemeManager.Apply(
            this,
            _currentTheme);

        _themeButton.Text =
            _currentTheme == AppTheme.Dark
                ? "☀ Light"
                : "☾ Dark";

        PositionHeaderControls();
    }

    // =========================================================
    // BUTTON STYLE
    // =========================================================

    private static void StylePrimaryButton(
        Button button)
    {
        button.AutoSize = true;

        button.MinimumSize =
            new Size(130, 38);

        button.FlatStyle =
            FlatStyle.Flat;

        button.FlatAppearance.BorderSize = 0;

        button.BackColor =
            Blue;

        button.ForeColor =
            Color.White;

        button.Cursor =
            Cursors.Hand;

        button.Margin =
            new Padding(8, 0, 0, 0);
    }

    private static void StyleSecondaryButton(
        Button button)
    {
        button.AutoSize = true;

        button.MinimumSize =
            new Size(130, 38);

        button.FlatStyle =
            FlatStyle.Flat;

        button.FlatAppearance.BorderColor =
            Color.FromArgb(203, 213, 225);

        button.BackColor =
            Color.White;

        button.ForeColor =
            Color.FromArgb(51, 65, 85);

        button.Cursor =
            Cursors.Hand;

        button.Margin =
            new Padding(8, 0, 0, 0);
    }
}