using FixMayInLan.Core;
using FixMayInLan.Models;

namespace FixMayInLan.UI;

public sealed class ClientPrinterDialog : Form
{
    private readonly LanServerScannerService _scannerService;
    private readonly ClientPrinterService _clientPrinterService;

    private readonly TextBox _serverTextBox = new();
    private readonly ComboBox _serverComboBox = new();

    private readonly Button _scanButton = new();
    private readonly Button _loadPrintersButton = new();
    private readonly Button _connectButton = new();
    private readonly Button _closeButton = new();

    private readonly DataGridView _printerGrid = new();
    private readonly CheckBox _setDefaultCheckBox = new();
    private readonly ProgressBar _progressBar = new();
    private readonly Label _statusLabel = new();

    private CancellationTokenSource? _operationCancellation;
    private bool _isBusy;

    public event EventHandler? PrinterConnected;

    public ClientPrinterDialog()
        : this(
            new LanServerScannerService(),
            new ClientPrinterService())
    {
    }

    public ClientPrinterDialog(
        LanServerScannerService scannerService,
        ClientPrinterService clientPrinterService)
    {
        _scannerService = scannerService;
        _clientPrinterService = clientPrinterService;

        InitializeForm();
        InitializeHeader();
        InitializeContent();
        ConnectEvents();
    }

    private void InitializeForm()
    {
        Text = "Máy Trạm - Kết nối máy in LAN";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(920, 650);
        MinimumSize = new Size(760, 540);

        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F);

