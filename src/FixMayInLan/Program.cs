using FixMayInLan.Core;
using FixMayInLan.UI;

namespace FixMayInLan;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        AppLogger logger = new();

        // Bắt những lỗi xảy ra trên UI thread.
        Application.SetUnhandledExceptionMode(
            UnhandledExceptionMode.CatchException);

        Application.ThreadException +=
            (_, eventArgs) =>
            {
                logger.Error(
                    "Lỗi giao diện chưa được xử lý: " +
                    eventArgs.Exception);

                MessageBox.Show(
                    "Ứng dụng gặp lỗi chưa được xử lý." +
                    "\r\n\r\n" +
                    eventArgs.Exception.Message +
                    "\r\n\r\n" +
                    "Vui lòng kiểm tra file log tại:" +
                    "\r\n" +
                    @"C:\ProgramData\FixMayInLan\logs",
                    "Lỗi ứng dụng",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

        // Bắt lỗi từ background thread.
        AppDomain.CurrentDomain.UnhandledException +=
            (_, eventArgs) =>
            {
                logger.Error(
                    "Lỗi hệ thống chưa được xử lý: " +
                    eventArgs.ExceptionObject);
            };

        logger.Info(
            "Đang khởi động Tool Fix In LAN.");
            using PasswordDialog passwordDialog = new();

DialogResult loginResult =
    passwordDialog.ShowDialog();

if (loginResult != DialogResult.OK)
{
    return;
}

        Application.Run(
            new MainForm(logger));

        logger.Info(
            "Ứng dụng đã đóng.");
    }
}