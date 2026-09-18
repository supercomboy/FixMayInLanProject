using FixMayInLan.Core;

namespace FixMayInLan.UI;

public sealed class SystemToolsDialog : Form
{
    private static readonly Color Navy =
        Color.FromArgb(20, 31, 49);

    private static readonly Color Blue =
        Color.FromArgb(35, 99, 235);

    private static readonly Color Danger =
        Color.FromArgb(190, 40, 40);

    private static readonly Color TextSecondary =
        Color.FromArgb(100, 116, 139);

    private readonly ComputerNameService
        _computerNameService;

    private readonly NetworkSharingService
        _networkSharingService;

    private readonly IAppLogger _logger;

    private readonly TextBox
        _computerNameTextBox = new();

    private readonly Button
        _renameButton = new();

    private readonly Button
        _privateButton = new();

    private readonly Button
        _sharingButton = new();

    private readonly Button
        _passwordButton = new();

    private readonly Button
        _undoPasswordButton = new();

    private readonly Label
        _renameStatusLabel = new();

    private readonly Label
        _privateStatusLabel = new();

    private readonly Label
        _sharingStatusLabel = new();

    private readonly Label
        _passwordStatusLabel = new();

    public SystemToolsDialog(
        ComputerNameService computerNameService,
        IAppLogger logger)
    {
        _computerNameService =
            computerNameService;

        _logger = logger;

        _networkSharingService =
            new NetworkSharingService(
                new PowerShellRunner(),
                new RegistryBackupService(),
                computerNameService,
                logger);

        Text = "Công cụ hệ thống";

        StartPosition =
            FormStartPosition.CenterParent;

        MinimumSize =
            new Size(900, 650);

        Size =
            new Size(960, 760);

        Font =
            new Font("Segoe UI", 10F);

        Panel header =
            CreateHeader();

        FlowLayoutPanel content =
            new()
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection =
                    FlowDirection.TopDown,
                WrapContents = false,
                Padding =
                    new Padding(24, 20, 24, 20)
            };

        content.Controls.Add(
            CreateRenameCard());

        content.Controls.Add(
            CreatePrivateNetworkCard());

        content.Controls.Add(
            CreateLanSharingCard());

        content.Controls.Add(
            CreatePasswordSharingCard());

        Controls.Add(content);
        Controls.Add(header);

