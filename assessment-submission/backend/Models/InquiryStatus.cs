namespace CourseInquiryApi.Models;

public enum InquiryStatus
{
    New,
    Contacted,
    Pending,
    Registered,
    Closed,
    Archived   // used by soft delete
}
