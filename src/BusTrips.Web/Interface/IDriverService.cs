using BusTrips.Domain.Entities;
using BusTrips.Web.Models;

namespace BusTrips.Web.Interface
{
    public interface IDriverService
    {
        Task<List<object>> GetDriversAsync(string? status);
        Task<ResponseVM<string>> ApproveDriverAsync(Guid userId);
        Task<ResponseVM<string>> RejectDriverAsync(Guid userId);
        Task<BusDriver> GetDriverByIdAsync(Guid UserId);
        Task<ResponseVM<string>> UpdateDriverAsync(UserRequestVm vm, AppUser user);
        Task<string?> SaveFileAsync(IFormFile? file, string relativePath);
    }
}
