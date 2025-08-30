using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // IFormFile için

namespace BussinessCupApi.Models // Veya ViewModel'ler için ayrı bir klasör/namespace
{
    public class CreateAdvertiseViewModel
    {
        [Required(ErrorMessage = "Reklam adı zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Reklam Adı/Başlığı")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Lütfen bir resim dosyası seçin.")]
        [Display(Name = "Resim Dosyası")]
        public IFormFile ImageFile { get; set; }

        [StringLength(200)]
        [Display(Name = "Alternatif Metin")]
        public string? AltText { get; set; }
        [StringLength(200)]
        [Display(Name = "Alternatif Metin")]
        public string? Category { get; set; }

        [StringLength(500)]
        [DataType(DataType.Url)]
        [Display(Name = "Bağlantı Adresi (URL)")]
        public string? LinkUrl { get; set; }

        [Display(Name = "Aktif Mi?")]
        public bool IsActive { get; set; } = true;
        [Display(Name = "Şehir")]
        public int CityID { get; set; }
    }
} 