        BackColor = Color.FromArgb(241, 245, 249);
        ForeColor = Color.FromArgb(15, 23, 42);

        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
    }

    private void InitializeHeader()
    {
        Panel headerPanel = new()
        {
            Dock = DockStyle.Top,
            Height = 82,
            Padding = new Padding(22, 14, 22, 12),
            BackColor = Color.FromArgb(15, 23, 42)
        };

        Label titleLabel = new()
        {
            AutoSize = true,
            Location = new Point(22, 13),
            Font = new Font("Segoe UI Semibold", 16F),
            ForeColor = Color.White,
            Text = "MÁY TRẠM"
        };

        Label descriptionLabel = new()
        {
            AutoSize = true,
            Location = new Point(24, 48),
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(203, 213, 225),
            Text = "Tìm máy chủ, chọn máy in được chia sẻ và kết nối vào máy hiện tại."
        };

        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(descriptionLabel);

        Controls.Add(headerPanel);
    }

    private void InitializeContent()
    {
        Panel contentPanel = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            BackColor = BackColor
        };

        TableLayoutPanel rootLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = BackColor
        };

        rootLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 158F));

        rootLayout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100F));

        rootLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 48F));

        rootLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 50F));

        rootLayout.Controls.Add(CreateServerPanel(), 0, 0);
        rootLayout.Controls.Add(CreatePrinterPanel(), 0, 1);
        rootLayout.Controls.Add(CreateStatusPanel(), 0, 2);
        rootLayout.Controls.Add(CreateActionPanel(), 0, 3);

        contentPanel.Controls.Add(rootLayout);
        Controls.Add(contentPanel);

        contentPanel.BringToFront();
    }

    private Control CreateServerPanel()
    {
        Panel cardPanel = CreateCardPanel();

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 12),
            ColumnCount = 4,
            RowCount = 4
        };

        layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 120F));

        layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100F));

        layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 135F));

        layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 135F));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 30F));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 38F));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 30F));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 38F));

        Label manualServerLabel = CreateFieldLabel(
            "Tên/IP máy chủ:");

        layout.Controls.Add(manualServerLabel, 0, 0);
        layout.SetColumnSpan(manualServerLabel, 4);

        ConfigureTextBox();
        layout.Controls.Add(_serverTextBox, 0, 1);
        layout.SetColumnSpan(_serverTextBox, 2);

        ConfigureButton(
            _loadPrintersButton,
            "Tải máy in",
            Color.FromArgb(37, 99, 235),
            Color.White);

        _loadPrintersButton.Margin =
            new Padding(8, 0, 0, 0);

        layout.Controls.Add(_loadPrintersButton, 2, 1);

        ConfigureButton(
            _scanButton,
            "Quét máy chủ",
            Color.FromArgb(14, 116, 144),
            Color.White);

        _scanButton.Margin =
            new Padding(8, 0, 0, 0);

        layout.Controls.Add(_scanButton, 3, 1);

        Label scannedServerLabel = CreateFieldLabel(
            "Máy chủ tìm thấy:");

        layout.Controls.Add(scannedServerLabel, 0, 2);
        layout.SetColumnSpan(scannedServerLabel, 4);

        ConfigureServerComboBox();
        layout.Controls.Add(_serverComboBox, 0, 3);
        layout.SetColumnSpan(_serverComboBox, 4);

        cardPanel.Controls.Add(layout);

        return cardPanel;
    }

    private Control CreatePrinterPanel()
    {
        Panel cardPanel = CreateCardPanel();
        cardPanel.Margin = new Padding(0, 12, 0, 0);

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 14),
            ColumnCount = 1,
            RowCount = 2
        };

        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 34F));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100F));

        Label titleLabel = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 10F),
            ForeColor = Color.FromArgb(30, 41, 59),
            Text = "MÁY IN ĐƯỢC CHIA SẺ"
        };

        ConfigurePrinterGrid();

        layout.Controls.Add(titleLabel, 0, 0);
        layout.Controls.Add(_printerGrid, 0, 1);

        cardPanel.Controls.Add(layout);

        return cardPanel;
    }

    private Control CreateStatusPanel()
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(2, 5, 2, 0)
        };

        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 21F));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 10F));

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.ForeColor = Color.FromArgb(71, 85, 105);
        _statusLabel.Text = "Sẵn sàng.";

        _progressBar.Dock = DockStyle.Fill;
        _progressBar.Minimum = 0;
        _progressBar.Maximum = 100;
        _progressBar.Value = 0;
        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.Visible = false;

        layout.Controls.Add(_statusLabel, 0, 0);
        layout.Controls.Add(_progressBar, 0, 1);

        return layout;
    }

    private Control CreateActionPanel()
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(0, 7, 0, 0)
        };

        layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100F));

        layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 130F));

        layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 130F));

        _setDefaultCheckBox.AutoSize = true;
        _setDefaultCheckBox.Anchor = AnchorStyles.Left;
        _setDefaultCheckBox.Checked = true;
        _setDefaultCheckBox.Text =
            "Đặt làm máy in mặc định sau khi kết nối";

        ConfigureButton(
            _connectButton,
            "Kết nối",
            Color.FromArgb(22, 163, 74),
            Color.White);

        _connectButton.Enabled = false;
        _connectButton.Margin = new Padding(8, 0, 0, 0);

        ConfigureButton(
            _closeButton,
            "Đóng",
            Color.FromArgb(226, 232, 240),
            Color.FromArgb(30, 41, 59));

        _closeButton.Margin = new Padding(8, 0, 0, 0);

        layout.Controls.Add(_setDefaultCheckBox, 0, 0);
        layout.Controls.Add(_connectButton, 1, 0);
        layout.Controls.Add(_closeButton, 2, 0);

        return layout;
    }

    private void ConfigureTextBox()
    {
        _serverTextBox.Dock = DockStyle.Fill;
        _serverTextBox.Margin = new Padding(0);
        _serverTextBox.BorderStyle = BorderStyle.FixedSingle;
        _serverTextBox.Font = new Font("Segoe UI", 10F);
        _serverTextBox.PlaceholderText =
            @"Ví dụ: MAYCHU hoặc 192.168.1.10";
    }

    private void ConfigureServerComboBox()
    {
        _serverComboBox.Dock = DockStyle.Fill;
        _serverComboBox.Margin = new Padding(0);
        _serverComboBox.DropDownStyle =
            ComboBoxStyle.DropDownList;
        _serverComboBox.Font = new Font("Segoe UI", 10F);
        _serverComboBox.DisplayMember =
            nameof(LanServerInfo.DisplayName);
        _serverComboBox.Enabled = false;
    }

    private void ConfigurePrinterGrid()
    {
        _printerGrid.Dock = DockStyle.Fill;
        _printerGrid.AutoGenerateColumns = false;
        _printerGrid.AllowUserToAddRows = false;
        _printerGrid.AllowUserToDeleteRows = false;
        _printerGrid.AllowUserToResizeRows = false;
        _printerGrid.MultiSelect = false;
        _printerGrid.ReadOnly = true;
        _printerGrid.RowHeadersVisible = false;
        _printerGrid.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect;
        _printerGrid.BackgroundColor = Color.White;
        _printerGrid.BorderStyle = BorderStyle.None;
        _printerGrid.GridColor = Color.FromArgb(226, 232, 240);
        _printerGrid.RowTemplate.Height = 34;

        _printerGrid.ColumnHeadersDefaultCellStyle =
            new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font(
                    "Segoe UI Semibold",
                    9F),
                SelectionBackColor =
                    Color.FromArgb(226, 232, 240),
                SelectionForeColor =
                    Color.FromArgb(30, 41, 59)
            };

        _printerGrid.EnableHeadersVisualStyles = false;

        _printerGrid.DefaultCellStyle =
            new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 41, 59),
                SelectionBackColor =
                    Color.FromArgb(219, 234, 254),
                SelectionForeColor =
                    Color.FromArgb(30, 64, 175)
            };

        _printerGrid.Columns.Add(
            CreateTextColumn(
                "Tên máy in",
                nameof(SharedPrinterInfo.Name),
                180));

        _printerGrid.Columns.Add(
            CreateTextColumn(
                "Tên Share",
                nameof(SharedPrinterInfo.ShareName),
                150));

        _printerGrid.Columns.Add(
            CreateTextColumn(
                "Driver",
                nameof(SharedPrinterInfo.DriverName),
                240));

        _printerGrid.Columns.Add(
            CreateTextColumn(
                "Port",
                nameof(SharedPrinterInfo.PortName),
                140));

        _printerGrid.Columns.Add(
            CreateFillColumn(
                "Đường dẫn kết nối",
                nameof(SharedPrinterInfo.ConnectionName)));
    }

    private void ConnectEvents()
    {
        Shown += (_, _) => _serverTextBox.Focus();

        FormClosing += (_, _) =>
        {
            _operationCancellation?.Cancel();
        };

        _scanButton.Click += async (_, _) =>
        {
            await ScanServersAsync();
        };

        _loadPrintersButton.Click += async (_, _) =>
        {
            await LoadPrintersAsync();
        };

        _connectButton.Click += async (_, _) =>
        {
            await ConnectSelectedPrinterAsync();
        };

        _closeButton.Click += (_, _) =>
        {
            if (_isBusy)
            {
                _operationCancellation?.Cancel();
                return;
            }

            Close();
        };

        _serverTextBox.KeyDown += async (_, eventArgs) =>
        {
            if (eventArgs.KeyCode != Keys.Enter ||
                _isBusy)
            {
                return;
            }

            eventArgs.SuppressKeyPress = true;
            await LoadPrintersAsync();
        };

        _serverComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (_serverComboBox.SelectedItem is
                not LanServerInfo server)
            {
                return;
            }

            _serverTextBox.Text = server.ConnectionTarget;
        };

        _printerGrid.SelectionChanged += (_, _) =>
        {
            UpdateConnectButtonState();
        };

        _printerGrid.CellDoubleClick += async (_, eventArgs) =>
        {
            if (eventArgs.RowIndex < 0 || _isBusy)
            {
                return;
            }

            await ConnectSelectedPrinterAsync();
        };
    }

    private async Task ScanServersAsync()
    {
        StartOperation(
            "Đang quét các máy chủ trong mạng LAN...",
            showProgress: true);

        try
        {
            Progress<int> progress = new(value =>
            {
                int safeValue = Math.Clamp(value, 0, 100);

                _progressBar.Value = safeValue;
                _statusLabel.Text =
                    $"Đang quét mạng LAN... {safeValue}%";
            });

            IReadOnlyList<LanServerInfo> servers =
                await _scannerService.ScanAsync(
                    progress,
                    _operationCancellation!.Token);

            _serverComboBox.DataSource = null;
            _serverComboBox.DataSource = servers.ToList();
            _serverComboBox.DisplayMember =
                nameof(LanServerInfo.DisplayName);
            _serverComboBox.Enabled = servers.Count > 0;

            if (servers.Count == 0)
            {
                SetStatus(
                    "Không tìm thấy máy nào đang mở cổng chia sẻ SMB.",
                    isError: true);

                return;
            }

            _serverComboBox.SelectedIndex = 0;

            SetStatus(
                $"Đã tìm thấy {servers.Count} máy có dịch vụ chia sẻ.");
        }
        catch (OperationCanceledException)
        {
            SetStatus("Đã hủy quét máy chủ.");
        }
        catch (Exception exception)
        {
            ShowError(
                "Không thể quét máy chủ.",
                exception);
        }
        finally
        {
            FinishOperation();
        }
    }

    private async Task LoadPrintersAsync()
    {
        string serverInput = _serverTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(serverInput))
        {
            MessageBox.Show(
                this,
                "Vui lòng nhập tên/IP máy chủ hoặc quét máy chủ trước.",
                "Thiếu tên máy chủ",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            _serverTextBox.Focus();
            return;
        }

        StartOperation(
            $"Đang đọc máy in từ {serverInput}...",
            showProgress: true);

        _progressBar.Style = ProgressBarStyle.Marquee;

        try
        {
            IReadOnlyList<SharedPrinterInfo> printers =
                await _clientPrinterService
                    .GetSharedPrintersAsync(
                        serverInput,
                        _operationCancellation!.Token);

            _printerGrid.DataSource = null;
            _printerGrid.DataSource = printers.ToList();

            if (printers.Count == 0)
            {
                SetStatus(
                    $"Máy {serverInput} không có máy in được share.",
                    isError: true);

                return;
            }

            _printerGrid.ClearSelection();
            _printerGrid.Rows[0].Selected = true;
            _printerGrid.CurrentCell =
                _printerGrid.Rows[0].Cells[0];

            SetStatus(
                $"Đã tìm thấy {printers.Count} máy in được chia sẻ.");
        }
        catch (OperationCanceledException)
        {
            SetStatus("Đã hủy đọc danh sách máy in.");
        }
        catch (Exception exception)
        {
            _printerGrid.DataSource = null;

            ShowError(
                "Không thể đọc danh sách máy in.",
                exception);
        }
        finally
        {
            _progressBar.Style = ProgressBarStyle.Continuous;
            FinishOperation();
        }
    }

    private async Task ConnectSelectedPrinterAsync()
    {
        SharedPrinterInfo? printer = GetSelectedPrinter();

        if (printer is null)
        {
            MessageBox.Show(
                this,
                "Vui lòng chọn một máy in trong danh sách.",
                "Chưa chọn máy in",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        string defaultDescription =
            _setDefaultCheckBox.Checked
                ? Environment.NewLine +
                  "Máy in này cũng sẽ được đặt làm mặc định."
                : string.Empty;

        DialogResult confirmation = MessageBox.Show(
            this,
            $"Kết nối máy in sau?{Environment.NewLine}" +
            $"{printer.ConnectionName}" +
            defaultDescription,
            "Xác nhận kết nối",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        StartOperation(
            $"Đang kết nối {printer.ConnectionName}...",
            showProgress: true);

        _progressBar.Style = ProgressBarStyle.Marquee;

        try
        {
            await _clientPrinterService.ConnectPrinterAsync(
                printer,
                _setDefaultCheckBox.Checked,
                _operationCancellation!.Token);

            SetStatus(
                $"Kết nối thành công: {printer.ConnectionName}");

            PrinterConnected?.Invoke(this, EventArgs.Empty);

            MessageBox.Show(
                this,
                _setDefaultCheckBox.Checked
                    ? "Đã kết nối và đặt máy in làm mặc định."
                    : "Đã kết nối máy in thành công.",
                "Hoàn thành",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            SetStatus("Đã hủy kết nối máy in.");
        }
        catch (Exception exception)
        {
            ShowError(
                "Không thể kết nối máy in.",
                exception);
        }
        finally
        {
            _progressBar.Style = ProgressBarStyle.Continuous;
            FinishOperation();
        }
    }

    private SharedPrinterInfo? GetSelectedPrinter()
    {
        return _printerGrid.CurrentRow?.DataBoundItem
            as SharedPrinterInfo;
    }

    private void StartOperation(
        string status,
        bool showProgress)
    {
        _operationCancellation?.Dispose();
        _operationCancellation =
            new CancellationTokenSource();

        _isBusy = true;
        _statusLabel.ForeColor =
            Color.FromArgb(37, 99, 235);
        _statusLabel.Text = status;

        _progressBar.Value = 0;
        _progressBar.Visible = showProgress;

        UpdateControlStates();
    }

    private void FinishOperation()
    {
        _isBusy = false;
        _progressBar.Visible = false;

        UpdateControlStates();
    }

    private void UpdateControlStates()
    {
        _scanButton.Enabled = !_isBusy;
        _loadPrintersButton.Enabled = !_isBusy;
        _serverTextBox.Enabled = !_isBusy;
        _serverComboBox.Enabled =
            !_isBusy &&
            _serverComboBox.Items.Count > 0;

        _printerGrid.Enabled = !_isBusy;
        _setDefaultCheckBox.Enabled = !_isBusy;

        _closeButton.Text = _isBusy
            ? "Hủy"
            : "Đóng";

        UpdateConnectButtonState();
    }

    private void UpdateConnectButtonState()
    {
        _connectButton.Enabled =
            !_isBusy &&
            GetSelectedPrinter() is not null;
    }

    private void SetStatus(
        string message,
        bool isError = false)
    {
        _statusLabel.ForeColor = isError
            ? Color.FromArgb(220, 38, 38)
            : Color.FromArgb(22, 101, 52);

        _statusLabel.Text = message;
    }

    private void ShowError(
        string title,
        Exception exception)
    {
        SetStatus(exception.Message, isError: true);

        MessageBox.Show(
            this,
            exception.Message,
            title,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private static Panel CreateCardPanel()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    private static Label CreateFieldLabel(string text)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(51, 65, 85),
            Text = text
        };
    }

    private static void ConfigureButton(
        Button button,
        string text,
        Color backgroundColor,
        Color foregroundColor)
    {
        button.Dock = DockStyle.Fill;
        button.Text = text;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = backgroundColor;
        button.ForeColor = foregroundColor;
        button.Font = new Font("Segoe UI Semibold", 9F);
        button.Cursor = Cursors.Hand;
    }

    private static DataGridViewTextBoxColumn CreateTextColumn(
        string title,
        string propertyName,
        int width)
    {
        return new DataGridViewTextBoxColumn
        {
            HeaderText = title,
            DataPropertyName = propertyName,
            Name = propertyName,
            Width = width,
            SortMode =
                DataGridViewColumnSortMode.Automatic
        };
    }

    private static DataGridViewTextBoxColumn CreateFillColumn(
        string title,
        string propertyName)
    {
        return new DataGridViewTextBoxColumn
        {
            HeaderText = title,
            DataPropertyName = propertyName,
            Name = propertyName,
            AutoSizeMode =
                DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 180,
            SortMode =
                DataGridViewColumnSortMode.Automatic
        };
    }
}