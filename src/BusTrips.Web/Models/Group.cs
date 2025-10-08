using BusTrips.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace BusTrips.Web.Models
{
    public class GroupRequestVM
    {
        public Guid? Id { get; set; }
        public Guid? OrgId { get; set; }
        [Required(ErrorMessage = "Group Name is required.")]
        [Display(Name = "Group Name")]
        public string GroupName { get; set; }
        [Required(ErrorMessage = "Short Name is required.")]
        [Display(Name = "Short Name")]
        public string ShortName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public string? DeActiveDiscription { get; set; }
    }

    public class GroupResponseVM
    {
        public Guid Id { get; set; }
        public string GroupName { get; set; }
        public string ShortName { get; set; }
        public string Description { get; set; }
        public Guid? OrgId { get; set; }
        public string? OrgName { get; set; }
        public string? DeActiveDiscription { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
    public class GroupListItemVm
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string GroupName { get; set; }
        public string Description { get; set; }
        public string ShortName { get; set; }
        public string Status { get; set; }
        public bool IsCreator { get; set; }
        public int TripsCount { get; set; }
    }
}
