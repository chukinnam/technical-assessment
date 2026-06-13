using CourseInquiryApi.Data;
using CourseInquiryApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseInquiryApi.Services;

public class InquiryService : IInquiryService
{
    private readonly AppDbContext _db;

    public InquiryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<InquiryResponse> CreateAsync(CreateInquiryRequest request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var inquiry = new Inquiry
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone?.Trim(),
            CourseId = request.CourseId,
            PreferredLocation = request.PreferredLocation?.Trim(),
            Message = request.Message?.Trim(),
            Status = InquiryStatus.New,   
            CreatedDate = now,  // set automatically
            UpdatedDate = now   // set automatically
        };

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        _db.Inquiries.Add(inquiry);
        await _db.SaveChangesAsync(ct);   

        _db.CrmSyncLogs.Add(new CrmSyncLog
        {
            InquiryId = inquiry.Id,
            Status = CrmSyncStatus.Pending,      
            CreatedDate = now,
            UpdatedDate = now
        });
        await _db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return InquiryResponse.From(inquiry);
    }

    public async Task<IReadOnlyList<InquiryResponse>> GetAllAsync(InquiryStatus? status, CancellationToken ct)
    {
        var query = _db.Inquiries.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }else{     
            query = query.Where(i => i.Status != InquiryStatus.Archived); 
        }
        var items = await query.OrderByDescending(i => i.CreatedDate).ToListAsync(ct);
        return items.Select(item => InquiryResponse.From(item)).ToList();
    }

    public async Task<InquiryResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var inquiry = await _db.Inquiries.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);
        return inquiry is null ? null : InquiryResponse.From(inquiry);
    }

    public async Task<InquiryResponse?> UpdateStatusAsync(int id, InquiryStatus status, CancellationToken ct)
    {
        var inquiry = await _db.Inquiries.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (inquiry is null) return null;

        inquiry.Status = status;
        inquiry.UpdatedDate = DateTime.UtcNow;   // set automatically on change
        await _db.SaveChangesAsync(ct);
        return InquiryResponse.From(inquiry);
    }

    public async Task<bool> ArchiveAsync(int id, CancellationToken ct)
    {
        var inquiry = await _db.Inquiries.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (inquiry is null) return false;

        inquiry.Status = InquiryStatus.Archived; // soft delete (see README for rationale)
        inquiry.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
