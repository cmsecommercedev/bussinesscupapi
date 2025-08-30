using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessCupApi.Models
{
    public class WeekBestTeams
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int WeekBestTeamID { get; set; }

        [Required]
        public int WeekID { get; set; }
        public Week Week { get; set; }

        [Required]
        public int LeagueID { get; set; }
        [ForeignKey("LeagueID")]
        public League League { get; set; }

        [Required]
        public int SeasonID { get; set; }
        [ForeignKey("SeasonID")]
        public Season Season { get; set; }

        public int BestPlayerID { get; set; }
        public int BestTeamID { get; set; }
        [ForeignKey("BestPlayerID")]

        public Player BestPlayer { get; set; }
        [ForeignKey("BestTeamID")]

        public Team BestTeam { get; set; }

        public List<WeekBestTeamPlayers> Players { get; set; } = new List<WeekBestTeamPlayers>();
    }

    public class WeekBestTeamPlayers
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int WeekBestTeamPlayerID { get; set; }

        [Required]
        public int WeekBestTeamID { get; set; }
        public WeekBestTeams WeekBestTeam { get; set; }

        [Required]
        public int PlayerID { get; set; }
        public Player Player { get; set; }
        public int OrderNumber { get; set; }
    }
} 