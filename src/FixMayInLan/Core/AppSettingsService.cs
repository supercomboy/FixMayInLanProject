using System.Text.Json;
using FixMayInLan.Models;

namespace FixMayInLan.Core;

public sealed class AppSettingsService
{
    private readonly string _settingsDirectory;
    private readonly string _settingsFile;

    private readonly JsonSerializerOptions
        _jsonOptions = new()
        {
            WriteIndented = true
        };

    public AppSettingsService()
    {
        string localAppData =
            Environment.GetFolderPath(
                Environment.SpecialFolder
                    .LocalApplicationData);

        _settingsDirectory =
            Path.Combine(
                localAppData,
                "FixMayInLan");

        _settingsFile =
            Path.Combine(
                _settingsDirectory,
                "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsFile))
            {
                return new AppSettings();
            }

            string json =
                File.ReadAllText(_settingsFile);

            return JsonSerializer.Deserialize
                       <AppSettings>(json)
                   ?? new AppSettings();
        }
        catch
        {
            // Nếu file settings bị lỗi,
            // quay về giao diện Light.
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(
            _settingsDirectory);

        string json =
            JsonSerializer.Serialize(
                settings,
                _jsonOptions);

        File.WriteAllText(
            _settingsFile,
            json);
    }
}