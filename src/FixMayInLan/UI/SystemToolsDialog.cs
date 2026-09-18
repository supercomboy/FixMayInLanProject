using FixMayInLan.Core;

namespace FixMayInLan.UI;

public sealed class SystemToolsDialog : Form
{
    private static readonly Color Navy =
        Color.FromArgb(20, 31, 49);

    private static readonly Color Blue =
        Color.FromArgb(35, 99, 235);

    private static readonly Color TextSecondary =
        Color.FromArgb(100, 116, 139);

    private readonly ComputerNameService
        _computerNameService;

    private readonly IAppLogger _logger;

    private readonly TextBox
        _computerNameTextBox = new();

    private readonly Button
        _renameButton = new();

    private readonly Label
        _renameStatusLabel = new();

    public SystemToolsDialog(
        ComputerNameService computerNameService,
        IAppLogger logger)
    {
        _computerNameService =
            computerNameService;

        _logger = logger;

        Text = "Công cụ hệ thống";

        StartPosition =
            FormStartPosition.CenterParent;

        MinimumSize = new Size(860, 620);
        Size = new Size(940, 700);

        Font = new Font("Segoe UI", 10F);

        Panel header = CreateHeader();

        FlowLayoutPanel content =
            CreateContentPanel();

        content.Controls.Add(
            CreateRenameComputerCard());

        content.Controls.Add(
            CreateComingToolCard(
                "Chuyển mạng Public → Private",
                "Chuyển profile mạng đang hoạt động " +
                "sang Private. Máy Domain sẽ bị chặn.",
                "Sẽ bổ sung ở bước tiếp theo"));

        content.Controls.Add(
            CreateComingToolCard(
                "Bật chia sẻ mạng LAN",
                "Bật Network Discovery và " +
                "File & Printer Sharing cho mạng Private.",
                "Sẽ bổ sung ở bước tiếp theo"));

        content.Controls.Add(
            CreateComingToolCard(
                "Tắt Password Protected Sharing",
                "Cho phép truy cập chia sẻ không cần " +
                "mật khẩu. Đây là chức năng rủi ro cao.",
                "Sẽ bổ sung ở bước tiếp theo"));

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
            Location = new Point(24, 16),
            Font =
                new Font(
                    "Segoe UI Semibold",
                    16F),
            ForeColor = Color.White
        };

        Label description = new()
        {
            Text =
                "Các thao tác quản trị Windows " +
                "hỗ trợ chia sẻ máy in trong LAN.",

            AutoSize = true,
            Location = new Point(26, 50),
            ForeColor =
                Color.FromArgb(200, 214, 235)
        };

        header.Controls.Add(title);
        header.Controls.Add(description);

