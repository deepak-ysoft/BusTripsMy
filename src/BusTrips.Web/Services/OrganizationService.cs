using BusTrips.Domain.Entities;
using BusTrips.Infrastructure.Persistence;
using BusTrips.Web.Interface;
using BusTrips.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Cryptography;

namespace BusTrips.Web.Services
{
    public class OrganizationService : IOrganizationService
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _users;
        private readonly IOrganizationPermissionService _permissionService;
        public OrganizationService(AppDbContext db, UserManager<AppUser> users, IOrganizationPermissionService permissionService)
        {
            _db = db;
            _users = users;
            _permissionService = permissionService;
        }

        #region Org

        // Get all organizations for the current user, including created and invited ones
        public async Task<OrganizationLists> GetUserOrganizationsAsync(Guid CurrentUserId)
        {
            // Fetch all user's organizations (created or creator)
            var myOrganizations = await _db.OrganizationMemberships
                .Include(m => m.Organization)
                .Where(m => m.AppUserId == CurrentUserId && !m.Organization.IsDeleted && !m.IsDeleted && !m.IsInvited).OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            // Ensure only one primary organization exists
            var primaryOrgs = myOrganizations
                .Select(m => m.Organization)
                .Where(o => o.IsPrimary)
                .ToList();

            if (primaryOrgs.Count > 1)
            {
                // Keep the one created by current user as primary, set others to false
                foreach (var org in primaryOrgs)
                {
                    org.IsPrimary = org.CreatedBy == CurrentUserId;
                }
                await _db.SaveChangesAsync();
            }

            var MyOrg = new List<OrgListItemVm>();
            foreach (var m in myOrganizations)
            {
                var permission = await _permissionService.GetOrgPermissionAsync(m.OrganizationId, m.MemberType);

                var creatorName = await _db.Users
                    .Where(u => u.Id == m.Organization.CreatedBy)
                    .Select(u => u.FirstName + " " + u.LastName)
                    .FirstOrDefaultAsync();

                var groups = await _db.Groups
                    .Where(g => g.OrgId == m.OrganizationId && !g.IsDeleted)
                    .Select(s => new GroupListItemVm
                    {
                        Id = s.Id,
                        GroupName = s.GroupName,
                        Description = s.Description,
                        ShortName = s.ShortName,
                        Status = s.IsActive ? "Active" : "Inactive",
                        IsCreator = s.CreatedBy == CurrentUserId,
                        TripsCount = _db.Trips.Count(t => t.GroupId == s.Id && !t.IsDeleted)
                    }).ToListAsync();

                var item = new OrgListItemVm
                {
                    Id = m.OrganizationId,
                    IsInvited = false,
                    OrgName = m.Organization.OrgName,
                    ShortName = m.Organization.ShortName,
                    tripCount = await _db.Trips.CountAsync(t => t.OrganizationId == m.OrganizationId && !t.IsDeleted),
                    Permissions = permission,
                    MemberType = m.MemberType.ToString(),
                    CreatorName = creatorName,
                    CreatedDate = m.Organization.CreatedAt,
                    IsActive = m.Organization.IsActive,
                    IsPrimary = m.Organization.IsPrimary,
                    Groups = groups
                };

                MyOrg.Add(item);
            }

            // Fetch invited organizations
            var invitedMemberships = await _db.OrganizationMemberships
                .Include(x => x.Organization)
                .Where(m => m.AppUserId == CurrentUserId && !m.Organization.IsDeleted && !m.IsDeleted && m.IsInvited).OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var InvitedOrg = new List<OrgListItemVm>();
            foreach (var m in invitedMemberships)
            {
                var permission = await _permissionService.GetOrgPermissionAsync(m.OrganizationId, m.MemberType);
                var creatorName = await _db.Users
                    .Where(u => u.Id == m.Organization.CreatedBy)
                    .Select(u => u.FirstName + " " + u.LastName)
                    .FirstOrDefaultAsync();

                var groups = await _db.Groups
                    .Where(g => g.OrgId == m.OrganizationId && !g.IsDeleted)
                    .Select(s => new GroupListItemVm
                    {
                        Id = s.Id,
                        GroupName = s.GroupName,
                        Description = s.Description,
                        ShortName = s.ShortName,
                        Status = s.IsActive ? "Active" : "Inactive",
                        IsCreator = s.CreatedBy == CurrentUserId,
                        TripsCount = _db.Trips.Count(t => t.GroupId == s.Id && !t.IsDeleted)
                    }).ToListAsync();

                var item = new OrgListItemVm
                {
                    Id = m.OrganizationId,
                    IsInvited = true,
                    OrgName = m.Organization.OrgName,
                    ShortName = m.Organization.ShortName,
                    tripCount = await _db.Trips.CountAsync(t => t.OrganizationId == m.OrganizationId && !t.IsDeleted),
                    Permissions = permission,
                    MemberType = m.MemberType.ToString(),
                    CreatorName = creatorName,
                    CreatedDate = m.Organization.CreatedAt,
                    IsActive = m.Organization.IsActive,
                    IsPrimary = m.Organization.IsPrimary,
                    Groups = groups
                };

                InvitedOrg.Add(item);
            }

            return new OrganizationLists { MyOrg = MyOrg, InvitedOrg = InvitedOrg, UserId = CurrentUserId };
        }

        // Get the default organization created by the current user
        public async Task<CreateOrganizationVm> GetDefaultOrganizationByUserAsync(Guid CurrentUserId)
        {
            return await _db.Organizations
            .Where(m => m.CreatedBy == CurrentUserId && !m.IsDeleted)
            .Select(m => new CreateOrganizationVm
            {
                Id = m.Id,
                OrgName = m.OrgName,
                ShortName = m.ShortName,
                IsActive = m.IsActive,
                DeActiveDiscription = m.DeActiveDiscription,
                IsPrimary = m.IsPrimary,
            })
            .FirstOrDefaultAsync();
        }

        // Get organization details by ID
        public async Task<CreateOrganizationVm> GetOrganizationAsync(Guid id)
        {
            return await _db.Organizations
                .Where(m => !m.IsDeleted && m.Id == id)
                .Select(m => new CreateOrganizationVm
                {
                    Id = m.Id,
                    OrgName = m.OrgName,
                    ShortName = m.ShortName,
                    IsActive = m.IsActive,
                    DeActiveDiscription = m.DeActiveDiscription,
                    IsPrimary = m.IsPrimary,
                    userId = m.CreatedBy

                }).FirstOrDefaultAsync() ?? new CreateOrganizationVm();
        }

        // Create a default organization for the user if they don't have one
        public async Task<ResponseVM<string>> CreatorDefaultOrgAcync(Guid userId)
        {
            if (await _db.Organizations.AnyAsync(o => o.CreatedBy == userId && !o.IsDeleted)) { return new ResponseVM<string> { IsSuccess = false, Message = "You already have an organization." }; }
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var org = new Organization
            {
                OrgName = user.FirstName + "_" + user.LastName + "_Default",
                ShortName = UniqueCodeGenerator.GenerateShortName(),
                IsPrimary = true,
                CreatedForId = userId,
                CreatedBy = userId,
                UpdatedBy = userId,
            };

            _db.Organizations.Add(org);
            await _db.SaveChangesAsync();
            _db.OrganizationMemberships.Add(new OrganizationMembership
            {
                OrganizationId = org.Id,
                Organization = org,
                AppUserId = userId,
                IsInvited = false,
                CreatedBy = userId,
                UpdatedBy = userId,
                MemberType = MemberTypeEnum.Creator,
            });
            await _db.SaveChangesAsync();

            await _permissionService.CreateDefaultPermissionsAsync(org.Id); // Create default permissions for the new organization
            return new ResponseVM<string> { IsSuccess = true, Message = "Group Added!", Data = org.Id.ToString() };
        }

