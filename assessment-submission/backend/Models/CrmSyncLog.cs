namespace CourseInquiryApi.Models;

public enum CrmSyncStatus { Pending, Attempted, Succeeded, Failed }

public class CrmSyncLog
{
    public int Id { get; set; }
    public int InquiryId { get; set; }
    public CrmSyncStatus Status { get; set; } = CrmSyncStatus.Pending;
    public int Attempts { get; set; }
    public string? ExternalId { get; set; }
    public string? LastErrorCode { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
