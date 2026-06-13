using CourseInquiryApi.Data;
using CourseInquiryApi.Models;
using CourseInquiryApi.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CourseInquiryApi.Tests.Services;

public class InquiryServiceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private InquiryService _service = null!;

    public Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _service = new InquiryService(_db);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static CreateInquiryRequest ValidRequest() => new()
    {
        FirstName = "  Ada",
        LastName = "Lovelace  ",
        Email = "  ada@example.com  ",
        Phone = " 555-0100 ",
        CourseId = 1,
        PreferredLocation = " Online ",
        Message = " Interested in details. "
    };

    private Inquiry SeedInquiry(InquiryStatus status, string firstName, DateTime? createdAt = null)
    {
        var ts = createdAt ?? DateTime.UtcNow;
        var inquiry = new Inquiry
        {
            FirstName = firstName,
            LastName = "X",
            Email = "x@example.com",
            CourseId = 1,
            Status = status,
            CreatedDate = ts,
            UpdatedDate = ts
        };
        _db.Inquiries.Add(inquiry);
        _db.SaveChanges();
        return inquiry;
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_TrimsStringFieldsAndPersists()
    {
        var response = await _service.CreateAsync(ValidRequest(), CancellationToken.None);

        var saved = await _db.Inquiries.AsNoTracking().SingleAsync();
        Assert.Equal("Ada", saved.FirstName);
        Assert.Equal("Lovelace", saved.LastName);
        Assert.Equal("ada@example.com", saved.Email);
        Assert.Equal("555-0100", saved.Phone);
        Assert.Equal("Online", saved.PreferredLocation);
        Assert.Equal("Interested in details.", saved.Message);
        Assert.Equal(1, saved.CourseId);

        Assert.Equal(saved.Id, response.Id);
        Assert.Equal("Ada", response.FirstName);
        Assert.Equal("ada@example.com", response.Email);
    }

    [Fact]
    public async Task CreateAsync_SetsStatusNewAndEqualTimestamps()
    {
        await _service.CreateAsync(ValidRequest(), CancellationToken.None);

        var saved = await _db.Inquiries.AsNoTracking().SingleAsync();
        Assert.Equal(InquiryStatus.New, saved.Status);
        Assert.Equal(saved.CreatedDate, saved.UpdatedDate);
    }

    [Fact]
    public async Task CreateAsync_WritesPendingCrmSyncLog()
    {
        var response = await _service.CreateAsync(ValidRequest(), CancellationToken.None);

        var log = await _db.CrmSyncLogs.AsNoTracking().SingleAsync();
        Assert.Equal(response.Id, log.InquiryId);
        Assert.Equal(CrmSyncStatus.Pending, log.Status);
    }

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_NullStatus_ExcludesArchived()
    {
        SeedInquiry(InquiryStatus.New, "active-1");
        SeedInquiry(InquiryStatus.Contacted, "active-2");
        SeedInquiry(InquiryStatus.Archived, "archived");

        var results = await _service.GetAllAsync(null, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.DoesNotContain(results, r => r.FirstName == "archived");
    }

    [Fact]
    public async Task GetAllAsync_SpecificStatus_ReturnsOnlyThatStatusIncludingArchived()
    {
        SeedInquiry(InquiryStatus.New, "n");
        SeedInquiry(InquiryStatus.Archived, "a1");
        SeedInquiry(InquiryStatus.Archived, "a2");

        var results = await _service.GetAllAsync(InquiryStatus.Archived, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(nameof(InquiryStatus.Archived), r.Status));
    }

    [Fact]
    public async Task GetAllAsync_NoRows_ReturnsEmptyList()
    {
        var results = await _service.GetAllAsync(null, CancellationToken.None);

        Assert.NotNull(results);
        Assert.Empty(results);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_MissingId_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsMatchingResponse()
    {
        var seeded = SeedInquiry(InquiryStatus.New, "Grace");

        var result = await _service.GetByIdAsync(seeded.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(seeded.Id, result!.Id);
        Assert.Equal("Grace", result.FirstName);
        Assert.Equal(nameof(InquiryStatus.New), result.Status);
    }

    // ---------- UpdateStatusAsync ----------

    [Fact]
    public async Task UpdateStatusAsync_MissingId_ReturnsNullAndWritesNothing()
    {
        var result = await _service.UpdateStatusAsync(999, InquiryStatus.Contacted, CancellationToken.None);

        Assert.Null(result);
        Assert.Empty(await _db.Inquiries.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task UpdateStatusAsync_BumpsStatusAndUpdatedDate()
    {
        var created = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var seeded = SeedInquiry(InquiryStatus.New, "A", created);

        var result = await _service.UpdateStatusAsync(seeded.Id, InquiryStatus.Registered, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(nameof(InquiryStatus.Registered), result!.Status);

        var reloaded = await _db.Inquiries.AsNoTracking().SingleAsync(i => i.Id == seeded.Id);
        Assert.Equal(InquiryStatus.Registered, reloaded.Status);
        Assert.True(reloaded.UpdatedDate > reloaded.CreatedDate);
    }

    // ---------- ArchiveAsync ----------

    [Fact]
    public async Task ArchiveAsync_MissingId_ReturnsFalse()
    {
        var ok = await _service.ArchiveAsync(999, CancellationToken.None);
        Assert.False(ok);
    }

    [Fact]
    public async Task ArchiveAsync_SetsArchivedAndBumpsUpdatedDate()
    {
        var created = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var seeded = SeedInquiry(InquiryStatus.New, "A", created);

        var ok = await _service.ArchiveAsync(seeded.Id, CancellationToken.None);

        Assert.True(ok);
        var reloaded = await _db.Inquiries.AsNoTracking().SingleAsync(i => i.Id == seeded.Id);
        Assert.Equal(InquiryStatus.Archived, reloaded.Status);
        Assert.True(reloaded.UpdatedDate > reloaded.CreatedDate);
    }
}
