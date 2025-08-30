using System;

namespace BussinessCupApi.ViewModels.Captain
{
    public class CaptainMatchViewModel
    {
        public int MatchID { get; set; }
        public string HomeTeamName { get; set; }
        public string AwayTeamName { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public DateTime MatchDate { get; set; }
        public bool IsPlayed { get; set; }
        public string LeagueName { get; set; }
        public string SeasonName { get; set; }
        public int WeekNumber { get; set; }
        public bool IsCaptainHomeTeam { get; set; } // Kaptanın takımı ev sahibi mi?

        public string OpponentName => IsCaptainHomeTeam ? AwayTeamName : HomeTeamName;
        public string CaptainTeamScore => IsCaptainHomeTeam ? (HomeScore?.ToString() ?? "-") : (AwayScore?.ToString() ?? "-");
        public string OpponentScore => IsCaptainHomeTeam ? (AwayScore?.ToString() ?? "-") : (HomeScore?.ToString() ?? "-");
         public string ScoreDisplay => IsPlayed ? $"{CaptainTeamScore} - {OpponentScore}" : "-";
        public string VersusText => IsCaptainHomeTeam ? $"{HomeTeamName} vs {AwayTeamName}" : $"{AwayTeamName} vs {HomeTeamName}";
    }
} 