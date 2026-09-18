using System.ComponentModel;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace FixMayInLan.Core;

public sealed class ComputerNameService
{
    private const int MaximumComputerNameLength = 15;

    private readonly IAppLogger _logger;

    public ComputerNameService(
        IAppLogger logger)
    {
        _logger = logger;
    }

    private enum ComputerNameFormat
    {
        ComputerNameNetBIOS = 0,
        ComputerNameDnsHostname = 1,
        ComputerNameDnsDomain = 2,
        ComputerNameDnsFullyQualified = 3,
        ComputerNamePhysicalNetBIOS = 4,
        ComputerNamePhysicalDnsHostname = 5,
        ComputerNamePhysicalDnsDomain = 6,
        ComputerNamePhysicalDnsFullyQualified = 7,
        ComputerNameMax = 8
    }

    [DllImport(
        "Kernel32.dll",
        EntryPoint = "SetComputerNameExW",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetComputerNameEx(
        ComputerNameFormat nameType,
        string newName);

    public string CurrentComputerName =>
        Environment.MachineName;

    public bool IsDomainJoined()
    {
        const string query = """
            SELECT PartOfDomain
            FROM Win32_ComputerSystem
            """;

        using ManagementObjectSearcher searcher =
            new(query);

        using ManagementObjectCollection results =
            searcher.Get();

        foreach (ManagementObject item in results)
        {
            return item["PartOfDomain"]
                is bool value && value;
        }

        return false;
    }

    /// <summary>
    /// Trả về null nếu tên hợp lệ.
    /// Nếu không hợp lệ, trả về nội dung lỗi.
    /// </summary>
    public string? ValidateNewName(
        string? newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return "Tên máy tính không được để trống.";
        }

        string normalizedName =
            newName.Trim();

        if (normalizedName.Length >
            MaximumComputerNameLength)
        {
            return
                "Tên máy tính không được vượt quá " +
                $"{MaximumComputerNameLength} ký tự.";
        }

        if (normalizedName.StartsWith('-') ||
            normalizedName.EndsWith('-'))
        {
            return
                "Tên máy tính không được bắt đầu " +
                "hoặc kết thúc bằng dấu gạch ngang.";
        }

        if (!Regex.IsMatch(
                normalizedName,
                @"^[A-Za-z0-9-]+$"))
        {
            return
                "Tên máy tính chỉ được chứa chữ cái, " +
                "chữ số và dấu gạch ngang.";
        }

        if (Regex.IsMatch(
                normalizedName,
                @"^[0-9]+$"))
        {
            return
                "Tên máy tính không nên chỉ chứa chữ số.";
        }

        if (string.Equals(
                normalizedName,
                CurrentComputerName,
                StringComparison.OrdinalIgnoreCase))
        {
            return
                "Tên mới giống tên máy tính hiện tại.";
        }

        return null;
    }

    public Task RenameAsync(
        string newName,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () =>
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                string normalizedName =
                    newName.Trim()
                        .ToUpperInvariant();

                string? validationError =
                    ValidateNewName(normalizedName);

                if (validationError is not null)
                {
                    throw new ArgumentException(
                        validationError,
                        nameof(newName));
                }

                if (IsDomainJoined())
                {
                    throw new InvalidOperationException(
                        "Máy tính đang thuộc Domain. " +
                        "Ứng dụng không cho phép đổi tên " +
                        "máy Domain-joined.");
                }

                string oldName =
                    CurrentComputerName;

                _logger.Warning(
                    $"Chuẩn bị đổi tên máy từ " +
                    $"'{oldName}' thành " +
                    $"'{normalizedName}'.");

                bool succeeded =
                    SetComputerNameEx(
                        ComputerNameFormat
                            .ComputerNamePhysicalDnsHostname,
                        normalizedName);

                if (!succeeded)
                {
                    int errorCode =
                        Marshal.GetLastWin32Error();

                    throw new Win32Exception(
                        errorCode,
                        "Windows không thể đổi tên máy tính.");
                }

                _logger.Success(
                    $"Đã đặt tên máy mới thành " +
                    $"'{normalizedName}'. " +
                    "Cần khởi động lại Windows.");
            },
            cancellationToken);
    }
}