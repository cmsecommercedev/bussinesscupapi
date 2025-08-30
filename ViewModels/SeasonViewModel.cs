using System.ComponentModel.DataAnnotations;

public class SeasonViewModel
{
    public int SeasonID { get; set; }

    [Required(ErrorMessage = "Sezon adı zorunludur")]
    [Display(Name = "Sezon Adı")]
    [StringLength(50)]
    public string Name { get; set; }

    [Display(Name = "Aktif Sezon")]
    public bool IsActive { get; set; }

    [Required(ErrorMessage = "Lig seçimi zorunludur")]
    [Display(Name = "Lig")]
    public int LeagueID { get; set; }
} 