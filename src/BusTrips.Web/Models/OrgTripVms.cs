using BusTrips.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace BusTrips.Web.Models;

public class OrgListItemVm
{
    public Guid Id { get; set; }
    public bool IsInvited { get; set; } = false;
    public string? OrgName { get; set; }
    public string? ShortName { get; set; }
    public int? tripCount { get; set; }
    public int? groupCount { get; set; }
    public string MemberType { get; set; }
    public string CreatorName { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; }
    public string? DeActiveDiscription { get; set; }
    public List<GroupListItemVm> Groups { get; set; } = new();
    public PermissionResponseVM? Permissions { get; set; }
}

public class OrganizationLists
{
    public Guid UserId { get; set; }
    public List<OrgListItemVm>? MyOrg { get; set; }
    public List<OrgListItemVm>? InvitedOrg { get; set; }
}


public class OrgMembersListVM
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid AppUserId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string MemberType { get; set; }
    public PermissionResponseVM Permissions { get; set; } = new();
}

public class OrgDetailsResponseVM
{
    public OrgListItemVm? Org { get; set; }
    public List<OrgMembersListVM>? Members { get; set; }
    public List<OrgMembersListVM>? Admins { get; set; }
}

public class GroupDetailsResponseVM
{
    public OrgListItemVm Org { get; set; }
    public Guid? userId { get; set; }
    public GroupResponseVM Group { get; set; }
    public List<TripListItemVm> Trips { get; set; }

}

public class OrgMemberDetailsResponseVM
{
    public OrgListItemVm? Org { get; set; }
    public OrgMembersListVM? Member { get; set; }
}

public class CreateOrganizationVm
{
    public Guid? Id { get; set; } = default!;
    public Guid? userId { get; set; }

    [Display(Name ="Organization Name")]
    [Required(ErrorMessage = "Organization Name is required.")] 
    public string OrgName { get; set; }

    [Display(Name = "Short Name")]
    [Required(ErrorMessage = "Short Name is required.")] 
    public string ShortName { get; set; }

    public bool? IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public string? DeActiveDiscription { get; set; }
}

public class InviteManagerVm
{
    public Guid OrganizationId { get; set; }
    [RegularExpression("^[a-zA-Z0-9._%+-]+@[a-zA-Z.-]+\\.[a-zA-Z]{2,}$", ErrorMessage = "Please enter a valid email address.")]
    [Required, EmailAddress] public string Email { get; set; } = default!;
}
public class TripListVm
{
    public Guid Id { get; set; }
    public string? TripName { get; set; }
    public string? UserName { get; set; }
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string? OrganizationName { get; set; }
    public string? OrganizationCreatorName { get; set; }
    public string? Status { get; set; }
    public DateOnly? DepartureDate { get; set; }
    public TimeOnly? DepartureTime { get; set; }
    public string? StartLocation { get; set; }
    public TimeOnly? DestinationArrivalTime { get; set; }
    public DateOnly? DestinationArrivalDate { get; set; }
    public string? DestinationLocation { get; set; }
    public DriverDetailsVM? Driver { get; set; }
    public EquipmentDetailsVM? Equipment { get; set; }
    public int? Passengers { get; set; }
    public int? TripDays { get; set; }
    public string? Notes { get; set; }
}

public class DriverDetailsVM
{
    public string? DriverId { get; set; }
    public string? DriverName { get; set; }
    public string? EmploymentType { get; set; }
    public string? LicenseNumber { get; set; }
    public string? LicenseProvince { get; set; }
}
public class EquipmentDetailsVM
{
   // equipment
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? LicensePlate { get; set; }
    public string? SeatingCapacity { get; set; }
    public string? Year { get; set; }
}

public class TripListItemVm
{
    public Guid Id { get; set; }
    public string? TripName { get; set; }
    public string? UserName { get; set; }
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string? OrganizationName { get; set; }
    public string? OrganizationCreatorName { get; set; }
    public string? Status { get; set; }
    public string? DepartureDate { get; set; }
    public string? DepartureTime { get; set; }
    public string? DestinationArrivalTime { get; set; }
    public string? DestinationArrivalDate { get; set; }
    public string? Driver { get; set; }
    public string? BusNumber { get; set; }
}

