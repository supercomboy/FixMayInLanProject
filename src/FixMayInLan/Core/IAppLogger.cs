namespace FixMayInLan.Core;

public enum LogLevel
{
    Information,
    Warning,
    Error,
    Success
}

public sealed class LogEntry
{
    public DateTime Timestamp { get; init; }

    public LogLevel Level { get; init; }

    public string Message { get; init; } =
        string.Empty;
}

public interface IAppLogger
{
    event EventHandler<LogEntry>? EntryWritten;

    void Info(string message);

    void Warning(string message);

    void Error(string message);

    void Success(string message);
}