using System.Text.Json;
using Microsoft.Win32;

namespace FixMayInLan.Core;

/// <summary>
/// Snapshot của một giá trị DWORD trong Registry.
/// </summary>
public sealed class DwordRegistrySnapshot
{
    public string Hive { get; init; } =
        string.Empty;

    public string SubKey { get; init; } =
        string.Empty;

    public string ValueName { get; init; } =
        string.Empty;

    public bool Existed { get; init; }

    public int? PreviousValue { get; init; }
}

/// <summary>
/// Sao lưu và khôi phục những giá trị Registry
/// mà ứng dụng trực tiếp thay đổi.
/// </summary>
public sealed class RegistryBackupService
{
    private readonly List<string> _snapshotFiles =
        new();

    private readonly JsonSerializerOptions
        _jsonOptions = new()
        {
            WriteIndented = true
        };

    public string? CurrentSessionFolder
    {
        get;
        private set;
    }

    public bool HasSnapshots =>
        _snapshotFiles.Count > 0;

    /// <summary>
    /// Bắt đầu một phiên backup mới.
    /// </summary>
    public string StartSession()
    {
        _snapshotFiles.Clear();

        string programData =
            Environment.GetFolderPath(
                Environment.SpecialFolder
                    .CommonApplicationData);

        string timestamp =
            DateTime.Now.ToString(
                "yyyyMMdd_HHmmss_fff");

        string backupFolder =
            Path.Combine(
                programData,
                "FixMayInLan",
                "backups",
                timestamp);

        Directory.CreateDirectory(
            backupFolder);

        CurrentSessionFolder =
            backupFolder;

        return backupFolder;
    }

    /// <summary>
    /// Sao lưu một giá trị DWORD trước khi thay đổi.
    /// </summary>
    public string BackupDwordValue(
        RegistryHive hive,
        string subKey,
        string valueName)
    {
        if (CurrentSessionFolder is null)
        {
            StartSession();
        }

        using RegistryKey baseKey =
            RegistryKey.OpenBaseKey(
                hive,
                RegistryView.Registry64);

        using RegistryKey? registryKey =
            baseKey.OpenSubKey(
                subKey,
                writable: false);

        bool existed =
            registryKey?
                .GetValueNames()
                .Contains(
                    valueName,
                    StringComparer.OrdinalIgnoreCase)
            == true;

        int? previousValue = null;

        if (existed)
        {
            RegistryValueKind valueKind =
                registryKey!.GetValueKind(
                    valueName);

            if (valueKind !=
                RegistryValueKind.DWord)
            {
                throw new InvalidOperationException(
                    $"Registry '{valueName}' tồn tại " +
                    $"nhưng không phải kiểu DWORD.");
            }

            object? rawValue =
                registryKey.GetValue(
                    valueName);

            if (rawValue is not null)
            {
                previousValue =
                    Convert.ToInt32(rawValue);
            }
        }

        DwordRegistrySnapshot snapshot =
            new()
            {
                Hive = hive.ToString(),
                SubKey = subKey,
                ValueName = valueName,
                Existed = existed,
                PreviousValue = previousValue
            };

        string safeValueName =
            string.Concat(
                valueName.Select(character =>
                    char.IsLetterOrDigit(character)
                        ? character
                        : '_'));

        string snapshotPath =
            Path.Combine(
                CurrentSessionFolder!,
                $"{_snapshotFiles.Count + 1:00}_" +
                $"{safeValueName}.json");

        string json =
            JsonSerializer.Serialize(
                snapshot,
                _jsonOptions);

        File.WriteAllText(
            snapshotPath,
            json);

        _snapshotFiles.Add(
            snapshotPath);

        return snapshotPath;
    }

    /// <summary>
    /// Khôi phục tất cả snapshot theo thứ tự ngược.
    /// </summary>
    public void RestoreCurrentSession()
    {
        if (_snapshotFiles.Count == 0)
        {
            throw new InvalidOperationException(
                "Không có Registry snapshot để hoàn tác.");
        }

        foreach (string snapshotPath in
                 _snapshotFiles
                     .AsEnumerable()
                     .Reverse())
        {
            string json =
                File.ReadAllText(snapshotPath);

            DwordRegistrySnapshot? snapshot =
                JsonSerializer.Deserialize
                    <DwordRegistrySnapshot>(json);

            if (snapshot is null)
            {
                throw new InvalidDataException(
                    $"Snapshot không hợp lệ: " +
                    snapshotPath);
            }

            RestoreSnapshot(snapshot);
        }
    }

    private static void RestoreSnapshot(
        DwordRegistrySnapshot snapshot)
    {
        RegistryHive hive =
            Enum.Parse<RegistryHive>(
                snapshot.Hive);

        using RegistryKey baseKey =
            RegistryKey.OpenBaseKey(
                hive,
                RegistryView.Registry64);

        using RegistryKey registryKey =
            baseKey.CreateSubKey(
                snapshot.SubKey,
                writable: true)
            ?? throw new UnauthorizedAccessException(
                $"Không thể mở Registry: " +
                snapshot.SubKey);

        if (!snapshot.Existed)
        {
            // Nếu giá trị không tồn tại trước đó,
            // xóa giá trị mà ứng dụng đã tạo.
            registryKey.DeleteValue(
                snapshot.ValueName,
                throwOnMissingValue: false);

            return;
        }

        registryKey.SetValue(
            snapshot.ValueName,
            snapshot.PreviousValue ?? 0,
            RegistryValueKind.DWord);
    }
}