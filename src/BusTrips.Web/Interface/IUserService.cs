using BusTrips.Domain.Entities;
using BusTrips.Web.Models;

namespace BusTrips.Web.Interface
{
    public interface IUserService
    {
        Task<List<UserVM>> GetAllUsersAsync();
        Task<UserRequestVm> GetUserByIdAsync(Guid id);
        Task<UserResponseVm> GetUserDetailsForAdminByIdAsync(Guid id);
        Task<ResponseVM<string>> DeleteUserAsync(Guid userId);

    }
}
