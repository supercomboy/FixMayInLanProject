using System.Diagnostics;
using System.Text;

namespace FixMayInLan.Core;

public sealed record PowerShellResult(
    int ExitCode,
    string Output,
    string Error)
{
    public bool Succeeded => ExitCode == 0;
}

public sealed class PowerShellRunner
{
    public async Task<PowerShellResult> RunAsync(
        string script,
        CancellationToken cancellationToken = default)
    {
        string encodedCommand =
            Convert.ToBase64String(
                Encoding.Unicode.GetBytes(script));

        ProcessStartInfo startInfo = new()
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-EncodedCommand");
        startInfo.ArgumentList.Add(encodedCommand);

        using Process process = new()
        {
            StartInfo = startInfo
        };

        if (!process.Start())
        {
            throw new InvalidOperationException(
                "Không thể khởi động Windows PowerShell.");
        }

        Task<string> outputTask =
            process.StandardOutput.ReadToEndAsync();

        Task<string> errorTask =
            process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(
            cancellationToken);

        string output = await outputTask;
        string error = await errorTask;

        return new PowerShellResult(
            process.ExitCode,
            output.Trim(),
            error.Trim());
    }
}