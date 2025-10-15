using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTrips.Domain.Entities;

public class Organization : BaseEntity
{
    [Key] public Guid Id { get; set; }
    public string? OrgName { get; set; }
    [Required] public string ShortName { get; set; }
    public bool IsActive { get; set; } = true;
    public string? DeActiveDiscription { get; set; }
    public bool IsPrimary { get; set; } = false;

    [Required]
    public Guid CreatedForId { get; set; }
    [ForeignKey("CreatedForId")]
    public AppUser CreatedForUser { get; set; }

    public ICollection<OrganizationMembership> Members { get; set; } = new List<OrganizationMembership>();
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    public ICollection<Group> Groups { get; set; } = new List<Group>();
}

public class OrganizationMembership : BaseEntity
{
    [Key] public Guid Id { get; set; }

    [Required]
    public Guid OrganizationId { get; set; }
    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; }

    [Required]
    public Guid AppUserId { get; set; }
    [ForeignKey("AppUserId")]
    public AppUser AppUser { get; set; }

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
    [ForeignKey("OrgId")]
    public Organization Org { get; set; }
    public bool IsActive { get; set; } = true;
    public string? DeActiveDiscription { get; set; }
    public Guid CreatedForId { get; set; }
    public AppUser CreatedForUser { get; set; }
}

public class OrganizationPermissions
{
    [Key]
    public Guid PId { get; set; }
    public Guid OrgId { get; set; }
    [ForeignKey("OrgId")]
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
    [ForeignKey("AppUserId")]
    public AppUser AppUser { get; set; }
    public Guid OrgId { get; set; }
    [ForeignKey("OrgId")]
    public Organization Org { get; set; }
    public bool IsPrimary { get; set; } = false;
    public bool IsStillCreator { get; set; } = true;
}