        ThemeManager.Apply(
            this,
            ThemeManager.CurrentTheme);
    }

    private Panel CreateHeader()
    {
        Panel header = new()
        {
            Dock = DockStyle.Top,
            Height = 82,
            BackColor = Navy
        };

        Label title = new()
        {
            Text = "CÔNG CỤ HỆ THỐNG",
            AutoSize = true,
            Location = new Point(24, 15),
            Font =
                new Font(
                    "Segoe UI Semibold",
                    16F),
            ForeColor = Color.White
        };

        Label description = new()
        {
            Text =
                "Cấu hình Windows phục vụ " +
                "chia sẻ máy in trong LAN.",

            AutoSize = true,
            Location = new Point(26, 50),
            ForeColor =
                Color.FromArgb(200, 214, 235)
        };

        header.Controls.Add(title);
        header.Controls.Add(description);

        return header;
    }

    private Panel CreateRenameCard()
    {
        Panel card = CreateCard(810, 178);

        card.Controls.Add(
            CreateTitle("Đổi tên máy tính"));

        Label description = CreateDescription(
            "Tên mới tối đa 15 ký tự. " +
            "Cần khởi động lại Windows.",
            50);

        Label currentName = CreateDescription(
            $"Tên hiện tại: " +
            $"{_computerNameService.CurrentComputerName}",
            78);

        _computerNameTextBox.SetBounds(
            20, 108, 350, 32);

        _computerNameTextBox.MaxLength = 15;

        _computerNameTextBox.CharacterCasing =
            CharacterCasing.Upper;

        _computerNameTextBox.PlaceholderText =
            "Ví dụ: PC-KETOAN-01";

        ConfigureButton(
            _renameButton,
            "Đổi tên máy",
            Blue);

        _renameButton.SetBounds(
            385, 106, 140, 36);

        ConfigureStatusLabel(
            _renameStatusLabel,
            "Chưa có thay đổi.",
            146);

        _renameButton.Click +=
            async (_, _) =>
            {
                await RenameComputerAsync();
            };

        card.Controls.Add(description);
        card.Controls.Add(currentName);
        card.Controls.Add(_computerNameTextBox);
        card.Controls.Add(_renameButton);
        card.Controls.Add(_renameStatusLabel);

        return card;
    }

    private Panel CreatePrivateNetworkCard()
    {
        Panel card = CreateCard(810, 135);

        card.Controls.Add(
            CreateTitle(
                "Chuyển mạng Public → Private"));

        card.Controls.Add(
            CreateDescription(
                "Chỉ thay đổi profile mạng đang hoạt động. " +
                "Máy Domain sẽ bị chặn.",
                50));

        ConfigureButton(
            _privateButton,
            "Chuyển sang Private",
            Blue);

        _privateButton.SetBounds(
            575, 42, 205, 38);

        ConfigureStatusLabel(
            _privateStatusLabel,
            "Chưa kiểm tra.",
            92);

        _privateButton.Click +=
            async (_, _) =>
            {
                DialogResult answer =
                    MessageBox.Show(
                        "Ứng dụng sẽ chạy:\r\n\r\n" +
                        "Set-NetConnectionProfile " +
                        "-NetworkCategory Private\r\n\r\n" +
                        "Chỉ tiếp tục nếu đây là mạng LAN tin cậy.",
                        "Xác nhận đổi network profile",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2);

                if (answer != DialogResult.Yes)
                {
                    return;
                }

                await RunActionAsync(
                    _privateButton,
                    _privateStatusLabel,
                    "Đang chuyển network profile...",
                    _networkSharingService
                        .ChangePublicToPrivateAsync,
                    "Đã chuyển sang Private.");
            };

        card.Controls.Add(_privateButton);
        card.Controls.Add(_privateStatusLabel);

        return card;
    }

    private Panel CreateLanSharingCard()
    {
        Panel card = CreateCard(810, 135);

        card.Controls.Add(
            CreateTitle(
                "Bật chia sẻ mạng LAN"));

        card.Controls.Add(
            CreateDescription(
                "Bật Network Discovery, File & Printer " +
                "Sharing và firewall rule cho Private.",
                50));

        ConfigureButton(
            _sharingButton,
            "Bật chia sẻ LAN",
            Blue);

        _sharingButton.SetBounds(
            575, 42, 205, 38);

        ConfigureStatusLabel(
            _sharingStatusLabel,
            "Yêu cầu mạng Private.",
            92);

        _sharingButton.Click +=
            async (_, _) =>
            {
                DialogResult answer =
                    MessageBox.Show(
                        "Ứng dụng sẽ:\r\n\r\n" +
                        "• Khởi động dịch vụ Network Discovery\r\n" +
                        "• Bật File & Printer Sharing\r\n" +
                        "• Chỉ mở firewall cho profile Private\r\n\r\n" +
                        "Bạn muốn tiếp tục?",
                        "Xác nhận bật chia sẻ LAN",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);

                if (answer != DialogResult.Yes)
                {
                    return;
                }

                await RunActionAsync(
                    _sharingButton,
                    _sharingStatusLabel,
                    "Đang bật chia sẻ mạng LAN...",
                    _networkSharingService
                        .EnableLanSharingAsync,
                    "Đã bật chia sẻ cho mạng Private.");
            };

        card.Controls.Add(_sharingButton);
        card.Controls.Add(_sharingStatusLabel);

        return card;
    }

    private Panel CreatePasswordSharingCard()
    {
        Panel card = CreateCard(810, 170);

        card.Controls.Add(
            CreateTitle(
                "Tắt Password Protected Sharing"));

        Label warning = CreateDescription(
            "RỦI RO CAO: Cho phép Guest/anonymous tiếp cận " +
            "tài nguyên chia sẻ. Chỉ dùng trong LAN tin cậy.",
            50);

        warning.ForeColor =
            Color.FromArgb(173, 103, 0);

        ConfigureButton(
            _passwordButton,
            "Tắt bảo vệ mật khẩu",
            Danger);

        _passwordButton.SetBounds(
            575, 42, 205, 38);

        ConfigureButton(
            _undoPasswordButton,
            "Hoàn tác",
            Color.FromArgb(71, 85, 105));

        _undoPasswordButton.SetBounds(
            575, 88, 205, 36);

        _undoPasswordButton.Visible = false;

        ConfigureStatusLabel(
            _passwordStatusLabel,
            "Đang bật bảo vệ mật khẩu.",
            132);

        _passwordButton.Click +=
            async (_, _) =>
            {
                await DisablePasswordSharingAsync();
            };

        _undoPasswordButton.Click +=
            async (_, _) =>
            {
                await UndoPasswordSharingAsync();
            };

        card.Controls.Add(warning);
        card.Controls.Add(_passwordButton);
        card.Controls.Add(_undoPasswordButton);
        card.Controls.Add(_passwordStatusLabel);

        return card;
    }

    private async Task RenameComputerAsync()
    {
        string newName =
            _computerNameTextBox.Text.Trim();

        string? validationError =
            _computerNameService
                .ValidateNewName(newName);

        if (validationError is not null)
        {
            MessageBox.Show(
                validationError,
                "Tên máy không hợp lệ",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        DialogResult answer =
            MessageBox.Show(
                $"Đổi tên máy thành " +
                $"'{newName.ToUpperInvariant()}'?\r\n\r\n" +
                "Cần restart để áp dụng.",
                "Xác nhận đổi tên",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

        if (answer != DialogResult.Yes)
        {
            return;
        }

        await RunActionAsync(
            _renameButton,
            _renameStatusLabel,
            "Đang đổi tên máy...",
            cancellationToken =>
                _computerNameService.RenameAsync(
                    newName,
                    cancellationToken),
            "Đã đổi tên. Hãy restart Windows.");
    }

    private async Task DisablePasswordSharingAsync()
    {
        DialogResult firstAnswer =
            MessageBox.Show(
                "Tắt Password Protected Sharing sẽ làm giảm " +
                "bảo mật của Windows.\r\n\r\n" +
                "Người trong LAN có thể thử truy cập tài nguyên " +
                "chia sẻ mà không cần tài khoản/mật khẩu.\r\n\r\n" +
                "Bạn có hiểu rủi ro và muốn tiếp tục?",
                "Cảnh báo bảo mật",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

        if (firstAnswer != DialogResult.Yes)
        {
            return;
        }

        DialogResult secondAnswer =
            MessageBox.Show(
                "XÁC NHẬN LẦN CUỐI\r\n\r\n" +
                "Chỉ sử dụng trên mạng Private tin cậy. " +
                "Không sử dụng tại Wi-Fi công cộng.\r\n\r\n" +
                "Tiếp tục tắt bảo vệ mật khẩu?",
                "Xác nhận rủi ro",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

        if (secondAnswer != DialogResult.Yes)
        {
            return;
        }

        await RunActionAsync(
            _passwordButton,
            _passwordStatusLabel,
            "Đang sao lưu và thay đổi Registry...",
            _networkSharingService
                .DisablePasswordProtectedSharingAsync,
            "Password Protected Sharing: OFF.");

        _undoPasswordButton.Visible = true;

        MessageBox.Show(
            "Đã tắt Password Protected Sharing.\r\n\r\n" +
            "Windows 11 mới có thể vẫn chặn SMB Guest do " +
            "yêu cầu SMB signing. Không nên tắt thêm các " +
            "tính năng bảo mật khác nếu không thật sự cần.",
            "Đã thay đổi",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private async Task UndoPasswordSharingAsync()
    {
        await RunActionAsync(
            _undoPasswordButton,
            _passwordStatusLabel,
            "Đang khôi phục Registry...",
            _ =>
                _networkSharingService
                    .UndoPasswordProtectedSharingAsync(),
            "Đã khôi phục bảo vệ mật khẩu.");

        _undoPasswordButton.Visible = false;
    }

    private async Task RunActionAsync(
        Button button,
        Label statusLabel,
        string runningText,
        Func<CancellationToken, Task> action,
        string successText)
    {
        button.Enabled = false;
        UseWaitCursor = true;

        statusLabel.Text = runningText;
        statusLabel.ForeColor = TextSecondary;

        try
        {
            await action(CancellationToken.None);

            statusLabel.Text = successText;
            statusLabel.ForeColor = Color.SeaGreen;

            _logger.Success(successText);
        }
        catch (Exception exception)
        {
            statusLabel.Text =
                "Thao tác thất bại.";

            statusLabel.ForeColor =
                Color.Firebrick;

            _logger.Error(
                exception.Message);

            MessageBox.Show(
                exception.Message,
                "Không thể hoàn tất",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            button.Enabled = true;
            UseWaitCursor = false;
        }
    }

    private static Panel CreateCard(
        int width,
        int height)
    {
        return new Panel
        {
            Width = width,
            Height = height,
            BackColor = Color.White,
            Margin =
                new Padding(0, 0, 0, 14)
        };
    }

    private static Label CreateTitle(
        string text)
    {
        return new Label
        {
            Text = text,
            Location = new Point(20, 16),
            AutoSize = true,
            Font =
                new Font(
                    "Segoe UI Semibold",
                    12F),
            ForeColor = Navy
        };
    }

    private static Label CreateDescription(
        string text,
        int top)
    {
        return new Label
        {
            Text = text,
            Location = new Point(20, top),
            AutoSize = true,
            MaximumSize = new Size(530, 0),
            ForeColor = TextSecondary
        };
    }

    private static void ConfigureStatusLabel(
        Label label,
        string text,
        int top)
    {
        label.Text = text;
        label.Location = new Point(20, top);
        label.AutoSize = true;
        label.ForeColor = TextSecondary;
    }

    private static void ConfigureButton(
        Button button,
        string text,
        Color background)
    {
        button.Text = text;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = background;
        button.ForeColor = Color.White;
        button.Cursor = Cursors.Hand;
    }
}