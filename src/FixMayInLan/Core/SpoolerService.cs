using System.ServiceProcess;

namespace FixMayInLan.Core;

public sealed class SpoolerService
{
    private readonly IAppLogger _logger;

    public SpoolerService(
        IAppLogger logger)
    {
        _logger = logger;
    }

    public Task RestartAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () =>
            {
                using ServiceController service =
                    new("Spooler");

                service.Refresh();

                if (service.Status !=
                        ServiceControllerStatus.Stopped &&
                    service.Status !=
                        ServiceControllerStatus.StopPending)
                {
                    _logger.Info(
                        "Đang dừng Print Spooler...");

                    service.Stop();
                }

                if (service.Status !=
                    ServiceControllerStatus.Stopped)
                {
                    service.WaitForStatus(
                        ServiceControllerStatus.Stopped,
                        TimeSpan.FromSeconds(30));
                }

                _logger.Success(
                    "Print Spooler đã dừng.");

                cancellationToken
                    .ThrowIfCancellationRequested();

                service.Refresh();

                if (service.Status !=
                        ServiceControllerStatus.Running &&
                    service.Status !=
                        ServiceControllerStatus.StartPending)
                {
                    _logger.Info(
                        "Đang khởi động Print Spooler...");

                    service.Start();
                }

                if (service.Status !=
                    ServiceControllerStatus.Running)
                {
                    service.WaitForStatus(
                        ServiceControllerStatus.Running,
                        TimeSpan.FromSeconds(30));
                }

                _logger.Success(
                    "Print Spooler đang hoạt động.");
            },
            cancellationToken);
    }
}