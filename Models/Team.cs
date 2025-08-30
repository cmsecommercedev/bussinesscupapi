using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace BussinessCupApi.Models
{
    public class Team
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TeamID { get; set; }

        [Required(ErrorMessage = "Takım adı zorunludur")]
        public string Name { get; set; }
         
        public string? Stadium { get; set; }
        public string? LogoUrl { get; set; }
        public string? Manager { get; set; }
        public string? TeamPassword { get; set; }
        public int CityID { get; set; }
        public bool TeamIsFree { get; set; }       

        // Navigation properties
        public virtual ICollection<Player> Players { get; set; }
        public virtual ICollection<Match> HomeMatches { get; set; } = new List<Match>();
        public virtual ICollection<Match> AwayMatches { get; set; } = new List<Match>();
        public virtual ICollection<Goal> Goals { get; set; } = new List<Goal>(); 
    }
} 