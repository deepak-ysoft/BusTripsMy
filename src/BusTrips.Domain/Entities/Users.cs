using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTrips.Domain.Entities;

public class AppUser : IdentityUser<Guid>
{
    [Required] public string FirstName { get; set; }
    [Required] public string LastName { get; set; }
    public string? SecondaryEmail { get; set; }
    public string? PhoneNumber2 { get; set; }
    public string? PhotoUrl { get; set; }
    public bool AcceptedUserTerms { get; set; }
    public bool IsActive { get; set; } = true;
    public string? DeActiveDiscription { get; set; }
    public bool IsFirstLogin { get; set; } = true;
    public bool WizardCompleted { get; set; } = false;
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relationships
    public ICollection<OrganizationMembership> OrganizationMemberships { get; set; } = new List<OrganizationMembership>();
}


public class BusDriver : BaseEntity
{
    [Key]
    public Guid Id { get; set; } // Primary key = AppUserId
    [Required]
    public Guid AppUserId { get; set; }
    [ForeignKey("AppUserId")]
    public AppUser AppUser { get; set; }

    public string DriverId { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? TerminationDate { get; set; }

    public bool IsEmployee { get; set; } = true;
    public string EmploymentType { get; set; } = "FullTime";
    public string LicenseNumber { get; set; } = string.Empty;
    public string LicenseProvince { get; set; } = string.Empty;
    public string? LicenseFrontUrl { get; set; }
    public string? LicenseBackUrl { get; set; }
    public string? DriverAbstractUrl { get; set; }

    public DriverApprovalStatus ApprovalStatus { get; set; } = DriverApprovalStatus.Pending;

    public ICollection<DriverDocument> AdditionalDocuments { get; set; } = new List<DriverDocument>();
}

public class DriverDocument : BaseEntity
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    public Guid BusDriverId { get; set; }
    [ForeignKey("BusDriverId")]
    public BusDriver BusDriver { get; set; }

    [Required]
    public string Name { get; set; }
    [Required]
    public string Url { get; set; }
}

public enum DriverApprovalStatus { Pending, Approved, Rejected }


public abstract class BaseEntity
{
    public bool IsDeleted { get; set; } = false;

    public Guid? CreatedBy { get; set; }

    [NotMapped]
    public AppUser? CreatedByUser { get; set; }

    public Guid UpdatedBy { get; set; }

    [NotMapped]
    public AppUser UpdatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}


public class TermsAndConditions : BaseEntity
{
    public Guid Id { get; set; }
    public string TermsFor { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
}