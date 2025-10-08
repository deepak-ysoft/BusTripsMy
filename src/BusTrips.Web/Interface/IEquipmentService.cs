using BusTrips.Web.Models;

namespace BusTrips.Web.Interface
{
    public interface IEquipmentService
    {
        Task<List<EquipmentListVm>> GetEquipmentsAsync();
        Task<EquipmentVM?> GetEquipmentByIdAsync(Guid id);
        Task<dynamic?> GetEquipmentImgAndDocByIdAsync(Guid id);
        Task<ResponseVM<string>> AddEquipmentAsync(EquipmentVM vm, Guid currentUserId);
        Task<ResponseVM<string>> UpdateEquipmentAsync(EquipmentVM vm, Guid currentUserId);
        Task<ResponseVM<string>> DeleteEquipmentAsync(Guid id, Guid currentUserId);
        Task<ResponseVM<string>> DeleteEquipmentImageAsync(Guid equipmentId, string imageUrl);
        Task<ResponseVM<string>> DeleteEquipmentDocumentAsync(Guid documentId);
    }
}
