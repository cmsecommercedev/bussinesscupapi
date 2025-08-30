using static BussinessCupApi.Models.Match;

namespace BussinessCupApi.DTOs
{
    public class ActualMatchesResponseDto
    {
        public int LeagueID { get; set; }
        public string LeagueName { get; set; }

        public string LeagueIcon { get; set; }
        public int WeekID { get; set; }
        public string WeekName { get; set; }
        public List<ActualMatchDayDto> Days { get; set; }
    }

    public class ActualMatchDayDto
    {
        public DateTime Date { get; set; }
        public List<ActualMatchDto> Matches { get; set; }
    }

    public class ActualMatchDto
    {
        public int MatchID { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public string HomeTeamLogo { get; set; }
        public string AwayTeamLogo { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public DateTime MatchDate { get; set; }

        public DateTime MatchStarted { get; set; }
        public MatchStatus MatchStatus { get; set; }     
        public bool IsPlayed { get; set; }
        public int HomeTeamId { get; set; }
        public int AwayTeamId { get; set; } 
    }
}
