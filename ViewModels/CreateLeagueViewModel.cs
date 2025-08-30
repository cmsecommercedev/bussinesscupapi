using BussinessCupApi.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

public class CreateLeagueViewModel
{
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

    [Display(Name = "Lig Logosu")]
    public IFormFile? LogoFile { get; set; } // Logo yükleme alanı

    [Required(ErrorMessage = "Şehir seçimi zorunludur.")]
    [Display(Name = "Şehir")]
    public int CityID { get; set; } // Seçilen şehir ID'si

    [Required(ErrorMessage = "Sahadaki Oyuncu Sayısı zorunludur.")]
    [Display(Name = "Sahadaki Oyuncu Sayısı")]
    public int TeamSquadCount { get; set; }

    public IEnumerable<SelectListItem> Cities { get; set; } // Şehir listesi
    public List<LeagueRankingStatusInput> RankingStatuses { get; set; } = new List<LeagueRankingStatusInput>();

}

public class LeagueRankingStatusInput
{
    public int OrderNo { get; set; }
    public string ColorCode { get; set; }
    public string Description { get; set; }
}
public class CreateLeagueInputModel
{
    [Required(ErrorMessage = "Lig adı zorunludur.")]
    [StringLength(100)]
    public string Name { get; set; }

    [Required(ErrorMessage = "Lig türü seçimi zorunludur.")]
    public LeagueType LeagueType { get; set; }

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Bitiş tarihi zorunludur.")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    public IFormFile? LogoFile { get; set; }

    [Required(ErrorMessage = "Şehir seçimi zorunludur.")]
    public int CityID { get; set; }

    [Required(ErrorMessage = "Maç Başlangıç Sayısı zorunludur.")]
    [Display(Name = "Maç Başlangıç Sayısı")]
    public int TeamSquadCount { get; set; }
    public List<LeagueRankingStatusInput> RankingStatuses { get; set; } = new List<LeagueRankingStatusInput>();

}
