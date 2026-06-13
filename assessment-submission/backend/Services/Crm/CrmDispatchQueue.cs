using CourseInquiryApi.Data;
using CourseInquiryApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseInquiryApi.Services.Crm;

public class CrmDispatchWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceProvider _services;
    private readonly ILogger<CrmDispatchWorker> _logger;

    public CrmDispatchWorker(IServiceProvider services, ILogger<CrmDispatchWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var dispatcher = scope.ServiceProvider.GetRequiredService<CrmSyncDispatcher>();

                var ids = await db.CrmSyncLogs
                    .Where(l => l.Status == CrmSyncStatus.Pending || l.Status == CrmSyncStatus.Attempted)
                    .Select(l => l.InquiryId)
                    .Distinct()
                    .ToListAsync(stoppingToken);

                foreach (var id in ids)
                {
                    try
                    {
                        await dispatcher.DispatchAsync(id, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        // Safety net so one bad inquiry never aborts the rest of the batch.
                        _logger.LogError(ex, "CRM dispatch crashed for inquiry {InquiryId}", id);
                    }
                }
            }
            catch (Exception ex)
            {
                // Poll/query itself failed — log and try again next sweep.
                _logger.LogError(ex, "CRM outbox poll failed");
            }

            await Task.Delay(PollInterval, stoppingToken);   // batch done -> idle -> next sweep
        }
    }
}
