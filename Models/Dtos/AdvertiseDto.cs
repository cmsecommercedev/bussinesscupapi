using System;

namespace BussinessCupApi.Models.Dtos
{
    public class AdvertiseDto
    {
        public int Id { get; set; } // Modeldeki Id alanı ile eşleşti
        public string Name { get; set; } // Modeldeki Name alanı ile eşleşti
        public string LinkUrl { get; set; } // Modeldeki LinkUrl alanı ile eşleşti
        public bool IsActive { get; set; }
        public string Category { get; set; }
        public string AltText { get; set; } // Modeldeki AltText alanı eklendi

        // İstemci tarafında doğrudan kullanılabilecek Data URL formatı
        public string ImageDataUrl { get; set; } 
        public int? CityId { get; set; }
        public string? CityName { get; set; }

        // İsteğe bağlı: Yüklenme tarihi de eklenebilir
        // public DateTime UploadDate { get; set; }
    }
} 