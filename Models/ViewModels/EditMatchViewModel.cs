using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

public class EditMatchViewModel
{
    public int MatchId { get; set; }

    [Required(ErrorMessage = "Ev sahibi takım seçilmelidir.")]
    [Display(Name = "Ev Sahibi Takım")]
    public int HomeTeamId { get; set; }

    [Required(ErrorMessage = "Deplasman takımı seçilmelidir.")]
    [Display(Name = "Deplasman Takımı")]
    public int AwayTeamId { get; set; }

    [Required(ErrorMessage = "Maç tarihi gereklidir.")]
    [Display(Name = "Maç Tarihi")]
    [DataType(DataType.Date)]
    public DateTime MatchDate { get; set; }


    public List<SelectListItem> Teams { get; set; }
} 