using System.ComponentModel.DataAnnotations;

namespace BussinessCupApi.ViewModels
{
    public class LeagueViewModel
    {
        public int? LeagueID { get; set; }

        [Required(ErrorMessage = "Lig adı gereklidir")]
        [Display(Name = "Lig Adı")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Sezon gereklidir")]
        [Display(Name = "Sezon")]
        public string Season { get; set; }

        [Required(ErrorMessage = "Başlangıç tarihi gereklidir")]
        [Display(Name = "Başlangıç Tarihi")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Display(Name = "Bitiş Tarihi")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;
    }
} 