namespace FixMayInLan.UI;

public sealed class PasswordDialog : Form
{
    // Thay mật khẩu tại đây.
    private const string ApplicationPassword =
        "mq@3009";

    private const int MaximumAttempts = 3;

    private readonly TextBox _passwordTextBox = new();
    private readonly CheckBox _showPasswordCheckBox = new();
    private readonly Button _loginButton = new();
    private readonly Button _exitButton = new();
    private readonly Label _messageLabel = new();

    private int _remainingAttempts = MaximumAttempts;

    public PasswordDialog()
    {
        InitializeForm();
        InitializeControls();
        ConnectEvents();
    }

    private void InitializeForm()
    {
        Text = "Đăng nhập - Fix Máy In LAN";

        StartPosition =
            FormStartPosition.CenterScreen;

        ClientSize = new Size(430, 315);
        MinimumSize = new Size(430, 315);
        MaximumSize = new Size(430, 315);

        FormBorderStyle =
            FormBorderStyle.FixedDialog;

        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;

        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F);

        BackColor = Color.FromArgb(241, 245, 249);
        ForeColor = Color.FromArgb(15, 23, 42);
    }

    private void InitializeControls()
    {
        Panel headerPanel = new()
        {
            Dock = DockStyle.Top,
            Height = 90,
            BackColor = Color.FromArgb(20, 31, 49)
        };

        Label titleLabel = new()
        {
            AutoSize = true,
            Location = new Point(28, 18),
            Font = new Font(
                "Segoe UI Semibold",
                17F),
            ForeColor = Color.White,
            Text = "XÁC THỰC"
        };

        Label descriptionLabel = new()
        {
            AutoSize = true,
            Location = new Point(30, 55),
            Font = new Font("Segoe UI", 9F),
            ForeColor =
                Color.FromArgb(203, 213, 225),
            Text =
                "Nhập mật khẩu để sử dụng Fix Máy In LAN."
        };

        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(descriptionLabel);

        Label passwordLabel = new()
        {
            AutoSize = true,
            Location = new Point(30, 116),
            Font = new Font(
                "Segoe UI Semibold",
                9.5F),
            ForeColor = Color.FromArgb(51, 65, 85),
            Text = "Mật khẩu"
        };

        _passwordTextBox.Location =
            new Point(30, 142);

        _passwordTextBox.Size =
            new Size(370, 30);

        _passwordTextBox.Font =
            new Font("Segoe UI", 11F);

        _passwordTextBox.BorderStyle =
            BorderStyle.FixedSingle;

        _passwordTextBox.UseSystemPasswordChar = true;

        _passwordTextBox.PlaceholderText =
            "Nhập mật khẩu ứng dụng";

        _showPasswordCheckBox.AutoSize = true;
        _showPasswordCheckBox.Location =
            new Point(30, 181);

        _showPasswordCheckBox.Text =
            "Hiện mật khẩu";

        _showPasswordCheckBox.ForeColor =
            Color.FromArgb(71, 85, 105);

        _messageLabel.Location =
            new Point(30, 207);

        _messageLabel.Size =
            new Size(370, 22);

        _messageLabel.TextAlign =
            ContentAlignment.MiddleLeft;

        _messageLabel.ForeColor =
            Color.FromArgb(100, 116, 139);

        _messageLabel.Text =
            $"Bạn có tối đa {MaximumAttempts} lần nhập.";

        ConfigureButton(
            _loginButton,
            "Đăng nhập",
            Color.FromArgb(37, 99, 235),
            Color.White);

        _loginButton.Location =
            new Point(160, 248);

        _loginButton.Size =
            new Size(115, 38);

        ConfigureButton(
            _exitButton,
            "Thoát",
            Color.FromArgb(226, 232, 240),
            Color.FromArgb(30, 41, 59));

        _exitButton.Location =
            new Point(285, 248);

        _exitButton.Size =
            new Size(115, 38);

        Controls.Add(headerPanel);
        Controls.Add(passwordLabel);
        Controls.Add(_passwordTextBox);
        Controls.Add(_showPasswordCheckBox);
        Controls.Add(_messageLabel);
        Controls.Add(_loginButton);
        Controls.Add(_exitButton);

        AcceptButton = _loginButton;
        CancelButton = _exitButton;
    }

    private void ConnectEvents()
    {
        Shown += (_, _) =>
        {
            _passwordTextBox.Focus();
        };

        _showPasswordCheckBox.CheckedChanged +=
            (_, _) =>
            {
                _passwordTextBox
                    .UseSystemPasswordChar =
                    !_showPasswordCheckBox.Checked;
            };

        _loginButton.Click += (_, _) =>
        {
            ValidatePassword();
        };

        _exitButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        _passwordTextBox.TextChanged += (_, _) =>
        {
            if (_messageLabel.ForeColor ==
                Color.FromArgb(220, 38, 38))
            {
                _messageLabel.ForeColor =
                    Color.FromArgb(100, 116, 139);

                _messageLabel.Text =
                    $"Còn {_remainingAttempts} lần nhập.";
            }
        };
    }

    private void ValidatePassword()
    {
        string enteredPassword =
            _passwordTextBox.Text;

        if (string.Equals(
                enteredPassword,
                ApplicationPassword,
                StringComparison.Ordinal))
        {
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        _remainingAttempts--;

        if (_remainingAttempts <= 0)
        {
            MessageBox.Show(
                this,
                "Bạn đã nhập sai mật khẩu quá 3 lần. " +
                "Ứng dụng sẽ đóng.",
                "Truy cập bị từ chối",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        _messageLabel.ForeColor =
            Color.FromArgb(220, 38, 38);

        _messageLabel.Text =
            $"Mật khẩu không đúng. " +
            $"Còn {_remainingAttempts} lần nhập.";

        _passwordTextBox.Clear();
        _passwordTextBox.Focus();
    }

    private static void ConfigureButton(
        Button button,
        string text,
        Color backgroundColor,
        Color foregroundColor)
    {
        button.Text = text;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = backgroundColor;
        button.ForeColor = foregroundColor;

        button.Font = new Font(
            "Segoe UI Semibold",
            9F);

        button.Cursor = Cursors.Hand;
        button.TabStop = false;
    }
}