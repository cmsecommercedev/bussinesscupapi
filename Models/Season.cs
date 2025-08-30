using BussinessCupApi.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Season
{
    public int SeasonID { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Name { get; set; }
    
    public bool IsActive { get; set; }
    
    public int LeagueID { get; set; }
    [ForeignKey("LeagueID")]
    public virtual League League { get; set; }
} 