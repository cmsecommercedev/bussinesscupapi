using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

public class MatchViewModel
{
    public int? MatchID { get; set; }

    [Required(ErrorMessage = "Lig seçimi zorunludur")]
    [Display(Name = "Lig")]
    public int LeagueID { get; set; }

    [Required(ErrorMessage = "Hafta seçimi zorunludur")]
    [Display(Name = "Hafta")]
    public int WeekID { get; set; }

    [Required(ErrorMessage = "Ev sahibi takım seçimi zorunludur")]
    [Display(Name = "Ev Sahibi")]
    public int HomeTeamID { get; set; }

    [Required(ErrorMessage = "Deplasman takımı seçimi zorunludur")]
    [Display(Name = "Deplasman")]
    public int AwayTeamID { get; set; }

    [Required(ErrorMessage = "Maç tarihi zorunludur")]
    [Display(Name = "Maç Tarihi")]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-ddTHH:mm}")]
    public DateTime MatchDate { get; set; }
    public int? GroupID { get; set; }
    public SelectList? Groups { get; set; }


    [Display(Name = "Ev Sahibi Skor")]
    public int? HomeScore { get; set; }

    [Display(Name = "Deplasman Skor")]
    public int? AwayScore { get; set; }

    [Display(Name = "Maç Oynandı")]
    public bool IsPlayed { get; set; }

    public List<SelectListItem> Weeks { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> Teams { get; set; } = new List<SelectListItem>();
} 