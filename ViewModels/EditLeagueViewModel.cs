using Microsoft.AspNetCore.Mvc.Rendering;
using BussinessCupApi.Models;
using System.ComponentModel.DataAnnotations;


public class EditLeagueViewModel
{
    public int LeagueID { get; set; }

    [Required(ErrorMessage = "Lig adı zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Lig Adı")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Lig türü seçimi zorunludur.")]
    [Display(Name = "Lig Türü")]
    public LeagueType LeagueType { get; set; }

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Başlangıç Tarihi")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Bitiş tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Bitiş Tarihi")]
    public DateTime EndDate { get; set; }

    [Display(Name = "Yeni Lig Logosu")]
    public IFormFile? NewLogoFile { get; set; } // Yeni logo yükleme alanı

    public string? ExistingLogoPath { get; set; } // Mevcut logo yolunu view'e taşımak için

    [Required(ErrorMessage = "Şehir seçimi zorunludur.")]
    [Display(Name = "Şehir")]
    public int CityID { get; set; } // Seçilen şehir ID'si

    [Required(ErrorMessage = "Maç Başlangıç Sayısı zorunludur.")]
    [Display(Name = "Maç Başlangıç Sayısı")]
    public int TeamSquadCount { get; set; }

    public IEnumerable<SelectListItem> Cities { get; set; } // Şehir listesi

    public List<RankingStatusViewModel> RankingStatuses { get; set; } = new();

}
public class RankingStatusViewModel
{
    public int OrderNo { get; set; }
    public string ColorCode { get; set; }
    public string Description { get; set; }
}


public class EditLeagueInputViewModel
{
    public int LeagueID { get; set; }

    [Required(ErrorMessage = "Lig adı zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Lig Adı")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Lig türü seçimi zorunludur.")]
    [Display(Name = "Lig Türü")]
    public LeagueType LeagueType { get; set; }

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Başlangıç Tarihi")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Bitiş tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Bitiş Tarihi")]
    public DateTime EndDate { get; set; }

    [Display(Name = "Yeni Lig Logosu")]
    public IFormFile? NewLogoFile { get; set; } // Yeni logo yükleme alanı

    public string? ExistingLogoPath { get; set; } // Mevcut logo yolunu view'e taşımak için

    [Required(ErrorMessage = "Şehir seçimi zorunludur.")]
    [Display(Name = "Şehir")]
    public int CityID { get; set; } // Seçilen şehir ID'si

    [Required(ErrorMessage = "Maç Başlangıç Sayısı zorunludur.")]
    [Display(Name = "Maç Başlangıç Sayısı")]
    public int TeamSquadCount { get; set; }
    public List<RankingStatusViewModel> RankingStatuses { get; set; } = new();

}