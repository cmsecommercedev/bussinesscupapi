namespace BussinessCupApi.ViewModels
{
    public class WeekBestTeamViewModel
    {
        public int LeagueID { get; set; }
        public string LeagueName { get; set; }
        public int SeasonID { get; set; }
        public string SeasonName { get; set; }
        public int WeekID { get; set; }
        public string WeekName { get; set; }
        public int? BestPlayerID { get; set; }
        public int? BestTeamID { get; set; }
        public List<PlayerSelectionViewModel> AvailablePlayers { get; set; } = new List<PlayerSelectionViewModel>();
        public List<PlayerSelectionViewModel> SelectedPlayers { get; set; } = new List<PlayerSelectionViewModel>();
    }

    public class PlayerSelectionViewModel
    {
        public int PlayerID { get; set; }
        public string FullName { get; set; }
        public string Position { get; set; }
        public string TeamName { get; set; }
        public string PlayerImage { get; set; }
        public bool IsSelected { get; set; }
        public bool IsBestPlayer { get; set; }
    }

    public class RequestWeekBestTeamViewModel
    {
        public int LeagueID { get; set; }
        public int SeasonID { get; set; }
        public int WeekID { get; set; }
        public int? BestPlayerID { get; set; }
        public int? BestTeamID { get; set; }

        public List<int> SelectedPlayers { get; set; } // Değişiklik burada
    }

}