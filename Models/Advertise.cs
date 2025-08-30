using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessCupApi.Models
{
    public class Advertise
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Reklam adı zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Reklam Adı/Başlığı")]
        public string Name { get; set; }

        [StringLength(300)] // Yol uzunluğunu ihtiyaca göre ayarlayın
        [Display(Name = "Resim Dosya Yolu")]
        public string? ImagePath { get; set; } // Resmin sunucudaki göreceli yolunu saklar

        [StringLength(100)]
        [Display(Name = "Kategori")]
        public string? Category { get; set; }

        [StringLength(200)]
        [Display(Name = "Alternatif Metin")]
        public string? AltText { get; set; }

        [StringLength(500)]
        [DataType(DataType.Url)]
        [Display(Name = "Bağlantı Adresi (URL)")]
        public string? LinkUrl { get; set; }

        [Display(Name = "Aktif Mi?")]
        public bool IsActive { get; set; } = true; // Varsayılan olarak aktif

        [Display(Name = "Yüklenme Tarihi")]
        public DateTime UploadDate { get; set; } = DateTime.UtcNow; 

        [Display(Name = "Şehir")]
        public int? CityId { get; set; } // Şehir sponsoru ise şehir seçilecek

        [ForeignKey("CityId")]
        public City? City { get; set; }
    }
} 