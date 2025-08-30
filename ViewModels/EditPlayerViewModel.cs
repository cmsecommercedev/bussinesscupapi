using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.ComponentModel.DataAnnotations;

public class EditPlayerViewModel
{
    public int PlayerID { get; set; }

    [Required(ErrorMessage = "Ad alanı zorunludur")]
    [Display(Name = "Ad")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Soyad alanı zorunludur")]
    [Display(Name = "Soyad")]
    public string LastName { get; set; }

    [Required(ErrorMessage = "Mevki alanı zorunludur")]
    [Display(Name = "Mevki")]
    public string Position { get; set; }

    [Required(ErrorMessage = "Forma numarası zorunludur")]
    [Display(Name = "Forma Numarası")]
    [Range(1, 99, ErrorMessage = "Forma numarası 1-99 arasında olmalıdır")]
    public int Number { get; set; }

    [Required(ErrorMessage = "Doğum tarihi zorunludur")]
    [Display(Name = "Doğum Tarihi")]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [Display(Name = "Uyruk")]
    public string? Nationality { get; set; }

    [Display(Name = "Fotoğraf")]
    public IFormFile? IconFile { get; set; }

    public string? ExistingIcon { get; set; }
    public int TeamID { get; set; }

    public List<SelectListItem> Positions { get; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "Kaleci", Text = "Kaleci" },
        new SelectListItem { Value = "Defans", Text = "Defans" },
        new SelectListItem { Value = "Orta Saha", Text = "Orta Saha" },
        new SelectListItem { Value = "Forvet", Text = "Forvet" }
    };

    public List<SelectListItem> Nationalities { get; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "Türkiye", Text = "Türkiye" },
        new SelectListItem { Value = "Almanya", Text = "Almanya" },
        new SelectListItem { Value = "İngiltere", Text = "İngiltere" },
        new SelectListItem { Value = "İspanya", Text = "İspanya" },
        new SelectListItem { Value = "İtalya", Text = "İtalya" },
        new SelectListItem { Value = "Fransa", Text = "Fransa" },
        new SelectListItem { Value = "Portekiz", Text = "Portekiz" },
        new SelectListItem { Value = "Hollanda", Text = "Hollanda" },
        new SelectListItem { Value = "Belçika", Text = "Belçika" },
        new SelectListItem { Value = "Hırvatistan", Text = "Hırvatistan" },
        // Daha fazla ülke eklenebilir
    };
}

// Path: ViewModels/EditMatchInputModel.cs

public class EditMatchInputModel
{
    [Required]
    public int MatchId { get; set; }

    [Required(ErrorMessage = "Ev sahibi takım seçilmelidir.")]
    [Display(Name = "Ev Sahibi Takım")]
    public int HomeTeamId { get; set; }

    [Required(ErrorMessage = "Deplasman takımı seçilmelidir.")]
    [Display(Name = "Deplasman Takımı")]
    public int AwayTeamId { get; set; }

    [Required(ErrorMessage = "Maç tarihi gereklidir.")]
    [Display(Name = "Maç Tarihi")]
    [DataType(DataType.DateTime)] // Veya sadece Date ise DataType.Date
    public DateTime MatchDate { get; set; }

}