        return header;
    }

    private static FlowLayoutPanel
        CreateContentPanel()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection =
                FlowDirection.TopDown,
            WrapContents = false,
            Padding =
                new Padding(24, 20, 24, 20)
        };
    }

    private Panel CreateRenameComputerCard()
    {
        Panel card = CreateCard(
            810,
            180);

        Label title = CreateCardTitle(
            "Đổi tên máy tính");

        Label description = new()
        {
            Text =
                "Tên mới có tối đa 15 ký tự, " +
                "chỉ gồm chữ, số và dấu gạch ngang. " +
                "Cần restart để áp dụng.",

            Location = new Point(20, 50),
            AutoSize = true,
            ForeColor = TextSecondary
        };

        Label currentNameLabel = new()
        {
            Text =
                $"Tên hiện tại: " +
                $"{_computerNameService.CurrentComputerName}",

            Location = new Point(20, 80),
            AutoSize = true,
            ForeColor = TextSecondary
        };

        _computerNameTextBox.Location =
            new Point(20, 108);

        _computerNameTextBox.Size =
            new Size(360, 32);

        _computerNameTextBox.MaxLength = 15;

        _computerNameTextBox.CharacterCasing =
            CharacterCasing.Upper;

        _computerNameTextBox.PlaceholderText =
            "Ví dụ: PC-KETOAN-01";

        _renameButton.Text = "Đổi tên máy";

        _renameButton.Location =
            new Point(395, 106);

        _renameButton.Size =
            new Size(135, 36);

        StylePrimaryButton(
            _renameButton);

        _renameStatusLabel.Location =
            new Point(20, 146);

        _renameStatusLabel.AutoSize = true;

        _renameStatusLabel.ForeColor =
            TextSecondary;

        _renameStatusLabel.Text =
            "Chưa có thay đổi.";

        _renameButton.Click += async (_, _) =>
        {
            await RenameComputerAsync();
        };

        card.Controls.Add(title);
        card.Controls.Add(description);
        card.Controls.Add(currentNameLabel);

        card.Controls.Add(
            _computerNameTextBox);

        card.Controls.Add(
            _renameButton);

        card.Controls.Add(
            _renameStatusLabel);

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

            _computerNameTextBox.Focus();
            return;
        }

        try
        {
            if (_computerNameService
                .IsDomainJoined())
            {
                MessageBox.Show(
                    "Máy tính đang thuộc Domain.\r\n\r\n" +
                    "Việc đổi tên cần được thực hiện " +
                    "bởi quản trị viên Domain.",
                    "Không được phép đổi tên",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            DialogResult confirmation =
                MessageBox.Show(
                    $"Tên hiện tại:\r\n" +
                    $"{_computerNameService.CurrentComputerName}" +
                    "\r\n\r\n" +
                    $"Tên mới:\r\n" +
                    $"{newName.ToUpperInvariant()}" +
                    "\r\n\r\n" +
                    "Tên mới chỉ có hiệu lực sau khi " +
                    "khởi động lại Windows.\r\n\r\n" +
                    "Bạn muốn tiếp tục?",
                    "Xác nhận đổi tên máy",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

            if (confirmation != DialogResult.Yes)
            {
                return;
            }

            SetRenameBusy(true);

            _renameStatusLabel.Text =
                "Đang đổi tên máy tính...";

            await _computerNameService
                .RenameAsync(newName);

            _renameStatusLabel.Text =
                $"Đã đặt tên mới: " +
                $"{newName.ToUpperInvariant()}. " +
                "Đang chờ restart.";

            _renameStatusLabel.ForeColor =
                Color.SeaGreen;

            _renameButton.Enabled = false;
            _computerNameTextBox.Enabled = false;

            MessageBox.Show(
                "Windows đã chấp nhận tên máy mới." +
                "\r\n\r\n" +
                $"Tên mới: " +
                $"{newName.ToUpperInvariant()}" +
                "\r\n\r\n" +
                "Hãy lưu công việc đang làm và " +
                "khởi động lại Windows để hoàn tất.",
                "Đổi tên thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            _logger.Error(
                $"Đổi tên máy thất bại: " +
                $"{exception.Message}");

            _renameStatusLabel.Text =
                "Đổi tên máy thất bại.";

            _renameStatusLabel.ForeColor =
                Color.Firebrick;

            MessageBox.Show(
                "Không thể đổi tên máy tính." +
                "\r\n\r\n" +
                exception.Message,
                "Đổi tên thất bại",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            if (_computerNameTextBox.Enabled)
            {
                SetRenameBusy(false);
            }
        }
    }

    private void SetRenameBusy(bool busy)
    {
        UseWaitCursor = busy;

        _renameButton.Enabled = !busy;

        _computerNameTextBox.Enabled =
            !busy;
    }

    private static Panel CreateComingToolCard(
        string titleText,
        string descriptionText,
        string buttonText)
    {
        Panel card = CreateCard(
            810,
            118);

        Label title =
            CreateCardTitle(titleText);

        Label description = new()
        {
            Text = descriptionText,
            Location = new Point(20, 50),
            AutoSize = true,
            ForeColor = TextSecondary
        };

        Button button = new()
        {
            Text = buttonText,
            Location = new Point(575, 43),
            Size = new Size(205, 38),
            Enabled = false
        };

        card.Controls.Add(title);
        card.Controls.Add(description);
        card.Controls.Add(button);

        return card;
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
                new Padding(0, 0, 0, 14),
            Padding = new Padding(20)
        };
    }

    private static Label CreateCardTitle(
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

    private static void StylePrimaryButton(
        Button button)
    {
        button.FlatStyle = FlatStyle.Flat;

        button.FlatAppearance.BorderSize = 0;

        button.BackColor = Blue;
        button.ForeColor = Color.White;
        button.Cursor = Cursors.Hand;
    }
}