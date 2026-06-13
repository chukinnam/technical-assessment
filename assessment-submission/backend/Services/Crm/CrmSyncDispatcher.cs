using CourseInquiryApi.Data;
using CourseInquiryApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseInquiryApi.Services.Crm;

public class CrmSyncDispatcher
{
    private const int MaxAttempts = 3;

    private readonly ICrmConnector _crm;
    private readonly AppDbContext _db;
    private readonly ILogger<CrmSyncDispatcher> _logger;

    public CrmSyncDispatcher(ICrmConnector crm, AppDbContext db, ILogger<CrmSyncDispatcher> logger)
    {
        _crm = crm;
        _db = db;
        _logger = logger;
    }

    public async Task DispatchAsync(int inquiryId, CancellationToken ct)
    {
        //query the inquiry to sync
        var inquiry = await _db.Inquiries.FindAsync(new object?[] { inquiryId }, ct);
        if (inquiry is null)
        {
            _logger.LogWarning("CRM sync skipped: inquiry {InquiryId} not found", inquiryId);
            return;
        }

        // Mask PII: log only first char + domain, never the full address / name / message.
        var maskedEmail = SensitiveData.MaskEmail(inquiry.Email);
        //quesry the existing log
        var log = await _db.CrmSyncLogs
            .Where(l => l.InquiryId == inquiryId &&(l.Status == CrmSyncStatus.Pending || l.Status == CrmSyncStatus.Attempted))
            .OrderBy(l => l.Id)
            .FirstOrDefaultAsync(ct);
        if (log is null)
        {
            _logger.LogWarning("CRM Sync Log: inquiry {InquiryId} not found", inquiryId);
            return;
        }
        //set the log status to attempted and save before start SendInquiryAsync
        log.Status = CrmSyncStatus.Attempted;
        log.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("CRM sync attempted for inquiry {InquiryId} ({Email})", inquiryId, maskedEmail);
        // Try to send the inquiry to CRM
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            log.Attempts = attempt;

            try
            {
                var result = await _crm.SendInquiryAsync(inquiry, ct);

                if (result.Success)
                {
                    log.Status = CrmSyncStatus.Succeeded;
                    log.ExternalId = result.ExternalId;
                    log.UpdatedDate = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);

                    _logger.LogInformation(
                        "CRM sync succeeded for inquiry {InquiryId} on attempt {Attempt}. ExternalId={ExternalId}",
                        inquiryId, attempt, result.ExternalId);
                    return;
                }

                var error = result.Error!;
                log.LastErrorCode = error.Code + error.Message;
                //if returb error is not transient, then no more retry, otherwise retry until max attempts
                if (!error.IsTransient)
                {
                    await FailAsync(log, inquiryId, attempt, error, maskedEmail, willRetry: false, ct);
                    return;
                }

                var willRetry = attempt < MaxAttempts;
                //keep update the log with error code and retry status
                await FailAsync(log, inquiryId, attempt, error, maskedEmail, willRetry, ct);
                if (!willRetry) return;
            }
            catch (Exception ex)
            {
                // Unexpected exception 
                log.LastErrorCode = "UNEXPECTED";
                var willRetry = attempt < MaxAttempts;
                _logger.LogWarning(ex,
                    "CRM sync error for inquiry {InquiryId} on attempt {Attempt} (willRetry={WillRetry})",
                    inquiryId, attempt, willRetry);

                if (!willRetry)
                {
                    log.Status = CrmSyncStatus.Failed;
                    log.UpdatedDate = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);
                    return;
                }
            }
        }
    }

    private async Task FailAsync(CrmSyncLog log, int inquiryId, int attempt, CrmError error,
        string maskedEmail, bool willRetry, CancellationToken ct)
    {
        log.Status = willRetry ? CrmSyncStatus.Attempted : CrmSyncStatus.Failed;
        log.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Log the error CODE and masked email only — never raw inquiry data.
        _logger.LogWarning(
            "CRM sync failed for inquiry {InquiryId} ({Email}) on attempt {Attempt}. " +
            "Code={Code}, Transient={Transient}, WillRetry={WillRetry}",
            inquiryId, maskedEmail, attempt, error.Code, error.IsTransient, willRetry);
    }
}


public static class SensitiveData
{
    //MaskEmail
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "(none)";
        var at = email.IndexOf('@');
        if (at <= 0) return "***";
        return $"{email[0]}***@{email[(at + 1)..]}";
    }
}
