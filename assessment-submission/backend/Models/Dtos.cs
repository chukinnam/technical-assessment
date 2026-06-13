using System.ComponentModel.DataAnnotations;

namespace CourseInquiryApi.Models;

/// <summary>Incoming payload to create an inquiry. Id, Status, and dates are server-controlled.</summary>
public class CreateInquiryRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required,Phone, MaxLength(50)]
    public string Phone { get; set; }

    [Required]
    public int CourseId { get; set; }

    [MaxLength(200)]
    public string? PreferredLocation { get; set; }

    [MaxLength(2000)]
    public string? Message { get; set; }
}

/// <summary>Incoming payload to update an inquiry's status.</summary>
public class UpdateStatusRequest
{
    [Required]
    public InquiryStatus? Status { get; set; }
}

/// <summary>Outgoing representation of an inquiry (Status serialized as its name).</summary>
public class InquiryResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int CourseId { get; set; } 
    public string? PreferredLocation { get; set; }
    public string? Message { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }

    public static InquiryResponse From(Inquiry i) => new()
    {
        Id = i.Id,
        FirstName = i.FirstName,
        LastName = i.LastName,
        Email = i.Email,
        Phone = i.Phone,
        CourseId = i.CourseId,
        PreferredLocation = i.PreferredLocation,
        Message = i.Message,
        Status = i.Status.ToString(),
        CreatedDate = i.CreatedDate,
        UpdatedDate = i.UpdatedDate
    };
}
