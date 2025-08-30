using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessCupApi.Models
{
    public class Match
    {
        public int MatchID { get; set; }
        public int LeagueID { get; set; }
        public int WeekID { get; set; }
        public int? GroupID { get; set; }
        public int HomeTeamID { get; set; }
        public int AwayTeamID { get; set; }
        public DateTime MatchDate { get; set; }
         public DateTime MatchStarted { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public string? MatchURL { get; set; }
        public bool IsPlayed { get; set; }
        public int? ManOfTheMatchID { get; set; }
        [ForeignKey("LeagueID")]
        public virtual League League { get; set; }
        [ForeignKey("WeekID")]
        public virtual Week Week { get; set; }
        public virtual Group Group { get; set; }
        [ForeignKey("HomeTeamID")]
        public virtual Team HomeTeam { get; set; }
        [ForeignKey("AwayTeamID")]
        public virtual Team AwayTeam { get; set; }
        public virtual MatchStatus Status { get; set; }
        public virtual ICollection<Goal> Goals { get; set; }
        public virtual ICollection<Card> Cards { get; set; }
        public virtual Player? ManOfTheMatch { get; set; }
        public virtual ICollection<MatchSquad> MatchSquads { get; set; }


        public Match()
        {
            Goals = new HashSet<Goal>();
            Cards = new HashSet<Card>();
        }


        public enum MatchStatus
        {
            [Display(Name = "Başlamadı")]
            NotStarted = 0,
            [Display(Name = "Maç Başladı")]
            Started = 1,

            [Display(Name = "İlk Yarı")]
            FirstHalf = 2,

            [Display(Name = "Devre Arası")]
            HalfTime = 3,

            [Display(Name = "İkinci Yarı")]
            SecondHalf = 4,

            [Display(Name = "Bitti")]
            Finished = 5
        }

    }
}