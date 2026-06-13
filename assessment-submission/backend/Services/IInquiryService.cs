using CourseInquiryApi.Models;

namespace CourseInquiryApi.Services;

public interface IInquiryService
{
    Task<InquiryResponse> CreateAsync(CreateInquiryRequest request, CancellationToken ct);
    Task<IReadOnlyList<InquiryResponse>> GetAllAsync(InquiryStatus? status, CancellationToken ct);
    Task<InquiryResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<InquiryResponse?> UpdateStatusAsync(int id, InquiryStatus status, CancellationToken ct);
    Task<bool> ArchiveAsync(int id, CancellationToken ct);
}
