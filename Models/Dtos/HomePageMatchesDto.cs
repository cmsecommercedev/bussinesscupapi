using System;
using System.Collections.Generic;

namespace BussinessCupApi.Models.Dtos
{
    public class HomePageLeagueOverviewDto
    {
        public int LeagueID { get; set; }
        public string Name { get; set; }
        public string LeagueIcon { get; set; }
        public LeagueType LeagueType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public HomePageWeekOverviewDto CurrentWeek { get; set; }
        public bool IsNextWeek { get; set; }
        public List<HomePageWeekOverviewDto> Weeks { get; set; }
    }

    public class HomePageWeekOverviewDto
    {
        public int WeekID { get; set; }
        public int SeasonID { get; set; }
        public string SeasonName { get; set; }
        public string LeagueName { get; set; }
        public int WeekNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<HomePageGroupMatchesDto> GroupedMatches { get; set; } = new List<HomePageGroupMatchesDto>();
        public List<HomePageMatchesDto> UngroupedMatches { get; set; } = new List<HomePageMatchesDto>();
    }

    public class HomePageGroupMatchesDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public List<HomePageMatchesDto> Matches { get; set; } = new List<HomePageMatchesDto>();
    }

    public class HomePageMatchesDto
    {
        public int MatchID { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public string HomeTeamLogo { get; set; }
        public string AwayTeamLogo { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public DateTime MatchDate { get; set; }
        public bool IsPlayed { get; set; }
        public int HomeTeamId { get; set; }
        public int AwayTeamId { get; set; }
        public int? GroupId { get; set; }
    }
} 