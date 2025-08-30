using System.ComponentModel.DataAnnotations;

namespace BussinessCupApi.Models.ViewModels
{

    // CreateAdvertiseViewModel tanımı (varsayılan)
    // Eğer bu model yoksa veya farklıysa Controller'daki Create action'ı düzenlenmeli.
    public class CreateAdvertiseViewModel
    {
        [Required(ErrorMessage = "Reklam adı zorunludur.")]
        [StringLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Lütfen bir resim dosyası seçin.")]
        public IFormFile ImageFile { get; set; } // Formdaki input adı ile eşleşmeli

        [StringLength(100)]
        public string? Category { get; set; }

        [StringLength(200)]
        public string? AltText { get; set; }

        [StringLength(500)]
        [DataType(DataType.Url)]
        public string? LinkUrl { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
