using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

public class TeamViewModel
{
    public int TeamID { get; set; }

    [Required(ErrorMessage = "Takım adı zorunludur")]
    [Display(Name = "Takım Adı")]
    public string Name { get; set; }

    [Display(Name = "Şehir")]
    public string? City { get; set; }

    [Display(Name = "Stadyum")]
    public string? Stadium { get; set; }

    [Display(Name = "Teknik Direktör")]
    public string? Manager { get; set; }

    [Display(Name = "Logo")]
    public IFormFile? Logo { get; set; }

    public string? ExistingLogoUrl { get; set; }

    [Display(Name = "TeamPassword")]
    public string? TeamPassword { get; set; }    
    [Display(Name = "CityID")]
    [Required(ErrorMessage = "Şehir seçimi zorunludur")]
    public int CityID { get; set; }

    public IEnumerable<SelectListItem>? AvailableCities { get; set; }
} 