using BusTrips.Domain.Entities;
using BusTrips.Infrastructure.Persistence;
using BusTrips.Web.Interface;
using BusTrips.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BusTrips.Web.Services
{
    public class UserService : IUserService
    {
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _db;
        private readonly IOrganizationService _organizationService;
        private readonly UserManager<AppUser> _users;
        public UserService(AppDbContext db, UserManager<AppUser> users, IWebHostEnvironment env, IOrganizationService organizationService)
        {
            _db = db;
            _users = users;
            _env = env;
            _organizationService = organizationService;
        }

        // Get all users with "User" role who are not deleted
        public async Task<List<UserVM>> GetAllUsersAsync()
        {
            return await (from u in _users.Users
                          join ur in _db.UserRoles on u.Id equals ur.UserId
                          join r in _db.Roles on ur.RoleId equals r.Id
                          where r.Name == "User" && !u.IsDeleted
                          orderby u.CreatedAt descending
                          select new UserVM
                          {
                              UserId = u.Id,
                              PhotoUrl = u.PhotoUrl,
                              FirstName = u.FirstName ?? "-",
                              LastName = u.LastName ?? "-",
                              Email = u.Email ?? "-",
                              EmailConfirmed = u.EmailConfirmed,
                              isActive = u.IsActive,
                              PhoneNumber = u.PhoneNumber ?? "-",
                              PhoneNumberConfirmed = u.PhoneNumberConfirmed
                          }).ToListAsync();
        }

        // Get user details by user ID, including driver info if applicable
        public async Task<UserRequestVm> GetUserByIdAsync(Guid id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return new UserRequestVm();
            var driver = await _db.BusDrivers.FirstOrDefaultAsync(d => d.AppUserId == id);

            var vm = new UserRequestVm
            {
                UserId = user.Id,
                Email = user.Email,
                SecondaryEmail = user.SecondaryEmail,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                PhoneNumber2 = user.PhoneNumber2,
                LicenseNumber = driver?.LicenseNumber,
                LicenseProvince = driver?.LicenseProvince,
            };
            return vm;
        }

        // Get detailed user info for admin view, including role and organization details
        public async Task<UserResponseVm> GetUserDetailsForAdminByIdAsync(Guid id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return new UserResponseVm();

            var roles = await _users.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "N/A";

            BusDriver? driver = null;
            if (role == "Driver")
                driver = await _db.BusDrivers.FirstOrDefaultAsync(d => d.AppUserId == id);

            var OrganizationsVm = await _organizationService.getUserOrgGropTripShortDetails(id);

            var vm = new UserResponseVm
            {
                UserId = user.Id,
                Email = user.Email,
                SecondaryEmail = user.SecondaryEmail,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                PhoneNumber2 = user.PhoneNumber2,
                PhotoUrl = user?.PhotoUrl,
                LicenseNumber = driver?.LicenseNumber,
                LicenseProvince = driver?.LicenseProvince,
                BirthDate = driver?.BirthDate,
                EmploymentType = driver?.EmploymentType,
                LicenseFrontUrl = driver?.LicenseFrontUrl,
                LicenseBackUrl = driver?.LicenseBackUrl,
                env = _env.WebRootPath,
                Role = role,
                ApprovalStatus = driver?.ApprovalStatus.ToString(),
                CreatedAt = user.CreatedAt.ToString("dd/MMM/yyyy"),
                Organizations = OrganizationsVm.Data
            };
            return vm;
        }

        // Soft delete user by setting IsActive to false and IsDeleted to true
        public async Task<ResponseVM<string>> DeleteUserAsync(Guid userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return new ResponseVM<string> { IsSuccess = false, Message = "User not found." };
            user.IsActive = false;
            user.IsDeleted = true;
            user.DeActiveDiscription = "Deactivated by Admin";
            await _db.SaveChangesAsync();
            return new ResponseVM<string> { IsSuccess = true, Message = "User deleted successfully." };
        }
    }
}
