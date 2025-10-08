using Microsoft.AspNetCore.Identity;

namespace BusTrips.Domain.Entities;

public class AppUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? SecondaryEmail { get; set; }
    public string? PhoneNumber2 { get; set; }
    public string? PhotoUrl { get; set; }
    public bool AcceptedUserTerms { get; set; }
    public bool IsActive { get; set; } = true;
    public string? DeActiveDiscription { get; set; }
    public bool IsFirstLogin { get; set; } = true;
    public bool WizardCompleted { get; set; } = false;

    public ICollection<OrganizationMembership> OrganizationMemberships { get; set; } = new List<OrganizationMembership>();
    public ICollection<TripMembership> TripMemberships { get; set; } = new List<TripMembership>();
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class BusDriver : BaseEntity
{
    public Guid Id { get; set; } // equals AppUserId
    public Guid AppUserId { get; set; }
    public AppUser AppUser { get; set; } = default!;

    public string DriverId { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? TerminationDate { get; set; }

    public bool IsEmployee { get; set; } = true; // employee vs contractor
    public string EmploymentType { get; set; } = "FullTime"; // FullTime/PartTime/Casual

    public string LicenseNumber { get; set; } = string.Empty;
    public string LicenseProvince { get; set; } = string.Empty;

    public string? LicenseFrontUrl { get; set; }
    public string? LicenseBackUrl { get; set; }
    public string? DriverAbstractUrl { get; set; }

    public ICollection<DriverDocument> AdditionalDocuments { get; set; } = new List<DriverDocument>();

    public DriverApprovalStatus ApprovalStatus { get; set; } = DriverApprovalStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum DriverApprovalStatus { Pending, Approved, Rejected }

public class DriverDocument : BaseEntity
{
    public Guid Id { get; set; }
    public Guid BusDriverId { get; set; }
    public BusDriver BusDriver { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Url { get; set; } = default!;
    public bool IsDeleted { get; set; } = false;
}


public class BaseEntity
{

    public bool IsDeleted { get; set; } = false;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class TermsAndConditions : BaseEntity
{
    public Guid Id { get; set; }
    public string TermsFor { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
}