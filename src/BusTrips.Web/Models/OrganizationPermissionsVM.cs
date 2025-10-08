using BusTrips.Domain.Entities;

namespace BusTrips.Web.Models
{
    public class PermissionRequestVM
    {
        public Guid OrgId { get; set; }           // Required to know which org this permission belongs to
        public string MemberType { get; set; }    // User role type (e.g., Admin, Creator, Member, etc.)

        public bool? IsView { get; set; }
        public bool? IsCreate { get; set; }
        public bool? IsEdit { get; set; }
        public bool? IsDeactive { get; set; }
    }

    public class PermissionResponseVM
    {
        public Guid PId { get; set; }
        public Guid OrgId { get; set; }
        public string OrgName { get; set; }
        public string MemberType { get; set; }
        public bool IsView { get; set; }
        public bool IsCreate { get; set; }
        public bool IsEdit { get; set; }
        public bool IsDeactive { get; set; }
    }
    public class PermissionPageVM
    {
        public Guid OrgId { get; set; }
        public string OrgName { get; set; }
        public OrganizationLists Organizations { get; set; } // dropdown (MyOrg + InvitedOrg)
        public List<PermissionResponseVM> Permissions { get; set; } // permissions table
    }
   
}
