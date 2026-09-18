namespace FixMayInLan.Models;

/// <summary>
/// Kết quả xử lý một mã lỗi.
/// </summary>
public sealed class FixResult
{
    public PrinterIssue Issue { get; init; }

    public bool Succeeded { get; init; }

    public string Message { get; init; } =
        string.Empty;
}

/// <summary>
/// Thông tin tiến trình gửi từ Core lên giao diện.
/// </summary>
public sealed class FixProgress
{
    public int Percent { get; init; }

    public string Status { get; init; } =
        string.Empty;
}