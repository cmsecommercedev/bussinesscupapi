using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessCupApi.Models
{
    public class LeagueRule
    {
        [Key]
        public int Id { get; set; }

        public int LeagueId { get; set; }
        // public virtual League League { get; set; }  <-- kaldır

        public int SeasonId { get; set; }
        // public virtual Season Season { get; set; }  <-- kaldır

        public int TeamId { get; set; }
        // public virtual Team Team { get; set; }  <-- kaldır

        [Required]
        public RuleType RuleType { get; set; }

        public int? Point { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }



    public enum RuleType
    {
        [Display(Name = "Puan Silme")]
        PointDeduction = 0,
        [Display(Name = "Küme Düşürme")]
        Relegation = 1,
        [Display(Name = "Diğer")]
        Other = 2
    }
}
