using Microsoft.EntityFrameworkCore;
using Whitelabel_backoffice.Database;

namespace Whitelabel_backoffice.BackgroundJobs
{
    public class CancelExpiredSelfDepositsJob
    {
        private readonly WhitelabelContext _context;
        private readonly ILogger<CancelExpiredSelfDepositsJob> _logger;

        public CancelExpiredSelfDepositsJob(
            WhitelabelContext context,
            ILogger<CancelExpiredSelfDepositsJob> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-30);

            var logs = await _context.FinanceLogs
                .Where(x =>
                    x.FinanceType == 0 &&
                    x.Status == 0 &&
                    x.CreatedAt <= cutoffTime)
                .OrderBy(x => x.Id)
                .Take(100)
                .ToListAsync();

            if (logs.Count == 0)
            {
                return;
            }

            foreach (var log in logs)
            {
                log.Status = 5;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Cancelled {Count} expired self-deposit finance logs.",
                logs.Count);
        }
    }
}