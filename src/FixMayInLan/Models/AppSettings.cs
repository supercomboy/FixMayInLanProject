namespace FixMayInLan.Models;

public enum AppTheme
{
    Light,
    Dark
}

public sealed class AppSettings
{
    public AppTheme Theme { get; set; } =
        AppTheme.Light;
}