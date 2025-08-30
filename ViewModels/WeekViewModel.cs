using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

public class WeekViewModel
{
    public int? WeekID { get; set; }
    
    [Required(ErrorMessage = "Lig seçimi zorunludur")]
    [Display(Name = "Lig")]
    public int LeagueID { get; set; }
    public int SeasonID { get; set; }

    [Required(ErrorMessage = "Hafta numarası zorunludur")]
    [Display(Name = "Hafta")]
    [Range(1, 52, ErrorMessage = "Hafta numarası 1-52 arasında olmalıdır")]
    public int WeekNumber { get; set; }

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur")]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Başlangıç Tarihi")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Bitiş tarihi zorunludur")]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Bitiş Tarihi")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }
    [Required(ErrorMessage = "Hafta adı zorunludur")]
    [Display(Name = "Hafta Adı")]
    public string WeekName { get; set; }

    [Required(ErrorMessage = "Hafta türü zorunludur")]
    [Display(Name = "Hafta Türü")]
    public string WeekStatus { get; set; }

    public List<SelectListItem> Leagues { get; set; } = new List<SelectListItem>();
    public List<WeekMatchViewModel> Matches { get; set; } = new List<WeekMatchViewModel>();
} 

public class WeekMatchViewModel
{
    public int? MatchID { get; set; }
    public int HomeTeamID { get; set; }
    public int AwayTeamID { get; set; }
    public DateTime MatchDate { get; set; }
    public List<SelectListItem> Teams { get; set; } = new List<SelectListItem>();
} 