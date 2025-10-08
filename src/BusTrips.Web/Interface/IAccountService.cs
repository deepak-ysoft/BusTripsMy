using BusTrips.Domain.Entities;
using BusTrips.Web.Models;

namespace BusTrips.Web.Interface
{
    public interface IAccountService
    {
        Task<ResponseVM<AppUser>> RegisterAsync(RegisterVm vm, string role = "User");
        Task<ResponseVM<string>> LoginAsync(LoginVm vm, string role);
        Task LogoutAsync();
        Task<ResponseVM<AppUser>> AddUserAsync(AddUserVm vm, Guid userId);
        Task<ResponseVM<List<TermsAndConditionResponseVM>>> GetTermsAndConditionsAsync(string role);
        Task<ResponseVM<string>> AddEditTermsAndConditionAsync(TermsAndConditionRequestVM vm, Guid userId);
        Task<ResponseVM<string>> DeleteTermsAndConditionAsync(Guid id,Guid userId);
    }
}
