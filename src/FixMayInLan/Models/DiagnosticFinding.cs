namespace FixMayInLan.Models;

/// <summary>
/// Hai lỗi duy nhất được ứng dụng hỗ trợ.
/// </summary>
public enum PrinterIssue
{
    Error0000011B,
    Error00000709
}

public enum FindingSeverity
{
    Information,
    Warning,
    Error
}

/// <summary>
/// Kết quả của một phép chẩn đoán.
/// </summary>
public sealed class DiagnosticFinding
{
    public PrinterIssue Issue { get; init; }

    public string Code { get; init; } =
        string.Empty;

    public string Title { get; init; } =
        string.Empty;

    public string Description { get; init; } =
        string.Empty;

    public FindingSeverity Severity { get; init; }

    /// <summary>
    /// Có thể thực hiện sửa trên máy hiện tại hay không.
    /// </summary>
    public bool CanFixLocally { get; init; }

    /// <summary>
    /// Có tự động tick đề xuất sửa hay không.
    /// </summary>
    public bool IsRecommended { get; init; }

    /// <summary>
    /// Nội dung sẽ hiển thị trong phần xem trước thay đổi.
    /// </summary>
    public string Preview { get; init; } =
        string.Empty;

    public string? SecurityWarning { get; init; }

    public string SeverityDisplay
    {
        get
        {
            return Severity switch
            {
                FindingSeverity.Information =>
                    "Thông tin",

                FindingSeverity.Warning =>
                    "Cảnh báo",

                FindingSeverity.Error =>
                    "Lỗi",

                _ => "Không xác định"
            };
        }
    }

    public string LocalActionDisplay =>
        CanFixLocally
            ? "Có thể xử lý"
            : "Không xử lý tại máy này";
}