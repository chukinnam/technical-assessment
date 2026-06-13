using CourseInquiryApi.Models;

namespace CourseInquiryApi.Services.Crm;

public interface ICrmConnector
{
    Task<CrmDispatchResult> SendInquiryAsync(Inquiry inquiry, CancellationToken ct);
}
