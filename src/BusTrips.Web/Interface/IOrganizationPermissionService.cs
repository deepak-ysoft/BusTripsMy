using BusTrips.Domain.Entities;
using BusTrips.Web.Models;

namespace BusTrips.Web.Interface
{
    public interface IOrganizationPermissionService
    {
        Task<PermissionResponseVM> GetOrgPermissionAsync(Guid orgId, MemberTypeEnum mt);
        Task<List<PermissionResponseVM>> GetPermissionsAsync(Guid orgId);
        Task<PermissionResponseVM> CreatePermissionAsync(PermissionRequestVM request);
        Task<PermissionResponseVM> UpdatePermissionAsync(Guid pid, PermissionRequestVM request);

        // When creating a new Organization
        Task CreateDefaultPermissionsAsync(Guid orgId);
    }
}
