using BusTrips.Domain.Entities;
using BusTrips.Infrastructure.Persistence;
using BusTrips.Web.Interface;
using BusTrips.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTrips.Web.Services
{
    public class DriverService : IDriverService
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        public DriverService(AppDbContext db, IWebHostEnvironment env) {_db = db;_env = env; } 

        public async Task<List<object>> GetDriversAsync(string? status) // status: Pending, Approved, Rejected, null=all
        {
            var q = _db.BusDrivers.Include(d => d.AppUser).AsQueryable();
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<DriverApprovalStatus>(status, true, out var s))
                q = q.Where(d => d.ApprovalStatus == s);

            return await q.OrderByDescending(x => x.CreatedAt).Select(d => new { d.AppUser.Email, d.ApprovalStatus, d.DriverId, d.AppUserId })
                          .ToListAsync<object>();
        }

        // Get driver by AppUserId (which is also BusDriver.Id)
        public async Task<BusDriver> GetDriverByIdAsync(Guid userId)
        {
            var driver = await _db.BusDrivers
                                  .Include(d => d.AppUser)
                                  .FirstOrDefaultAsync(x => x.AppUserId == userId);
            return driver;
        }

        // Approve driver and generate DriverId
        public async Task<ResponseVM<string>> ApproveDriverAsync(Guid userId)
        {
            var d = await _db.BusDrivers.FindAsync(userId);
            if (d is null) return new ResponseVM<string> { IsSuccess = false, Message = "Bus Driver Not Found" };
            d.ApprovalStatus = DriverApprovalStatus.Approved;
            d.DriverId = $"DRV-{userId.ToString()[..8].ToUpper()}";
            await _db.SaveChangesAsync();
            return new ResponseVM<string> { IsSuccess = true, Message = "Driver Approved Successfully!" };
        }

        // Reject driver application
        public async Task<ResponseVM<string>> RejectDriverAsync(Guid userId)
        {
            var d = await _db.BusDrivers.FindAsync(userId);
            if (d is null) return new ResponseVM<string> { IsSuccess = false, Message = "Bus Driver Not Found" };
            d.ApprovalStatus = DriverApprovalStatus.Rejected;
            await _db.SaveChangesAsync();
            return new ResponseVM<string> { IsSuccess = true, Message = "Driver Rejected Successfully!" };
        }

        // Update driver details and images
        public async Task<ResponseVM<string>> UpdateDriverAsync(UserRequestVm vm, AppUser user)
        {
            var driver = await _db.BusDrivers.FirstOrDefaultAsync(d => d.AppUserId == user.Id);
            if (driver == null) return new ResponseVM<string> { IsSuccess = false, Message = "Bus Driver Not Found" };

            // Save images (relativePath is folder inside wwwroot)
            var licenseFrontUrl = await SaveFileAsync(vm.LicenseFrontImg, "uploads/license");
            var licenseBackUrl = await SaveFileAsync(vm.LicenseBackImg, "uploads/license");

            // Update driver details
            driver.LicenseNumber = vm.LicenseNumber ?? "";
            driver.LicenseProvince = vm.LicenseProvince ?? "";
            driver.BirthDate = vm.BirthDate;
            driver.EmploymentType = vm.EmploymentType ?? "FullTime";
            driver.UpdatedBy = user.Id;
            driver.UpdatedAt = DateTime.Now;

       

            if (!string.IsNullOrEmpty(licenseFrontUrl))
                driver.LicenseFrontUrl = licenseFrontUrl;

            if (!string.IsNullOrEmpty(licenseBackUrl))
                driver.LicenseBackUrl = licenseBackUrl;

            _db.BusDrivers.Update(driver);
            await _db.SaveChangesAsync();
            return new ResponseVM<string> { IsSuccess = true, Message = "Driver Updated Successfully!" };
        }

        // Save uploaded file to wwwroot and return relative URL for DB storage
        public async Task<string?> SaveFileAsync(IFormFile? file, string relativePath)
        {
            if (file == null || file.Length == 0) return null;

            // Build full path inside wwwroot
            var uploadPath = Path.Combine(_env.WebRootPath, relativePath);
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            // Unique file name
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fullPath = Path.Combine(uploadPath, fileName);

            // Save file
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return concatenated relative path (for DB)
            return Path.Combine("/", relativePath.Replace("\\", "/"), fileName);
        }
    }
}