        // Generate a unique short name for the organization
        public static class UniqueCodeGenerator
        {
            private static Random _random = new Random();

            public static string GenerateShortName(int length = 5)
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                return new string(Enumerable.Repeat(chars, length)
                    .Select(s => s[_random.Next(s.Length)]).ToArray());
            }
        }

        // Add or edit an organization
        public async Task<ResponseVM<string>> AddEditOrganizationAsync(CreateOrganizationVm vm, Guid userId)
        {
            if (vm.Id == null)
            {
                if (await _db.Organizations.AnyAsync(o => o.OrgName == vm.OrgName && o.CreatedBy == userId && !o.IsDeleted)) { return new ResponseVM<string> { IsSuccess = false, Message = "Orgamization name must be unique." }; }

                if (vm.IsPrimary == true && await _db.Organizations.AnyAsync(o => o.IsPrimary && o.CreatedBy == userId && !o.IsDeleted))
                {
                    var primaryOrg = await _db.Organizations
                        .Where(o => o.IsPrimary && o.CreatedBy == userId && !o.IsDeleted)
                        .ToListAsync();

                    if (primaryOrg != null)
                    {
                        foreach (var org1 in primaryOrg)
                        {
                            org1.IsPrimary = false;
                            _db.Organizations.Update(org1);
                        }
                    }
                }

                var org = new Organization
                {
                    OrgName = vm.OrgName,
                    ShortName = vm.ShortName,
                    IsPrimary = vm.IsPrimary ?? true,
                    IsActive = true,
                    CreatedForId = userId,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                };
                _db.Organizations.Add(org);
                _db.OrganizationMemberships.Add(new OrganizationMembership
                {
                    Organization = org,
                    AppUserId = userId,
                    CreatedBy = userId,
                    IsInvited = false,
                    UpdatedBy = userId,
                    MemberType = MemberTypeEnum.Creator,
                });

                await _db.SaveChangesAsync();

                await _permissionService.CreateDefaultPermissionsAsync(org.Id); // Create default permissions for the new organization
                return new ResponseVM<string> { IsSuccess = true, Message = "Organization Added!", Data = org.Id.ToString() };
            }
            else
            {
                var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == vm.Id);
                if (org == null)
                    return new ResponseVM<string> { IsSuccess = false, Message = "Organization not found" };

                ;
                if (!vm.IsActive && (string.IsNullOrEmpty(vm.DeActiveDiscription)))
                    return new ResponseVM<string>
                    {
                        IsSuccess = false,
                        Message = "Please provide a reason for deactivating the organization."
                    };


                if (org.OrgName != vm.OrgName && await _db.Organizations.AnyAsync(o => o.OrgName == vm.OrgName && o.CreatedBy == userId && !o.IsDeleted))
                    return new ResponseVM<string> { IsSuccess = false, Message = "Orgamization name must be unique." };

                if (vm.IsPrimary == true && !org.IsPrimary && await _db.Organizations.AnyAsync(o => o.IsPrimary && o.CreatedBy == userId && !o.IsDeleted))
                {
                    var primaryOrg = await _db.Organizations
                        .Where(o => o.IsPrimary && o.CreatedBy == userId && !o.IsDeleted)
                        .ToListAsync();

                    if (primaryOrg != null)
                    {
                        foreach (var org1 in primaryOrg)
                        {
                            org1.IsPrimary = false;
                            _db.Organizations.Update(org1);
                        }
                    }
                }
                if (vm.IsPrimary == true && !vm.IsActive)
                    return new ResponseVM<string> { IsSuccess = false, Message = "Only active organizations can be set as primary." };

                if (vm.IsPrimary == false && org.IsPrimary && org.CreatedBy == userId)
                    return new ResponseVM<string> { IsSuccess = false, Message = "You must have at least one primary organization." };

                if (vm.IsActive == false && org.IsActive && org.IsPrimary)
                    return new ResponseVM<string> { IsSuccess = false, Message = "You can not deactive primary organization." };

                if (vm.IsActive == false && org.IsActive && !await _db.Organizations.AnyAsync(o => o.IsActive && o.Id != org.Id && o.CreatedBy == userId && !o.IsDeleted))
                    return new ResponseVM<string> { IsSuccess = false, Message = "You must have at least one active organization." };

                org.OrgName = vm.OrgName;
                org.ShortName = vm.ShortName;
                org.IsActive = vm.IsActive;
                org.DeActiveDiscription = vm.IsActive ? null : vm.DeActiveDiscription;
                org.IsPrimary = vm.IsPrimary ?? org.IsPrimary;
                org.UpdatedBy = userId;
                org.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                return new ResponseVM<string> { IsSuccess = true, Message = "Organization Updated!", Data = org.Id.ToString() };
            }
        }

        // Revert (undelete) an organization by ID
        public async Task<ResponseVM<string>> RevertOrganizationAsync(Guid? id)
        {
            if (id == null) return new ResponseVM<string> { IsSuccess = false, Message = "Organization Id is required" };
            var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
            if (org == null) return new ResponseVM<string> { IsSuccess = false, Message = "Organization not found" };
            _db.Organizations.Remove(org);
            await _db.SaveChangesAsync();
            return new ResponseVM<string> { IsSuccess = true, Message = "Organization Reverted Successfully!" };
        }

        // Invite a user to join an organization
        public async Task<ResponseVM<string>> InviteAsync(InviteManagerVm vm, Guid inviterId)
        {
            // 1. Find user by email
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == vm.Email);
            if (user is null)
                return new ResponseVM<string> { IsSuccess = false, Message = "User not found" };

            // 2. Ensure the organization exists
            var orgExists = await _db.Organizations.AnyAsync(o => o.Id == vm.OrganizationId);
            if (!orgExists)
                return new ResponseVM<string> { IsSuccess = false, Message = "Organization not found" };

            // 3. Check membership
            var existingMembership = await _db.OrganizationMemberships
                .FirstOrDefaultAsync(m => m.OrganizationId == vm.OrganizationId && m.AppUserId == user.Id);

            // 4. Validate user role
            var roles = await _users.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "User";

            if (userRole == "Driver")
                return new ResponseVM<string> { IsSuccess = false, Message = "You cannot invite a driver to an organization." };

            // 5. Handle membership cases
            if (existingMembership != null && !existingMembership.IsDeleted)
            {
                return new ResponseVM<string>
                {
                    IsSuccess = false,
                    Message = "This user is already a member of the organization."
                };
            }

            if (existingMembership != null && existingMembership.IsDeleted)
            {
                existingMembership.IsDeleted = false;
                existingMembership.MemberType = MemberTypeEnum.ReadOnly;
                existingMembership.UpdatedBy = inviterId;
            }
            else
            {
                _db.OrganizationMemberships.Add(new OrganizationMembership
                {
                    OrganizationId = vm.OrganizationId,
                    AppUserId = user.Id,
                    MemberType = MemberTypeEnum.ReadOnly,
                    IsInvited = true,
                    CreatedBy = inviterId,
                    UpdatedBy = inviterId,
                    IsDeleted = false
                });
            }

            await _db.SaveChangesAsync();

            return new ResponseVM<string> { IsSuccess = true, Message = "User invited successfully!" };
        }

        // Soft delete an organization by ID
        public async Task<ResponseVM<Organization>> DeleteOrganizationAsync(Guid id)
        {
            var org = await _db.Organizations
           .FirstOrDefaultAsync(o => o.Id == id);

            if (org is null) return new ResponseVM<Organization> { IsSuccess = false, Message = "Organization Not Found" };
            org.IsDeleted = true;
            _db.Organizations.Update(org);
            await _db.SaveChangesAsync();
            return new ResponseVM<Organization> { IsSuccess = true, Message = "Organization Deleted Successfully!", Data = org };
        }

        // Get detailed information about a specific organization, including its members and admins
        public async Task<dynamic> GetUserOrganizationDetailsAsync(Guid id, Guid CurrentUserId)
        {
            var Organization = await _db.OrganizationMemberships.Include(x => x.Organization)
            .Where(m => !m.IsDeleted && m.Organization.Id == id && !m.Organization.IsDeleted).FirstOrDefaultAsync();

            if (Organization == null)
            {
                return null;
            }

            var Members = await _db.OrganizationMemberships.Include(o => o.AppUser).Where(o => o.OrganizationId == id && !o.Organization.IsDeleted && !o.IsDeleted).ToListAsync();

            OrgDetailsResponseVM Org = new OrgDetailsResponseVM
            {
                Org = new OrgListItemVm
                {
                    Id = Organization.Organization.Id,
                    OrgName = Organization.Organization.OrgName,
                    ShortName = Organization.Organization.ShortName,
                    MemberType = Organization.MemberType.ToString(),
                    Permissions = await _permissionService.GetOrgPermissionAsync(Organization.OrganizationId, Organization.MemberType),
                    tripCount = _db.Trips.Count(t => t.OrganizationId == Organization.Organization.Id && !t.IsDeleted),
                    groupCount = _db.Groups.Count(g => g.OrgId == Organization.Organization.Id && !g.IsDeleted),
                    CreatedDate = Organization.Organization.CreatedAt,
                    IsActive = Organization.Organization.IsActive,
                    IsPrimary = Organization.Organization.IsPrimary,
                },
                Members = Members.Where(m => m.MemberType != MemberTypeEnum.Admin && m.MemberType != MemberTypeEnum.Creator).OrderByDescending(x => x.CreatedAt).Select(m => new OrgMembersListVM
                {
                    Id = m.Id,
                    OrganizationId = m.OrganizationId,
                    AppUserId = m.AppUserId,
                    UserName = m.AppUser?.FirstName + " " + m.AppUser?.LastName,
                    Email = m.AppUser?.Email,
                    PhoneNumber = m.AppUser?.PhoneNumber,
                    MemberType = m.MemberType.ToString(),
                }).ToList(),
                Admins = Members.Where(m => m.MemberType == MemberTypeEnum.Admin).OrderByDescending(x => x.CreatedAt).Select(m => new OrgMembersListVM
                {
                    Id = m.Id,
                    OrganizationId = m.OrganizationId,
                    AppUserId = m.AppUserId,
                    UserName = m.AppUser?.FirstName + " " + m.AppUser?.LastName,
                    Email = m.AppUser?.Email,
                    PhoneNumber = m.AppUser?.PhoneNumber,
                    MemberType = m.MemberType.ToString(),
                }).ToList()
            };

            return Org;
        }

        //public async Task<OrgMemberDetailsResponseVM> GetMemberDetailsAsync(Guid id, Guid orgId, Guid CurrentUserId)
        //{
        //    // First, get the org itself
        //    var org = await _db.Organizations
        //        .Where(o => o.Id == orgId && o.IsDeleted == false)
        //        .FirstOrDefaultAsync();

        //    if (org == null)
        //        return null;

        //    // Get the member being viewed
        //    var memberData = await _db.OrganizationMemberships
        //        .Include(o => o.AppUser)
        //        .Where(o => o.OrganizationId == orgId && o.AppUserId == id && !o.IsDeleted)
        //        .Select(m => new
        //        {
        //            m.Id,
        //            m.OrganizationId,
        //            m.AppUserId,
        //            UserName = m.AppUser.FirstName + " " + m.AppUser.LastName,
        //            m.AppUser.Email,
        //            m.AppUser.PhoneNumber,
        //            MemberType = m.MemberType,
        //        })
        //        .FirstOrDefaultAsync();

        //    if (memberData == null)
        //        return null;

        //    // Get the current user's membership
        //    var currentUserMembership = await _db.OrganizationMemberships
        //        .Where(m => m.OrganizationId == orgId && m.AppUserId == CurrentUserId && !m.IsDeleted)
        //        .FirstOrDefaultAsync();

        //    if (currentUserMembership == null)
        //        return null; // optional: handle case where current user isn’t part of the org

        //    // Get permissions for current user
        //    var permissions = await _permissionService.GetOrgPermissionAsync(orgId, currentUserMembership.MemberType);

        //    // Build the response
        //    var member = new OrgMembersListVM
        //    {
        //        Id = memberData.Id,
        //        OrganizationId = memberData.OrganizationId,
        //        AppUserId = memberData.AppUserId,
        //        UserName = memberData.UserName,
        //        Email = memberData.Email,
        //        PhoneNumber = memberData.PhoneNumber,
        //        MemberType = memberData.MemberType.ToString(),
        //        Permissions = permissions
        //    };

        //    var result = new OrgMemberDetailsResponseVM
        //    {
        //        Org = new OrgListItemVm
        //        {
        //            Id = org.Id,
        //            OrgName = org.OrgName,
        //            IsActive = org.IsActive,
        //            MemberType = currentUserMembership.MemberType.ToString(),
        //            Permissions = permissions
        //        },
        //        Member = member
        //    };

        //    return result;
        //}

        // Change the role of an organization member
        public async Task<ResponseVM<string>> ChangeOrgMemberRoleAsync(Guid targetUserId, Guid orgId, Guid currentUserId, MemberTypeEnum action)
        {
            var member = await _db.OrganizationMemberships
                .FirstOrDefaultAsync(o => o.AppUserId == targetUserId && o.OrganizationId == orgId && !o.IsDeleted);

            if (member is null)
                return new ResponseVM<string> { IsSuccess = false, Message = "Organization Member Not Found" };

            var currentMember = await _db.OrganizationMemberships
                .FirstOrDefaultAsync(o => o.AppUserId == currentUserId && o.OrganizationId == orgId && !o.IsDeleted);

            var currentUserCreatorOrAdminOrgs = await _db.OrganizationMemberships
                .Where(o => o.AppUserId == currentUserId && o.MemberType == MemberTypeEnum.Creator && !o.IsDeleted)
                .Select(x => x.OrganizationId)
                .ToListAsync();

            switch (action)
            {
                case MemberTypeEnum.Member:
                    if (member.MemberType == MemberTypeEnum.Creator)
                        return new ResponseVM<string> { IsSuccess = false, Message = "Cannot change role of Creator" };

                    if (member.MemberType == MemberTypeEnum.Admin && !currentUserCreatorOrAdminOrgs.Contains(member.OrganizationId))
                        return new ResponseVM<string> { IsSuccess = false, Message = "You cannot change role of Admin" };

                    member.MemberType = MemberTypeEnum.Member;
                    member.UpdatedBy = currentUserId;
                    member.UpdatedAt = DateTime.Now;
                    _db.OrganizationMemberships.Update(member);
                    await _db.SaveChangesAsync();
                    return new ResponseVM<string> { IsSuccess = true, Message = "Made Member Successfully!" };

                case MemberTypeEnum.Admin:
                    if (member.MemberType == MemberTypeEnum.Creator)
                        return new ResponseVM<string> { IsSuccess = false, Message = "Cannot change role of Creator" };

                    member.MemberType = member.MemberType != MemberTypeEnum.Admin ? MemberTypeEnum.Admin : MemberTypeEnum.Member;
                    member.UpdatedBy = currentUserId;
                    member.UpdatedAt = DateTime.Now;

                    _db.OrganizationMemberships.Update(member);
                    await _db.SaveChangesAsync();

                    return new ResponseVM<string>
                    {
                        IsSuccess = true,
                        Message = member.MemberType == MemberTypeEnum.Admin ? "Made Admin Successfully!" : "Removed Admin Successfully!"
                    };

                case MemberTypeEnum.Creator:
                    if (member.MemberType == MemberTypeEnum.Creator)
                        return new ResponseVM<string> { IsSuccess = false, Message = "Member is already Creator" };

                    // Update member to Creator
                    member.MemberType = MemberTypeEnum.Creator;
                    member.UpdatedBy = currentUserId;
                    member.UpdatedAt = DateTime.Now;
                    _db.OrganizationMemberships.Update(member);

                    // Update organization
                    var organization = await _db.Organizations.FirstOrDefaultAsync(x => x.Id == orgId);
                    if (organization != null)
                    {
                        organization.CreatedForId = targetUserId;
                        organization.UpdatedAt = DateTime.Now;
                        organization.UpdatedBy = currentUserId;
                        _db.Organizations.Update(organization);
                    }

                    // Downgrade current member to Admin
                    if (currentMember != null)
                    {
                        currentMember.MemberType = MemberTypeEnum.Admin;
                        currentMember.UpdatedBy = currentUserId;
                        currentMember.UpdatedAt = DateTime.Now;
                        _db.OrganizationMemberships.Update(currentMember);
                    }

                    await _db.SaveChangesAsync();
                    return new ResponseVM<string> { IsSuccess = true, Message = "Made Creator Successfully!" };

                case MemberTypeEnum.ReadOnly:
                    if (member.MemberType == MemberTypeEnum.Creator)
                        return new ResponseVM<string> { IsSuccess = false, Message = "Cannot change role of Creator" };

                    member.MemberType = MemberTypeEnum.ReadOnly;
                    member.UpdatedBy = currentUserId;
                    member.UpdatedAt = DateTime.Now;
                    _db.OrganizationMemberships.Update(member);
                    await _db.SaveChangesAsync();

                    return new ResponseVM<string> { IsSuccess = true, Message = "Member set to ReadOnly successfully!" };

                default:
                    return new ResponseVM<string> { IsSuccess = false, Message = "Invalid action" };
            }
        }

        // Allow a user to remove themselves from an organization, with role-based restrictions
        public async Task<ResponseVM<string>> SelfRemoveFromOrgAsync(Guid currentUserId, Guid orgId, MemberTypeEnum memberType)
        {
            var member = await _db.OrganizationMemberships
                .FirstOrDefaultAsync(o => o.AppUserId == currentUserId && o.OrganizationId == orgId && !o.IsDeleted);

            if (member is null)
                return new ResponseVM<string> { IsSuccess = false, Message = "Organization Member Not Found" };

            switch (memberType)
            {
                case MemberTypeEnum.Creator:
                    var otherCreators = await _db.OrganizationMemberships
                        .Where(o => o.OrganizationId == orgId && o.MemberType == MemberTypeEnum.Creator && o.AppUserId != currentUserId && !o.IsDeleted)
                        .ToListAsync();

                    if (otherCreators.Count == 0)
                        return new ResponseVM<string>
                        {
                            IsSuccess = false,
                            Message = "You are the only Creator. Assign another Creator before leaving."
                        };

                    member.MemberType = MemberTypeEnum.Admin; // downgrade to Admin
                    break;

                case MemberTypeEnum.Admin:
                    var otherAdmins = await _db.OrganizationMemberships
                        .Where(o => o.OrganizationId == orgId && o.MemberType == MemberTypeEnum.Admin && o.AppUserId != currentUserId && !o.IsDeleted)
                        .ToListAsync();

                    if (otherAdmins.Count == 0)
                        return new ResponseVM<string>
                        {
                            IsSuccess = false,
                            Message = "You are the only Admin. Assign another Admin before leaving."
                        };

                    member.MemberType = MemberTypeEnum.Member; // downgrade to Member
                    break;

                case MemberTypeEnum.Member:
                    var creatorsOrAdmins = await _db.OrganizationMemberships
                        .Where(o => o.OrganizationId == orgId &&
                                    (o.MemberType == MemberTypeEnum.Creator || o.MemberType == MemberTypeEnum.Admin) &&
                                    o.AppUserId != currentUserId &&
                                    !o.IsDeleted)
                        .ToListAsync();

                    if (creatorsOrAdmins.Count == 0)
                        return new ResponseVM<string>
                        {
                            IsSuccess = false,
                            Message = "You are the only Creator/Admin. Assign another before leaving."
                        };

                    member.IsDeleted = true; // delete membership
                    break;

                default:
                    return new ResponseVM<string> { IsSuccess = false, Message = "Invalid member type." };
            }

            member.UpdatedAt = DateTime.Now;
            member.UpdatedBy = currentUserId;
            _db.OrganizationMemberships.Update(member);
            await _db.SaveChangesAsync();

            return new ResponseVM<string>
            {
                IsSuccess = true,
                Message = memberType switch
                {
                    MemberTypeEnum.Creator => "Creator removed successfully!",
                    MemberTypeEnum.Admin => "Admin removed successfully!",
                    MemberTypeEnum.Member => "Member removed successfully!",
                    _ => "Removed successfully!"
                }
            };
        }


        //public async Task<ResponseVM<string>> RemoveMemberAsync(Guid id, Guid orgId, Guid currentUserId)
        //{
        //    var member = await _db.OrganizationMemberships
        //   .FirstOrDefaultAsync(o => o.AppUserId == id && o.OrganizationId == orgId && !o.IsDeleted);

        //    var currentUserMemberShips = await _db.OrganizationMemberships.Where(o => o.AppUserId == currentUserId && o.MemberType == MemberTypeEnum.Creator && !o.IsDeleted).Select(x => x.OrganizationId)
        //   .ToListAsync();

        //    if (member is null) return new ResponseVM<string> { IsSuccess = false, Message = "Organization Member Not Found" };
        //    if (member.MemberType == MemberTypeEnum.Creator) return new ResponseVM<string> { IsSuccess = false, Message = "You cannot remove Creator" };
        //    if (member.MemberType == MemberTypeEnum.Creator) return new ResponseVM<string> { IsSuccess = false, Message = "You cannot remove Creator" };
        //    if (member.MemberType == MemberTypeEnum.Admin && !currentUserMemberShips.Contains(member.OrganizationId)) return new ResponseVM<string> { IsSuccess = false, Message = "You cannot remove Admin" };
        //    member.IsDeleted = true;
        //    _db.OrganizationMemberships.Update(member);
        //    await _db.SaveChangesAsync();
        //    return new ResponseVM<string> { IsSuccess = true, Message = "Removed Successfully!" };
        //}

        //public async Task<ResponseVM<string>> MakeRemoverAdminAcync(Guid id, Guid orgId)
        //{
        //    var member = await _db.OrganizationMemberships
        //   .FirstOrDefaultAsync(o => o.AppUserId == id && o.OrganizationId == orgId && !o.IsDeleted);
        //    if (member is null) return new ResponseVM<string> { IsSuccess = false, Message = "Organization Member Not Found" };
        //    if (member.MemberType == MemberTypeEnum.Creator && member.MemberType == MemberTypeEnum.Admin) return new ResponseVM<string> { IsSuccess = false, Message = "Cannot change role of Creator" };

        //    if (member.MemberType != MemberTypeEnum.Admin)
        //        member.MemberType = MemberTypeEnum.Admin;
        //    else
        //        member.MemberType = MemberTypeEnum.Member;

        //    _db.OrganizationMemberships.Update(member);
        //    await _db.SaveChangesAsync();
        //    if (member.MemberType == MemberTypeEnum.Admin)
        //        return new ResponseVM<string> { IsSuccess = true, Message = "Made Admin Successfully!" };
        //    else
        //        return new ResponseVM<string> { IsSuccess = true, Message = "Removed Admin Successfully!" };
        //}

        //public async Task<ResponseVM<string>> MakeCreatorAcync(Guid id, Guid orgId, Guid CurrentUserId)
        //{
        //    var member = await _db.OrganizationMemberships
        //   .FirstOrDefaultAsync(o => o.AppUserId == id && o.OrganizationId == orgId && !o.IsDeleted);

        //    var currentMember = await _db.OrganizationMemberships
        //   .FirstOrDefaultAsync(o => o.AppUserId == CurrentUserId && o.OrganizationId == orgId && !o.IsDeleted);

        //    if (member is null) return new ResponseVM<string> { IsSuccess = false, Message = "Organization Member Not Found" };

        //    if (member.MemberType == MemberTypeEnum.Creator) return new ResponseVM<string> { IsSuccess = false, Message = "Member is already Creator" };

        //    member.MemberType = MemberTypeEnum.Creator;
        //    member.UpdatedBy = CurrentUserId;
        //    member.UpdatedAt = DateTime.Now;
        //    _db.OrganizationMemberships.Update(member);

        //    var organization = await _db.Organizations.FirstOrDefaultAsync(x => x.Id == orgId);

        //    organization.IsPrimary = false;
        //    organization.CreatedBy = id;
        //    organization.UpdatedAt = DateTime.Now;
        //    organization.UpdatedBy = CurrentUserId;
        //    _db.Organizations.Update(organization);

        //    currentMember.MemberType = MemberTypeEnum.Admin;
        //    currentMember.UpdatedBy = CurrentUserId;
        //    currentMember.UpdatedAt = DateTime.Now;
        //    _db.OrganizationMemberships.Update(currentMember);

        //    await _db.SaveChangesAsync();

        //    return new ResponseVM<string> { IsSuccess = true, Message = "Made Creator Successfully!" };
        //}

        //public async Task<ResponseVM<string>> SelfRemoveFromCreatorAcync(Guid currentUserId, Guid orgId)
        //{
        //    var member = await _db.OrganizationMemberships.Include(o => o.Organization)
        //   .FirstOrDefaultAsync(o => o.AppUserId == currentUserId && o.Organization.Id == orgId && !o.IsDeleted && !o.Organization.IsDeleted);

        //    if (member is null) return new ResponseVM<string> { IsSuccess = false, Message = "Organization Member Not Found" };

        //    var organization = await _db.OrganizationMemberships.Where(o => o.OrganizationId == orgId && o.MemberType == MemberTypeEnum.Creator && o.AppUserId != currentUserId && !o.IsDeleted).ToListAsync();

        //    if (organization == null || organization.Count == 0) return new ResponseVM<string>
        //    {
        //        IsSuccess = false,
        //        Message = "You are the only Creator, Please assign another member as Creator before removing yourself."
        //    };

        //    member.MemberType = MemberTypeEnum.Admin;
        //    member.UpdatedBy = currentUserId;
        //    member.UpdatedAt = DateTime.Now;
        //    _db.OrganizationMemberships.Update(member);
        //    await _db.SaveChangesAsync();
        //    return new ResponseVM<string> { IsSuccess = true, Message = "Creator Removed Successfully!" };
        //}

        //public async Task<ResponseVM<string>> SelfRemoveFromAdminAcync(Guid currentUserId, Guid orgId)
        //{
        //    var member = await _db.OrganizationMemberships
        //   .FirstOrDefaultAsync(o => o.AppUserId == currentUserId && o.OrganizationId == orgId && !o.IsDeleted);

        //    if (member is null) return new ResponseVM<string> { IsSuccess = false, Message = "Organization Member Not Found" };

        //    var organization = await _db.OrganizationMemberships.Where(o => o.OrganizationId == orgId && o.MemberType == MemberTypeEnum.Admin && o.AppUserId != currentUserId && !o.IsDeleted).ToListAsync();

        //    if (organization == null || organization.Count == 0) return new ResponseVM<string>
        //    {
        //        IsSuccess = false,
        //        Message = "You are the only Admin, Please assign another member as Admin before removing yourself."
        //    };

        //    member.MemberType = MemberTypeEnum.Admin;
        //    member.UpdatedBy = currentUserId;
        //    member.UpdatedAt = DateTime.Now;
        //    _db.OrganizationMemberships.Update(member);
        //    await _db.SaveChangesAsync();
        //    return new ResponseVM<string> { IsSuccess = true, Message = "Admin Removed Successfully!" };
        //}

        //public async Task<ResponseVM<string>> SelfRemoveAsMemberFromOrgAsync(Guid currentUserId, Guid orgId)
        //{
        //    var member = await _db.OrganizationMemberships
        //   .FirstOrDefaultAsync(o => o.AppUserId == currentUserId && o.OrganizationId == orgId && !o.IsDeleted);

        //    if (member is null) return new ResponseVM<string> { IsSuccess = false, Message = "Organization Member Not Found" };

        //    var organization = await _db.OrganizationMemberships.Where(o => o.OrganizationId == orgId && o.MemberType == MemberTypeEnum.Creator && o.MemberType == MemberTypeEnum.Admin && o.AppUserId != currentUserId && !o.IsDeleted).ToListAsync();

        //    if (organization == null || organization.Count == 0) return new ResponseVM<string>
        //    {
        //        IsSuccess = false,
        //        Message = "You are the only Creator/Admin, Please assign another member as Creator and Admin before removing yourself."
        //    };

        //    member.IsDeleted = true;
        //    member.UpdatedBy = currentUserId;
        //    member.UpdatedAt = DateTime.Now;
        //    _db.OrganizationMemberships.Update(member);
        //    await _db.SaveChangesAsync();
        //    return new ResponseVM<string> { IsSuccess = true, Message = "Member Removed Successfully!" };
        //}

        #endregion

        #region Admin

        // Get a list of all organizations with summary details for admin view
        public async Task<List<OrgListItemVm>> GetOrganizationsAsync()
        {
            return await _db.Organizations
                .Where(o => !o.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .Select(m => new OrgListItemVm
                {
                    Id = m.Id,
                    OrgName = m.OrgName,
                    ShortName = m.ShortName,
                    CreatorName = _db.Users
                                    .Where(x => x.Id == m.CreatedBy && !x.IsDeleted)
                                    .Select(x => x.FirstName + " " + x.LastName)
                                    .FirstOrDefault(), // stays in SQL
                    CreatedDate = m.CreatedAt,
                    tripCount = _db.Trips.Count(t => t.OrganizationId == m.Id && !t.IsDeleted),
                    groupCount = _db.Groups.Count(g => g.OrgId == m.Id && !g.IsDeleted),
                    IsActive = m.IsActive,
                    IsPrimary = m.IsPrimary,
                })
                .ToListAsync();
        }

        // Add or edit an organization by admin, with validation for unique names and primary status
        public async Task<ResponseVM<string>> AddEditOrganizationByAdminAsync(CreateOrganizationVm vm, Guid userId)
        {
            if (vm.Id == null)
            {
                if (await _db.Organizations.AnyAsync(o => o.OrgName == vm.OrgName && o.CreatedBy == userId)) { return new ResponseVM<string> { IsSuccess = false, Message = "This user already has an organization with the same name. Please choose a unique name." }; }

                if (vm.IsPrimary == true && await _db.Organizations.AnyAsync(o => o.IsPrimary && o.CreatedBy == userId && !o.IsDeleted))
                {
                    var primaryOrg = await _db.Organizations
                        .Where(o => o.IsPrimary && o.CreatedBy == userId && !o.IsDeleted)
                        .ToListAsync();

                    if (primaryOrg != null)
                    {
                        foreach (var org1 in primaryOrg)
                        {
                            org1.IsPrimary = false;
                            _db.Organizations.Update(org1);
                        }
                    }
                }

                var org = new Organization
                {
                    OrgName = vm.OrgName,
                    ShortName = vm.ShortName,
                    IsPrimary = vm.IsPrimary ?? true,
                    IsActive = true,
                    CreatedForId = vm.userId.Value,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                };
                _db.Organizations.Add(org);
                _db.OrganizationMemberships.Add(new OrganizationMembership
                {
                    Organization = org,
                    AppUserId = userId,
                    IsInvited = false,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                    MemberType = MemberTypeEnum.Creator,
                });

                await _db.SaveChangesAsync();

                await _permissionService.CreateDefaultPermissionsAsync(org.Id); // Create default permissions for the new organization
                return new ResponseVM<string> { IsSuccess = true, Message = "Organization Added!", Data = org.Id.ToString() };
            }
            else
            {
                var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == vm.Id);
                if (org == null)
                    return new ResponseVM<string> { IsSuccess = false, Message = "Organization not found" };

                ;
                if (!vm.IsActive && (string.IsNullOrEmpty(vm.DeActiveDiscription)))
                    return new ResponseVM<string>
                    {
                        IsSuccess = false,
                        Message = "Please provide a reason for deactivating the organization."
                    };


                if (org.OrgName != vm.OrgName && await _db.Organizations.AnyAsync(o => o.OrgName == vm.OrgName && o.CreatedBy == userId))
                    return new ResponseVM<string> { IsSuccess = false, Message = "This user already has an organization with the same name. Please choose a unique name." };

                if (vm.IsPrimary == true && !org.IsPrimary && await _db.Organizations.AnyAsync(o => o.IsPrimary && o.CreatedBy == userId && !o.IsDeleted))
                {
                    var primaryOrg = await _db.Organizations
                        .Where(o => o.IsPrimary && o.CreatedBy == userId && !o.IsDeleted)
                        .ToListAsync();

                    if (primaryOrg != null)
                    {
                        foreach (var org1 in primaryOrg)
                        {
                            org1.IsPrimary = false;
                            _db.Organizations.Update(org1);
                        }
                    }
                }

                if (org.IsPrimary == true && !vm.IsActive)
                    return new ResponseVM<string> { IsSuccess = false, Message = "You can not deactive primary organization." };

                if (vm.IsPrimary == false && org.IsPrimary && org.CreatedBy == userId)
                    return new ResponseVM<string> { IsSuccess = false, Message = "User must have at least one primary organization." };

                if (vm.IsActive == false && org.IsActive && !await _db.Organizations.AnyAsync(o => o.IsActive && o.Id != org.Id && o.CreatedBy == userId && !o.IsDeleted))
                    return new ResponseVM<string> { IsSuccess = false, Message = "User must have at least one active organization." };

                org.OrgName = vm.OrgName;
                org.ShortName = vm.ShortName;
                org.IsActive = vm.IsActive;
                org.DeActiveDiscription = vm.IsActive ? null : vm.DeActiveDiscription;
                org.IsPrimary = vm.IsPrimary ?? org.IsPrimary;
                org.UpdatedBy = userId;
                org.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                return new ResponseVM<string> { IsSuccess = true, Message = "Organization Updated!", Data = org.Id.ToString() };
            }
        }

        // Get detailed information about a specific organization, including its creator and permissions
        public async Task<ResponseVM<OrgDetailsVm>> GetOrganizationDetailsAsync(Guid orgId, Guid? userId = null)
        {
            var org = await _db.Organizations
                .Include(o => o.Members).ThenInclude(m => m.AppUser)
                //.Include(o => o.Groups)
                .FirstOrDefaultAsync(o => o.Id == orgId);

            if (org == null)
                return new ResponseVM<OrgDetailsVm> { IsSuccess = false, Message = "Organization not found" };

            var creator = org.Members.FirstOrDefault(m => m.MemberType == MemberTypeEnum.Creator);

            // Permissions async
            var permissions = await _permissionService.GetPermissionsAsync(org.Id);

            var result = new OrgDetailsVm
            {
                Id = org.Id,
                UserId = org.CreatedBy,
                OrgName = org.OrgName,
                ShortName = org.ShortName,
                IsActive = org.IsActive,
                DeActiveDiscription = org.DeActiveDiscription,
                IsPrimary = org.IsPrimary,
                Creator = creator != null ? new CreatorVm
                {
                    Id = creator.AppUserId,
                    FullName = creator.AppUser.FirstName + " " + creator.AppUser.LastName,
                    Email = creator.AppUser.Email,
                    Number = creator.AppUser.PhoneNumber
                } : null!,
                Permissions = permissions,
                MemberType = userId.HasValue
                             ? org.Members.FirstOrDefault(m => m.AppUserId == userId.Value)?.MemberType.ToString()
                             : null
            };

            return new ResponseVM<OrgDetailsVm>
            {
                IsSuccess = true,
                Message = "Organization details fetched successfully",
                Data = result
            };
        }

        // Get a list of groups within an organization, including trip counts and creator status
        public async Task<ResponseVM<List<GroupListItemVm>>> GetOrganizationGroupsAsync(Guid orgId)
        {
            var org = await _db.Organizations
                .Include(o => o.Members).ThenInclude(m => m.AppUser)
                .Include(o => o.Groups)
                .FirstOrDefaultAsync(o => o.Id == orgId);

            if (org == null)
                return new ResponseVM<List<GroupListItemVm>> { IsSuccess = false, Message = "Organization not found" };

            var creator = org.Members.FirstOrDefault(m => m.MemberType == MemberTypeEnum.Creator);

            var tripsCounts = await (from g in _db.Groups
                                     join t in _db.Trips on g.Id equals t.GroupId into gt
                                     from t in gt.DefaultIfEmpty()
                                     where g.OrgId == org.Id && !t.IsDeleted
                                     group t by g.Id into gGroup
                                     select new
                                     {
                                         GroupId = gGroup.Key,
                                         Count = gGroup.Count(t => t != null)
                                     }).ToListAsync();

            // Permissions async
            var permissions = await _permissionService.GetPermissionsAsync(org.Id);

            var result = org.Groups.Where(x => !x.IsDeleted).OrderByDescending(x => x.CreatedAt).Select(g => new GroupListItemVm
            {
                Id = g.Id,
                UserId = org.CreatedBy,
                GroupName = g.GroupName,
                Description = g.Description ?? "",
                ShortName = g.ShortName,
                Status = g.IsActive ? "<span class=\"badge badge-soft-success\"> Active</span>" : "<span class=\"badge badge-soft-danger\">  Not Active</span>",
                TripsCount = tripsCounts.FirstOrDefault(x => x.GroupId == g.Id)?.Count ?? 0,
                IsCreator = org.Members.Any(m => m.MemberType == MemberTypeEnum.Creator && m.AppUserId == creator?.AppUserId)
            }).ToList();

            return new ResponseVM<List<GroupListItemVm>>
            {
                IsSuccess = true,
                Message = "Organization groups fetched successfully",
                Data = result
            };
        }

        // Get a list of members within an organization, excluding the current user if specified
        public async Task<ResponseVM<List<OrgMembersListVM>>> GetOrganizationMembersAsync(Guid orgId, Guid? CurrentUserId = null)
        {
            var org = await _db.Organizations
                .Include(o => o.Members).ThenInclude(m => m.AppUser)
                .FirstOrDefaultAsync(o => o.Id == orgId);

            if (org == null)
                return new ResponseVM<List<OrgMembersListVM>> { IsSuccess = false, Message = "Organization not found" };

            // Permissions async
            var permissions = await _permissionService.GetPermissionsAsync(org.Id);

            var result = org.Members.Where(x => !x.IsDeleted && x.AppUserId != CurrentUserId).OrderByDescending(x => x.CreatedAt).Select(m => new OrgMembersListVM
            {
                Id = m.Id,
                UserId = org.CreatedBy,
                OrganizationId = m.OrganizationId,
                AppUserId = m.AppUserId,
                UserName = m.AppUser.FirstName + " " + m.AppUser.LastName,
                Email = m.AppUser.Email,
                PhoneNumber = m.AppUser.PhoneNumber,
                MemberType = m.MemberType.ToString()
            }).ToList()
            ;

            return new ResponseVM<List<OrgMembersListVM>>
            {
                IsSuccess = true,
                Message = "Organization members fetched successfully",
                Data = result
            };
        }

        // Get detailed information about a specific organization member for admin view
        public async Task<OrgMemberDetailsResponseVM> GetMemberDetailsByAdminAsync(Guid id, Guid orgId)
        {
            // First, get the org itself
            var org = await _db.Organizations
                .Where(o => o.Id == orgId && !o.IsDeleted)
                .FirstOrDefaultAsync();

            if (org == null)
                return null;

            // Get the member being viewed
            var memberData = await _db.OrganizationMemberships
                .Include(o => o.AppUser)
                .Where(o => o.OrganizationId == orgId && o.AppUserId == id && !o.IsDeleted)
                .Select(m => new
                {
                    m.Id,
                    m.OrganizationId,
                    m.AppUserId,
                    UserName = m.AppUser.FirstName + " " + m.AppUser.LastName,
                    m.AppUser.Email,
                    m.AppUser.PhoneNumber,
                    MemberType = m.MemberType,
                })
                .FirstOrDefaultAsync();

            if (memberData == null)
                return null;

            // Build the response
            var member = new OrgMembersListVM
            {
                Id = memberData.Id,
                OrganizationId = memberData.OrganizationId,
                AppUserId = memberData.AppUserId,
                UserName = memberData.UserName,
                Email = memberData.Email,
                PhoneNumber = memberData.PhoneNumber,
                MemberType = memberData.MemberType.ToString(),
            };

            var result = new OrgMemberDetailsResponseVM
            {
                Org = new OrgListItemVm
                {
                    Id = org.Id,
                    OrgName = org.OrgName,
                    IsActive = org.IsActive,
                },
                Member = member
            };

            return result;
        }

        // Delete an organization member, with restrictions on deleting the creator
        public async Task<ResponseVM<string>> DeleteOrgMemberAsync(Guid id)
        {
            var member = await _db.OrganizationMemberships.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

            if (member is null) return new ResponseVM<string> { IsSuccess = false, Message = "Organization Member Not Found" };
            if (member.MemberType == MemberTypeEnum.Creator) return new ResponseVM<string> { IsSuccess = false, Message = "You cannot delete Creator" };

            member.IsDeleted = true;
            _db.OrganizationMemberships.Update(member);
            await _db.SaveChangesAsync();

            return new ResponseVM<string> { IsSuccess = true, Message = "Member Deleted Successfully!" };
        }

        // Change the role of an organization member, with restrictions on changing the creator's role
        public async Task<ResponseVM<string>> ChangeMemberRoleAsync(Guid memberShipId, MemberTypeEnum newRole)
        {
            var member = await _db.OrganizationMemberships.FirstOrDefaultAsync(o => o.Id == memberShipId && !o.IsDeleted);
            if (member is null) return new ResponseVM<string> { IsSuccess = false, Message = "Organization Member Not Found" };

            if (member.MemberType == MemberTypeEnum.Creator) return new ResponseVM<string> { IsSuccess = false, Message = "You cannot change role of Creator" };
            if (member.MemberType == newRole) return new ResponseVM<string> { IsSuccess = false, Message = "Member already has this role" };

            member.MemberType = newRole;
            _db.OrganizationMemberships.Update(member);
            await _db.SaveChangesAsync();
            return new ResponseVM<string> { IsSuccess = true, Message = "Member Role Changed Successfully!" };
        }

        // Get a list of organizations a user belongs to, with summary details for each organization
        public async Task<ResponseVM<List<OrgListItemVm>>> getUserOrgGropTripShortDetails(Guid userId)
        {
            var orgs = await _db.OrganizationMemberships
                .Where(om => om.AppUserId == userId
                             && !om.IsDeleted
                             && !om.Organization.IsDeleted)
                .Select(om => new OrgListItemVm
                {
                    Id = om.Organization.Id,
                    OrgName = om.Organization.OrgName,
                    ShortName = om.Organization.ShortName,
                    CreatorName = _db.Users
                                    .Where(x => x.Id == om.Organization.CreatedBy && !x.IsDeleted)
                                    .Select(x => x.FirstName + " " + x.LastName)
                                    .FirstOrDefault(), // stays in SQL

                    MemberType = om.MemberType.ToString(),

                    tripCount = _db.Trips.Count(t => t.OrganizationId == om.Organization.Id && !t.IsDeleted),
                    groupCount = _db.Groups.Count(g => g.OrgId == om.Organization.Id && !g.IsDeleted),

                    CreatedDate = om.Organization.CreatedAt,
                    IsActive = om.Organization.IsActive,
                    IsPrimary = om.Organization.IsPrimary,
                })
                .ToListAsync();

            return new ResponseVM<List<OrgListItemVm>>
            {
                IsSuccess = true,
                Message = orgs.Any() ? "Organizations Found" : "No Organizations Found",
                Data = orgs
            };
        }


        #endregion

        #region groups

        // Get a list of groups a user belongs to within a specific organization
        public async Task<ResponseVM<List<GroupResponseVM>>> GetUserGroupsAsync(Guid userId, Guid orgId)
        {
            var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == orgId && !o.IsDeleted);
            if (org == null) return new ResponseVM<List<GroupResponseVM>> { IsSuccess = false, Message = "Organization Not Found" };
            var groups = await _db.Groups.Where(o => o.OrgId == orgId && !o.IsDeleted).OrderByDescending(x => x.CreatedAt).Select(o => new GroupResponseVM
            {
                Id = o.Id,
                GroupName = o.GroupName,
                ShortName = o.ShortName,
                Description = o.Description,
                OrgId = org.Id,
                OrgName = org.OrgName,
            }).ToListAsync() ?? new List<GroupResponseVM>();
            return new ResponseVM<List<GroupResponseVM>> { IsSuccess = true, Message = "Groups Found", Data = groups };
        }

        // Get detailed information about a specific group, including its organization details
        public async Task<ResponseVM<GroupResponseVM>> GetGroupAsync(Guid groupId)
        {
            var group = await _db.Groups.FirstOrDefaultAsync(o => o.Id == groupId && !o.IsDeleted);
            if (group == null) return new ResponseVM<GroupResponseVM> { IsSuccess = false, Message = "Group Not Found" };
            var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == group.OrgId && !o.IsDeleted);
            if (org == null) return new ResponseVM<GroupResponseVM> { IsSuccess = false, Message = "Organization Not Found" };
            var data = new GroupResponseVM
            {
                Id = group.Id,
                GroupName = group.GroupName,
                ShortName = group.ShortName,
                Description = group.Description,
                IsActive = group.IsActive,
                DeActiveDiscription = group.DeActiveDiscription,
                OrgId = org.Id,
                OrgName = org.OrgName,
            } ?? new GroupResponseVM();
            return new ResponseVM<GroupResponseVM> { IsSuccess = true, Message = "Group Found", Data = data };
        }

        // Get a group by its associated organization ID
        public async Task<ResponseVM<GroupRequestVM>> GetGroupByOrgIdAsync(Guid orgId)
        {
            var group = await _db.Groups.FirstOrDefaultAsync(o => o.OrgId == orgId && !o.IsDeleted);
            if (group == null) return new ResponseVM<GroupRequestVM> { IsSuccess = false, Message = "Group Not Found" };

            var data = new GroupRequestVM
            {
                Id = group.Id,
                GroupName = group.GroupName,
                ShortName = group.ShortName,
                Description = group.Description,
                IsActive = group.IsActive,
                DeActiveDiscription = group.DeActiveDiscription,
                OrgId = group.OrgId,
            } ?? new GroupRequestVM();

            return new ResponseVM<GroupRequestVM> { IsSuccess = true, Message = "Group Found", Data = data };
        }

        // Get detailed information about a specific group along with its organization details
        public async Task<ResponseVM<GroupDetailsResponseVM>> GetGroupDetailsAsync(Guid groupId)
        {
            var group = await _db.Groups.FirstOrDefaultAsync(o => o.Id == groupId && !o.IsDeleted);
            if (group == null) return new ResponseVM<GroupDetailsResponseVM> { IsSuccess = false, Message = "Group Not Found" };
            var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == group.OrgId && !o.IsDeleted);
            if (org == null) return new ResponseVM<GroupDetailsResponseVM> { IsSuccess = false, Message = "Organization Not Found" };

            var groupData = new GroupResponseVM
            {
                Id = group.Id,
                GroupName = group.GroupName,
                ShortName = group.ShortName,
                Description = group.Description,
                IsActive = group.IsActive,
                DeActiveDiscription = group.DeActiveDiscription,
                OrgId = org.Id,
                OrgName = org.OrgName,
                CreatedDate = group.CreatedAt,
            } ?? new GroupResponseVM();

            var orgData = new OrgListItemVm
            {
                Id = org.Id,
                OrgName = org.OrgName,
                ShortName = org.ShortName,
                tripCount = _db.Trips.Count(t => t.OrganizationId == org.Id && !t.IsDeleted),
                groupCount = _db.Groups.Count(g => g.OrgId == org.Id && !g.IsDeleted),
                CreatedDate = org.CreatedAt,
                IsActive = org.IsActive,
                IsPrimary = org.IsPrimary,
            };

            var data = new GroupDetailsResponseVM
            {
                Org = orgData,
                Group = groupData
            };

            return new ResponseVM<GroupDetailsResponseVM> { IsSuccess = true, Message = "Group Found", Data = data };
        }


        // Add or edit a group within an organization, with validation for unique names and deactivation reasons
        public async Task<ResponseVM<Group>> AddEditGroupAsync(GroupRequestVM vm, Guid userId)
        {
            var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == vm.OrgId && !o.IsDeleted);
            if (org == null)
            { return new ResponseVM<Group> { IsSuccess = false, Message = "Organization not found." }; }
            if (vm.Id == null)
            {
                if (await _db.Groups.AnyAsync(o => o.GroupName == vm.GroupName && o.OrgId == org.Id && !o.IsDeleted)) { return new ResponseVM<Group> { IsSuccess = false, Message = "Group name must be unique." }; }
                var group = new Group
                {
                    Id = Guid.NewGuid(),
                    GroupName = vm.GroupName,
                    OrgId = org.Id,
                    ShortName = vm.ShortName,
                    Description = vm.Description,
                    IsActive = true,
                    CreatedForId = org.CreatedForId,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                };
                await _db.Groups.AddAsync(group);
                int res = await _db.SaveChangesAsync();
                if (res == 0)
                    return new ResponseVM<Group> { IsSuccess = false, Message = "Failed to add Group" };
                return new ResponseVM<Group> { IsSuccess = true, Message = "Group Added!", Data = group };
            }
            else
            {
                var group = await _db.Groups.FirstOrDefaultAsync(o => o.Id == vm.Id && !o.IsDeleted);
                if (group == null) return new ResponseVM<Group> { IsSuccess = false, Message = "Group Not Found" };

                if (!vm.IsActive && (string.IsNullOrEmpty(vm.DeActiveDiscription)))
                {
                    return new ResponseVM<Group>
                    {
                        IsSuccess = false,
                        Message = "Please provide a reason for deactivating the group."
                    };
                }

                if (group.GroupName != vm.GroupName &&
                 await _db.Groups.AnyAsync(o => o.GroupName == vm.GroupName && o.OrgId == org.Id && o.Id != group.Id && !o.IsDeleted))
                {
                    return new ResponseVM<Group> { IsSuccess = false, Message = "Group name must be unique." };
                }

                group.GroupName = vm.GroupName;
                group.ShortName = vm.ShortName;
                group.Description = vm.Description;
                group.IsActive = vm.IsActive;
                group.DeActiveDiscription = vm.DeActiveDiscription;
                group.UpdatedBy = userId;
                group.UpdatedAt = DateTime.Now;
                _db.Groups.Update(group);
                await _db.SaveChangesAsync();
                return new ResponseVM<Group> { IsSuccess = true, Message = "Group Updated!", Data = group };
            }
        }

        // Revert a deleted group by removing the deletion flag and saving changes
        public async Task<ResponseVM<string>> RevertGroupAsync(Guid gId)
        {
            var group = await _db.Groups.FirstOrDefaultAsync(o => o.Id == gId && o.IsDeleted);
            if (group == null) return new ResponseVM<string> { IsSuccess = false, Message = "Group not found" };
            _db.Groups.Remove(group);
            await _db.SaveChangesAsync();
            return new ResponseVM<string> { IsSuccess = true, Message = "Group Reverted Successfully!" };
        }

        // Soft delete a group by setting the deletion flag and saving changes
        public async Task<ResponseVM<dynamic>> DeleteGroupAsync(Guid groupId)
        {
            var group = await _db.Groups.Include(x => x.Org)
           .FirstOrDefaultAsync(o => o.Id == groupId && !o.IsDeleted);
            if (group is null) return new ResponseVM<dynamic> { IsSuccess = false, Message = "Group Not Found" };
            group.IsDeleted = true;
            _db.Groups.Update(group);
            await _db.SaveChangesAsync();
            return new ResponseVM<dynamic> 
            { 
                IsSuccess = true, 
                Message = "Group Deleted Successfully!",
                Data = new { groupName = group.GroupName, OrgName = group.Org.OrgName, userId = group.Org.CreatedBy }
            };
        }

        #endregion
    }
}
