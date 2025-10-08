using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BusTrips.Domain.Entities;
[Index(nameof(OrgName), IsUnique = false)]
public class Organization : BaseEntity
{
    public Guid Id { get; set; }
    public string? OrgName { get; set; }
    public string ShortName { get; set; }
    public bool IsActive { get; set; } = true;
    public string? DeActiveDiscription { get; set; }
    public bool IsPrimary { get; set; } = false;

    public ICollection<OrganizationMembership> Managers { get; set; } = new List<OrganizationMembership>();
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    public ICollection<Group> Groups { get; set; } = new List<Group>();
}

public class OrganizationMembership : BaseEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;
    public Guid AppUserId { get; set; }
    public AppUser AppUser { get; set; } = default!;
    public MemberTypeEnum MemberType { get; set; } = MemberTypeEnum.ReadOnly;
    public bool IsInvited { get; set; } = true;
}

public enum MemberTypeEnum
{
    [Display(Name = "Creator")]
    Creator,

    [Display(Name = "Admin")]
    Admin,

    [Display(Name = "Member")]
    Member,

    [Display(Name = "Read Only")]
    ReadOnly
}

public class Group : BaseEntity
{
    public Guid Id { get; set; }
    public string GroupName { get; set; }
    public string ShortName { get; set; }
    public string? Description { get; set; }
    public Guid OrgId { get; set; }
    public Organization Org { get; set; }
    public bool IsActive { get; set; } = true;
    public string? DeActiveDiscription { get; set; }
}

public class OrganizationPermissions
{
    [Key]
    public Guid PId { get; set; }
    public Guid OrgId { get; set; }
    public Organization Org { get; set; }
    public MemberTypeEnum MemberType { get; set; }
    public bool IsView { get; set; }
    public bool IsCreate { get; set; }
    public bool IsEdit { get; set; }
    public bool IsDeactive { get; set; }
}

public class OrgCreatorLog : BaseEntity
{
    [Key]
    public int Id { get; set; }
    public Guid AppUserId { get; set; }
    public AppUser AppUser { get; set; }
    public Guid OrgId { get; set; }
    public Organization Org { get; set; }
    public bool IsPrimary { get; set; } = false;
    public bool IsStillCreator { get; set; } = true;
}

