using System.Text;

namespace FixMayInLan.Core;

/// <summary>
/// Ghi log ra giao diện và file trong ProgramData.
/// </summary>
public sealed class AppLogger : IAppLogger
{
    private readonly object _writeLock = new();

    private readonly string _logDirectory;

    public AppLogger()
    {
        string programData =
            Environment.GetFolderPath(
                Environment.SpecialFolder
                    .CommonApplicationData);

        _logDirectory =
            Path.Combine(
                programData,
                "FixMayInLan",
                "logs");

        Directory.CreateDirectory(
            _logDirectory);

        DeleteExpiredLogs();
    }

    public event EventHandler<LogEntry>?
        EntryWritten;

    public void Info(string message)
    {
        Write(
            LogLevel.Information,
            message);
    }

    public void Warning(string message)
    {
        Write(
            LogLevel.Warning,
            message);
    }

    public void Error(string message)
    {
        Write(
            LogLevel.Error,
            message);
    }

    public void Success(string message)
    {
        Write(
            LogLevel.Success,
            message);
    }

    private void Write(
        LogLevel level,
        string message)
    {
        LogEntry entry = new()
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = message
        };

        string logFile =
            Path.Combine(
                _logDirectory,
                $"fixmayinlan_" +
                $"{DateTime.Now:yyyyMMdd}.log");

        string logLine =
            $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] " +
            $"{GetLevelText(level),-7} " +
            $"{message}" +
            Environment.NewLine;

        try
        {
            lock (_writeLock)
            {
                File.AppendAllText(
                    logFile,
                    logLine,
                    new UTF8Encoding(
                        encoderShouldEmitUTF8Identifier:
                            false));
            }
        }
        catch
        {
            // Không để lỗi ghi file log
            // làm ứng dụng bị dừng.
        }

        EntryWritten?.Invoke(
            this,
            entry);
    }

    private void DeleteExpiredLogs()
    {
        try
        {
            DateTime expirationDate =
                DateTime.UtcNow.AddDays(-30);

            foreach (string logFile in
                     Directory.EnumerateFiles(
                         _logDirectory,
                         "fixmayinlan_*.log"))
            {
                DateTime lastWriteTime =
                    File.GetLastWriteTimeUtc(
                        logFile);

                if (lastWriteTime <
                    expirationDate)
                {
                    File.Delete(logFile);
                }
            }
        }
        catch
        {
            // Không chặn ứng dụng nếu
            // không thể dọn log cũ.
        }
    }

    private static string GetLevelText(
        LogLevel level)
    {
        return level switch
        {
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Success => "OK",
            _ => "LOG"
        };
    }
}