using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Whitelabel_backoffice.BackgroundJobs
{
    public class JobRunner : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<JobRunner> _logger;

        public JobRunner(
            IServiceScopeFactory scopeFactory,
            ILogger<JobRunner> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var job = scope.ServiceProvider
                        .GetRequiredService<CancelExpiredSelfDepositsJob>();

                    await job.ExecuteAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error executing CancelExpiredSelfDepositsJob.");
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(30),
                    stoppingToken);
            }
        }
    }
}