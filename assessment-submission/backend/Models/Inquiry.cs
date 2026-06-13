namespace CourseInquiryApi.Models;

public class Inquiry
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int CourseId { get; set; }
    public string? PreferredLocation { get; set; }
    public string? Message { get; set; }

    public InquiryStatus Status { get; set; } = InquiryStatus.New;

    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
