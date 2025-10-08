using BusTrips.Domain.Entities;
using BusTrips.Infrastructure.Persistence;
using BusTrips.Web.Interface;
using BusTrips.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTrips.Web.Services
{
    public class EquipmentService : IEquipmentService
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        public EquipmentService(AppDbContext db, IWebHostEnvironment env) { _db = db; _env = env; }

        // Get all non-deleted equipment, ordered by CreatedAt descending
        public async Task<List<EquipmentListVm>> GetEquipmentsAsync()
        {
            return await _db.Equipment
                .Where(e => !e.IsDeleted).OrderByDescending(x => x.CreatedAt)
                .Select(e => new EquipmentListVm
                {
                    Id = e.Id,
                    BusNumber = e.BusNumber,              // new field
                    LicensePlate = e.LicensePlate,
                    IssuingProvince = e.IssuingProvince,  // new field
                    Manufacturer = e.Manufacturer,
                    Model = e.Model,
                    Year = e.Year,
                    IsActive = e.IsActive
                }).ToListAsync();
        }

        // Get equipment by ID, including non-deleted documents
        public async Task<EquipmentVM?> GetEquipmentByIdAsync(Guid id)
        {
            var eq = await _db.Equipment
                .Include(e => e.Documents)
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

            if (eq is null) return null;

            return new EquipmentVM
            {
                Id = eq.Id,
                BusNumber = eq.BusNumber,
                Vin = eq.Vin,
                VehicleType = eq.VehicleType,
                LicensePlate = eq.LicensePlate,
                IssuingProvince = eq.IssuingProvince,
                Manufacturer = eq.Manufacturer,
                Model = eq.Model,
                Year = eq.Year,
                Color = eq.Color,
                SeatingCapacity = eq.SeatingCapacity,
                Length = eq.Length,
                Height = eq.Height,
                GrossVehicleWeight = eq.GrossVehicleWeight,
                IsActive = eq.IsActive,
                VehicleUrl = eq.VehicleUrl,
                DeactivationReason = eq.DeactivationReason,
                DeactivatedAt = eq.DeactivatedAt,
                // Only include non-deleted documents
                Documents = eq.Documents
                              .Where(d => !d.IsDeleted)
                              .Select(d => new EquipmentDocumentVM
                              {
                                  Id = d.Id,
                                  FilePath = d.FilePath,
                                  Description = d.Description,
                                  UploadedAt = d.UploadedAt
                              }).ToList()
            };
        }

        // Get count of images and documents for an equipment by ID
        public async Task<dynamic?> GetEquipmentImgAndDocByIdAsync(Guid id)
        {
            var equipment = await _db.Equipment
                .Include(e => e.Documents)
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

            if (equipment == null)
                return null;

            // Get images as comma-separated string
            var images = string.IsNullOrWhiteSpace(equipment.VehicleUrl)
            ? Array.Empty<string>() // Use an empty string array instead of an empty string
            : equipment.VehicleUrl.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(i => i.Trim())
                          .ToArray();

            var imageCount = images.Length;
            var docCount = equipment.Documents?.Count ?? 0;


            return new
            {
                ImageCount = imageCount,
                DocumentCount = docCount
            };
        }

        // Add new equipment with images and documents
        public async Task<ResponseVM<string>> AddEquipmentAsync(EquipmentVM vm, Guid currentUserId)
        {
            // Save vehicle images
            if (vm.VehicleImages?.Any(f => f != null) == true)
            {
                vm.VehicleUrl = await SaveImagesAsync(vm.VehicleImages!, "uploads/vehicles");
            }

            var eq = new Equipment
            {
                Id = Guid.NewGuid(),
                Vin = vm.Vin,
                LicensePlate = vm.LicensePlate,
                IssuingProvince = vm.IssuingProvince,
                BusNumber = vm.BusNumber,
                Manufacturer = vm.Manufacturer,
                Model = vm.Model,
                Year = vm.Year,
                Color = vm.Color,
                SeatingCapacity = vm.SeatingCapacity ?? 10,
                VehicleType = vm.VehicleType,
                VehicleUrl = vm.VehicleUrl,
                Length = vm.Length ?? throw new ArgumentNullException(nameof(vm.Length)),
                Height = vm.Height ?? throw new ArgumentNullException(nameof(vm.Height)),
                GrossVehicleWeight = vm.GrossVehicleWeight ?? throw new ArgumentNullException(nameof(vm.GrossVehicleWeight)),
                IsActive = vm.IsActive,
                DeactivationReason = vm.DeactivationReason,
                DeactivatedAt = vm.DeactivatedAt,
                CreatedBy = currentUserId,
                UpdatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Equipment.Add(eq);

            // Save equipment documents
            if (vm.Documents != null && vm.Documents.Any())
            {
                string uploadsRoot = Path.Combine(_env.WebRootPath, "uploads/vehicle-documents");
                Directory.CreateDirectory(uploadsRoot);

                foreach (var doc in vm.Documents)
                {
                    if (doc.File != null && !string.IsNullOrEmpty(doc.Description))
                    {
                        string uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(doc.File.FileName)}";
                        string filePath = Path.Combine(uploadsRoot, uniqueName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await doc.File.CopyToAsync(stream);
                        }

                        doc.FilePath = $"/uploads/vehicle-documents/{uniqueName}";
                        doc.UploadedAt = DateTime.UtcNow;

                        // Save each document to DB
                        var dbDoc = new EquipmentDocument
                        {
                            Id = Guid.NewGuid(),
                            EquipmentId = eq.Id,
                            FilePath = doc.FilePath,
                            Description = doc.Description,
                            UploadedAt = doc.UploadedAt
                        };

                        _db.EquipmentDocuments.Add(dbDoc);
                    }
                }

                await _db.SaveChangesAsync();
            }

            await _db.SaveChangesAsync();
            return new ResponseVM<string> { IsSuccess = true, Message = "Equipment Added!" };
        }

        // Update existing equipment, including adding new images and documents
        public async Task<ResponseVM<string>> UpdateEquipmentAsync(EquipmentVM vm, Guid currentUserId)
        {
            var eq = await _db.Equipment.FindAsync(vm.Id);
            if (eq is null)
                return new ResponseVM<string> { IsSuccess = false, Message = "Equipment Not Found" };

            // Update vehicle images
            if (vm.VehicleImages?.Any(f => f != null) == true)
            {
                var newImages = await SaveImagesAsync(vm.VehicleImages!, "uploads/vehicles");

                if (!string.IsNullOrEmpty(eq.VehicleUrl))
                    eq.VehicleUrl = eq.VehicleUrl + "," + newImages; // append new images
                else
                    eq.VehicleUrl = newImages;
            }

            // Update main details
            eq.Vin = vm.Vin;
            eq.LicensePlate = vm.LicensePlate;
            eq.IssuingProvince = vm.IssuingProvince;
            eq.BusNumber = vm.BusNumber;
            eq.Manufacturer = vm.Manufacturer;
            eq.Model = vm.Model;
            eq.Year = vm.Year;
            eq.Color = vm.Color;
            eq.SeatingCapacity = vm.SeatingCapacity ?? 10;
            eq.VehicleType = vm.VehicleType;
            eq.Length = vm.Length ?? throw new ArgumentNullException(nameof(vm.Length));
            eq.Height = vm.Height ?? throw new ArgumentNullException(nameof(vm.Height));
            eq.GrossVehicleWeight = vm.GrossVehicleWeight ?? throw new ArgumentNullException(nameof(vm.GrossVehicleWeight));
            eq.IsActive = vm.IsActive;
            eq.DeactivationReason = vm.DeactivationReason;
            eq.DeactivatedAt = vm.DeactivatedAt;
            eq.UpdatedBy = currentUserId;
            eq.UpdatedAt = DateTime.UtcNow;

            // Update documents
            if (vm.Documents != null && vm.Documents.Any())
            {
                foreach (var doc in vm.Documents)
                {
                    if (doc.File != null)
                    {
                        var savedPath = await SaveFileAsync(doc.File, "uploads/vehicle-documents");
                        var newDoc = new EquipmentDocument
                        {
                            Id = Guid.NewGuid(),
                            EquipmentId = eq.Id,
                            FilePath = $"/{savedPath}",
                            Description = doc.Description,
                            UploadedAt = DateTime.UtcNow
                        };
                        _db.EquipmentDocuments.Add(newDoc);
                    }
                }
            }

            await _db.SaveChangesAsync();
            return new ResponseVM<string> { IsSuccess = true, Message = "Equipment Updated Successfully!" };
        }

        // Save multiple images and return their URLs as a comma-separated string
        private async Task<string?> SaveImagesAsync(List<IFormFile?> files, string folder)
        {
            if (files == null || files.Count == 0)
                return null;

            string uploadsRoot = Path.Combine(_env.WebRootPath, folder);
            Directory.CreateDirectory(uploadsRoot);

            var fileNames = new List<string>();

            foreach (var file in files.Where(f => f != null))
            {
                string uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file!.FileName)}";
                string filePath = Path.Combine(uploadsRoot, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                fileNames.Add($"{folder}/{uniqueFileName}");
            }

            return string.Join(",", fileNames); // or return list instead of string if needed
        }

        // Save a single file and return its URL
        private async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            string uploadsRoot = Path.Combine(_env.WebRootPath, folder); 
            Directory.CreateDirectory(uploadsRoot);

            string uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            string filePath = Path.Combine(uploadsRoot, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"{folder}/{uniqueFileName}";
        }

        // Delete an equipment image by its URL
        public async Task<ResponseVM<string>> DeleteEquipmentImageAsync(Guid equipmentId, string imageUrl)
        {
            var eq = await _db.Equipment.FindAsync(equipmentId);
            if (eq == null)
                return new ResponseVM<string> { IsSuccess = false, Message = "Equipment Not Found" };

            var images = eq.VehicleUrl?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();

            // Normalize: remove leading slash and lowercase
            var normalizedUrl = imageUrl.TrimStart('/').ToLower();

            var matchedImage = images.FirstOrDefault(i => i.TrimStart('/').Equals(normalizedUrl, StringComparison.OrdinalIgnoreCase));
            if (matchedImage == null)
                return new ResponseVM<string> { IsSuccess = false, Message = "Image not found" };

            images.Remove(matchedImage);

            // Delete file from disk
            var filePath = Path.Combine(_env.WebRootPath, matchedImage.Replace("/", "\\"));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            eq.VehicleUrl = string.Join(",", images);
            await _db.SaveChangesAsync();

            return new ResponseVM<string> { IsSuccess = true, Message = "Image Deleted Successfully" };
        }

        // Soft delete an equipment document by its ID
        public async Task<ResponseVM<string>> DeleteEquipmentDocumentAsync(Guid documentId)
        {
            var doc = await _db.EquipmentDocuments.FindAsync(documentId);
            if (doc == null)
                return new ResponseVM<string> { IsSuccess = false, Message = "Document Not Found" };

            // Delete file from disk
            //if (!string.IsNullOrEmpty(doc.FilePath))
            //{
            //    var filePath = Path.Combine(_env.WebRootPath, doc.FilePath.Replace("/", "\\"));
            //    if (System.IO.File.Exists(filePath))
            //        System.IO.File.Delete(filePath);
            //}

            doc.IsDeleted = true;
            _db.EquipmentDocuments.Update(doc);
            await _db.SaveChangesAsync();

            return new ResponseVM<string> { IsSuccess = true, Message = "Document Deleted Successfully" };
        }

        // Validate image file type and size
        private bool ValidateImage(IFormFile file, out string errorMessage)
        {
            errorMessage = string.Empty;
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var maxFileSizeMB = 5;

            if (file == null)
            {
                errorMessage = "No file uploaded.";
                return false;
            }

            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
            {
                errorMessage = "Only JPG, PNG, GIF, or WebP files are allowed.";
                return false;
            }

            if (file.Length > maxFileSizeMB * 1024 * 1024)
            {
                errorMessage = $"File size must be less than {maxFileSizeMB} MB.";
                return false;
            }

            return true;
        }

        // Soft delete equipment by setting IsDeleted flag
        public async Task<ResponseVM<string>> DeleteEquipmentAsync(Guid id, Guid currentUserId)
        {
            var eq = await _db.Equipment.FindAsync(id);
            if (eq is null) return new ResponseVM<string> { IsSuccess = false, Message = "Equipment Not Found" };
            eq.IsDeleted = true;
            eq.UpdatedBy = currentUserId;
            await _db.SaveChangesAsync();
            return new ResponseVM<string> { IsSuccess = true, Message = "Equipment Deleted Successfully!" };
        }
    }
}
