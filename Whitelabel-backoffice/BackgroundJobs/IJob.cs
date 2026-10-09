namespace Whitelabel_backoffice.BackgroundJobs
{
    public interface IJob
    {
        Task ExecuteAsync(CancellationToken cancellationToken);
    }
}