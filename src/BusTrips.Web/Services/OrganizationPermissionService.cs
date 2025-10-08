using BusTrips.Domain.Entities;
using BusTrips.Infrastructure.Persistence;
using BusTrips.Web.Interface;
using BusTrips.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTrips.Web.Services
{
    public class OrganizationPermissionService : IOrganizationPermissionService
    {
        private readonly AppDbContext _db;

        public OrganizationPermissionService(AppDbContext db)
        {
            _db = db;
        }

        // Get permissions for a specific organization and member type
        public async Task<PermissionResponseVM> GetOrgPermissionAsync(Guid orgId, MemberTypeEnum mt)
        {
            if (!await _db.OrganizationPermissions.AnyAsync(x => x.OrgId == orgId))
            {
                await CreateDefaultPermissionsAsync(orgId);
            }
            return await _db.OrganizationPermissions
                .Where(p => p.OrgId == orgId && p.MemberType == mt).OrderBy(x => x.MemberType)
                .Select(p => new PermissionResponseVM
                {
                    PId = p.PId,
                    OrgId = p.OrgId,
                    OrgName = p.Org.OrgName,
                    MemberType = p.MemberType.ToString(),
                    IsView = p.IsView,
                    IsCreate = p.IsCreate,
                    IsEdit = p.IsEdit,
                    IsDeactive = p.IsDeactive
                }).FirstOrDefaultAsync() ?? new PermissionResponseVM();
        }

        // Get all permissions for a specific organization (excluding Creator type)
        public async Task<List<PermissionResponseVM>> GetPermissionsAsync(Guid orgId)
        {
            if (!await _db.OrganizationPermissions.AnyAsync(x => x.OrgId == orgId))
            {
                await CreateDefaultPermissionsAsync(orgId);
            }

            return await _db.OrganizationPermissions
                .Where(p => p.OrgId == orgId && p.MemberType != MemberTypeEnum.Creator).OrderBy(x => x.MemberType)
                .Select(p => new PermissionResponseVM
                {
                    PId = p.PId,
                    OrgId = p.OrgId,
                    OrgName = p.Org.OrgName,
                    MemberType = p.MemberType.ToString(),
                    IsView = p.IsView,
                    IsCreate = p.IsCreate,
                    IsEdit = p.IsEdit,
                    IsDeactive = p.IsDeactive
                }).ToListAsync();
        }

        // Create a new permission entry
        public async Task<PermissionResponseVM> CreatePermissionAsync(PermissionRequestVM request)
        {
            var entity = new OrganizationPermissions
            {
                PId = Guid.NewGuid(),
                OrgId = request.OrgId,
                MemberType = Enum.TryParse<MemberTypeEnum>(request.MemberType, out var memberType) ? memberType : throw new ArgumentException("Invalid MemberType value"),
                IsView = request.IsView ?? true,
                IsCreate = request.IsCreate ?? true,
                IsEdit = request.IsEdit ?? true,
                IsDeactive = request.IsDeactive ?? true
            };

            _db.OrganizationPermissions.Add(entity);
            await _db.SaveChangesAsync();

            return new PermissionResponseVM
            {
                PId = entity.PId,
                OrgId = entity.OrgId,
                OrgName = (await _db.Organizations.FindAsync(entity.OrgId))?.OrgName,
                MemberType = entity.MemberType.ToString(),
                IsView = entity.IsView,
                IsCreate = entity.IsCreate,
                IsEdit = entity.IsEdit,
                IsDeactive = entity.IsDeactive
            };
        }

        // Update an existing permission entry
        public async Task<PermissionResponseVM?> UpdatePermissionAsync(Guid pid, PermissionRequestVM request)
        {
            var permission = await _db.OrganizationPermissions.Include(x => x.Org).FirstOrDefaultAsync(x => x.PId == pid);
            if (permission == null) return null;

            if (request.IsView.HasValue) permission.IsView = request.IsView.Value;
            if (request.IsCreate.HasValue) permission.IsCreate = request.IsCreate.Value;
            if (request.IsEdit.HasValue) permission.IsEdit = request.IsEdit.Value;
            if (request.IsDeactive.HasValue) permission.IsDeactive = request.IsDeactive.Value;

            await _db.SaveChangesAsync();

            return new PermissionResponseVM
            {
                PId = permission.PId,
                OrgId = permission.OrgId,
                OrgName = permission.Org.OrgName,
                MemberType = permission.MemberType.ToString(),
                IsView = permission.IsView,
                IsCreate = permission.IsCreate,
                IsEdit = permission.IsEdit,
                IsDeactive = permission.IsDeactive
            };
        }

        // Create default permissions for all member types when a new organization is created
        public async Task CreateDefaultPermissionsAsync(Guid orgId)
        {
            var defaults = Enum.GetValues(typeof(MemberTypeEnum))
                .Cast<MemberTypeEnum>()
                .Select(mt => new OrganizationPermissions
                {
                    PId = Guid.NewGuid(),
                    OrgId = orgId,
                    MemberType = mt,
                    IsView = true,
                    IsCreate = true,
                    IsEdit = true,
                    IsDeactive = true
                }).ToList();

            _db.OrganizationPermissions.AddRange(defaults);
            await _db.SaveChangesAsync();
        }
    }
}