// TripFilterVM.cs
public class TripFilterVM
{
    public Guid? OrganizationId { get; set; }
    public string? Search { get; set; }
    public DateTime? TripDate { get; set; }
    public string? Status { get; set; }

    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class TripDetailsResponseVm
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string TripName { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string DepartureDate { get; set; } = default!;
    public string DepartureTime { get; set; } = default!;
    public string StartCity { get; set; } = default!;
    public string StartLocation { get; set; } = default!;
    public int? NumberOfPassengers { get; set; }
    public string DestinationCity { get; set; } = default!;
    public string DestinationLocation { get; set; } = default!;
    public string DestinationArrivalDate { get; set; } = default!;
    public string DestinationArrivalTime { get; set; } = default!;
    public string? Notes { get; set; }

    public int? QuotedPrice { get; set; }
    public string? EstimateLinkUrl { get; set; }
    public string? InvoiceLinkUrl { get; set; }
    public string? TripCreatedAt { get; set; }

    //Org
    public Guid? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public string? OrgShortName { get; set; }
    public bool IsPrimary { get; set; } = false;

    // Driver
    public string? DriverId { get; set; }
    public string? DriverName { get; set; }
    public string? EmploymentType { get; set; }
    public string? LicenseNumber { get; set; }
    public string? LicenseProvince { get; set; }

    // equipment
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? LicensePlate { get; set; }
    public string? SeatingCapacity { get; set; }
    public string? Year { get; set; }

    // Group 
    public Guid? GourpId { get; set; } 
    public bool IsActive { get; set; } = false;
    public string? GroupName { get; set; }
    public string? ShortName { get; set; }
    public string? Creator { get; set; }
    public string? CreatedAt { get; set; }
}

public class CreateTripVm
{
    public Guid? Id { get; set; }
    public Guid? userId { get; set; }
    [Display(Name = "Trip Name")]
    [Required] public string TripName { get; set; }
    [Display(Name = "Organization Id")]
    public Guid? OrganizationId { get; set; }
    [Display(Name = "Group Id")]
    public Guid? groupId { get; set; }
    [Display(Name = "Departure Date")]
    [Required] public DateOnly DepartureDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    [Display(Name = "Departure Time")]
    [Required] public TimeOnly DepartureTime { get; set; } = TimeOnly.FromDateTime(DateTime.Now);
    [Display(Name = "Start City")]
    [Required] public string StartCity { get; set; } = default!;
    [Display(Name = "Start Location")]
    [Required] public string StartLocation { get; set; } = default!;
    [Display(Name = "Number Of Passengers")]
    [Required] public int? NumberOfPassengers { get; set; }
    [Display(Name = "Destination City")]
    [Required] public string DestinationCity { get; set; } = default!;
    [Display(Name = "Destination Location")]
    [Required] public string DestinationLocation { get; set; } = default!;
    [Display(Name = "Total Trip Days")]
    [Required]public int? TripDays { get; set; }
    [Display(Name = "Destination Arrival Date")]
    [Required] public DateOnly DestinationArrivalDate { get; set; } = DateOnly.FromDateTime(DateTime.Now).AddDays(1);
    [Display(Name = "Destination Arrival Time")]
    [Required] public TimeOnly DestinationArrivalTime { get; set; } = TimeOnly.FromDateTime(DateTime.Now).AddHours(2);
    public string? Notes { get; set; }
    public bool SaveAsDraft { get; set; } = false;
    public string? returnUrl { get; set; }
    public string? controller { get; set; }
}

public class OrgDetailsVm
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; } 
    public string OrgName { get; set; }
    public string ShortName { get; set; }
    public string? MemberType { get; set; }
    public bool IsActive { get; set; }
    public string? DeActiveDiscription { get; set; }
    public bool IsPrimary { get; set; }

    public CreatorVm Creator { get; set; } = default!;
    //public List<GroupListItemVm> Groups { get; set; } = new();
    public List<PermissionResponseVM> Permissions { get; set; } = new();
    //public List<OrgMembersListVM>? Members { get; set; } = new();
}

public class CreatorVm
{
    public Guid Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Number { get; set; }
}

