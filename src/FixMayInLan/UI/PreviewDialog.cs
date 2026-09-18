namespace FixMayInLan.UI;

public sealed class PreviewDialog : Form
{
    public PreviewDialog(string content)
    {
        Text = "Xem trước thay đổi";

        StartPosition =
            FormStartPosition.CenterParent;

        MinimumSize = new Size(700, 430);
        Size = new Size(820, 520);

        BackColor =
            Color.FromArgb(246, 248, 252);

        Font = new Font("Segoe UI", 10F);

        Label title = new()
        {
            Text = "Các thay đổi sẽ được thực hiện",
            Dock = DockStyle.Top,
            Height = 62,
            Padding = new Padding(20, 18, 0, 0),
            Font =
                new Font(
                    "Segoe UI Semibold",
                    14F),
            ForeColor =
                Color.FromArgb(24, 36, 54)
        };

        TextBox previewTextBox = new()
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = true,
            Text = content,
            BackColor = Color.White,
            ForeColor =
                Color.FromArgb(36, 48, 66),
            BorderStyle =
                BorderStyle.FixedSingle,
            Font = new Font("Consolas", 10F)
        };

        Button closeButton = new()
        {
            Text = "Đóng",
            Width = 110,
            Height = 38,
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.Flat,
            BackColor =
                Color.FromArgb(35, 99, 235),
            ForeColor = Color.White
        };

        closeButton.FlatAppearance.BorderSize = 0;

        FlowLayoutPanel footer = new()
        {
            Dock = DockStyle.Bottom,
            Height = 66,
            FlowDirection =
                FlowDirection.RightToLeft,
            Padding =
                new Padding(0, 14, 20, 0)
        };

        Panel contentPanel = new()
        {
            Dock = DockStyle.Fill,
            Padding =
                new Padding(20, 0, 20, 0)
        };

        contentPanel.Controls.Add(
            previewTextBox);

        footer.Controls.Add(closeButton);

        Controls.Add(contentPanel);
        Controls.Add(footer);
        Controls.Add(title);
        ThemeManager.Apply(
    this,
    ThemeManager.CurrentTheme);

        AcceptButton = closeButton;
    }
}