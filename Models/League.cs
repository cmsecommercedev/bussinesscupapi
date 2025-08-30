using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http; // IFormFile için gerekli

namespace BussinessCupApi.Models
{
    public class League
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LeagueID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        public LeagueType LeagueType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }

        [StringLength(255)] // Yol uzunluğu için sınır ekleyebilirsiniz
        public string? LogoPath { get; set; } // Logo dosya yolunu saklamak için

        public int CityID { get; set; } // İl için foreign key
        public int TeamSquadCount { get; set; } // İl için foreign key
        public virtual City City { get; set; } // Navigation property

        public virtual ICollection<Team> Teams { get; set; }
        public virtual ICollection<Week> Weeks { get; set; } = new List<Week>();
        public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
        public virtual ICollection<Season> Seasons { get; set; }
    }
    public enum LeagueType
    {
        Knockout=1,        // Elemeli
        League=2,      // Lig usulü
        LeagueThenKnockout=3, // Lig sonra elemeli
        GroupLeagueThenKnockout=4 // Gruplu lig sonra elemeli
    }


}