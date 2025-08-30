using static BussinessCupApi.Models.Match;

namespace BussinessCupApi.Models.Dtos
{
    public class LeagueWeeksDto
    {
        public int LeagueID { get; set; }
        public string Name { get; set; }
        public int? GroupId { get; set; }
        public List<LeagueWeekSeasonDto> Seasons { get; set; }
    }

    public class LeagueWeekSeasonDto
    {
        public int SeasonID { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<LeagueWeekDto> Weeks { get; set; }
    }

    public class LeagueWeekDto
    {
        public int WeekID { get; set; }
        public int WeekNumber { get; set; }
        public string WeekName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrentWeek { get; set; } 
        public List<DateGroupedMatchesDto> DateGroupedMatches { get; set; } = new List<DateGroupedMatchesDto>();
    }

    public class DateGroupedMatchesDto
    {
        public string Date { get; set; } // Format: "dd.MM"
        public DateTime FullDate { get; set; }
        public List<LeagueWeekGroupMatchesDto> GroupedMatches { get; set; } = new List<LeagueWeekGroupMatchesDto>();
        public List<LeagueWeekMatchDto> UngroupedMatches { get; set; } = new List<LeagueWeekMatchDto>();
    }

    public class LeagueWeekGroupMatchesDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public List<LeagueWeekMatchDto> Matches { get; set; } = new List<LeagueWeekMatchDto>();
    }

    public class LeagueWeekMatchDto
    {
        public int MatchID { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public string HomeTeamLogo { get; set; }
        public string AwayTeamLogo { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public DateTime MatchDate { get; set; } 
        public int HomeTeamId { get; set; }
        public MatchStatus MatchStatus { get; set; }
        public int AwayTeamId { get; set; }
        public int? GroupId { get; set; }
    }
} 