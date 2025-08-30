using Microsoft.AspNetCore.Mvc.Rendering;
using BussinessCupApi.Models;

namespace BussinessCupApi.ViewModels
{
    public class DashboardViewModel
    {
        public List<DashboardLeagueViewModel> Leagues { get; set; }
        public Dictionary<int, LeagueStats> LeagueStandings { get; set; }

        public List<City> Cities { get; set; } // Şehir listesi

        public DashboardViewModel()
        {
            Leagues = new List<DashboardLeagueViewModel>();
            LeagueStandings = new Dictionary<int, LeagueStats>();
        }
    }

    public class DashboardLeagueViewModel
    {
        public int LeagueID { get; set; }
        public int CityID { get; set; }
        public string Name { get; set; }
             public LeagueType LeagueType { get; set; }
        public List<DashboardSeasonViewModel> Seasons { get; set; }
        public List<DashboardWeekViewModel> Weeks { get; set; }

        public DashboardLeagueViewModel()
        {
            Weeks = new List<DashboardWeekViewModel>();
        }
    }

    public class DashboardWeekViewModel
    {
        public int WeekID { get; set; }
        public int WeekNumber { get; set; }
        public string WeekName { get; set; }
        public string WeekStatus { get; set; }
        public int SeasonID { get; set; }
        public List<DashboardMatchViewModel> Matches { get; set; }
    }

    public class DashboardMatchViewModel
    {
        public int MatchID { get; set; }
        public DateTime MatchDate { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public bool IsPlayed { get; set; }
        public TeamBasicViewModel HomeTeam { get; set; }
        public TeamBasicViewModel AwayTeam { get; set; }
    }

    public class TeamBasicViewModel
    {
        public int TeamID { get; set; }
        public string Name { get; set; }
    }

    public class DashboardSeasonViewModel
    {
        public int SeasonID { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }
} 