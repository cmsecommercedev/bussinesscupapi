using BussinessCupApi.Models;
using System.Text.Json.Serialization;

namespace BussinessCupApi.ViewModels.MatchSquad
{
    public class DashBoardMatchSquadViewModel
    {
        public int MatchId { get; set; }
        public int HomeTeamId { get; set; }
        public int AwayTeamId { get; set; }
        public Team HomeTeam { get; set; }
        public Team AwayTeam { get; set; }
        public string AwayTeamFormationImg { get; set; }
        public string HomeTeamFormationImg { get; set; }
        public List<DashBoardPlayerSquadViewModel> HomeSquad { get; set; }
        public List<DashBoardPlayerSquadViewModel> AwaySquad { get; set; }
    }

    public class DashBoardSaveMatchSquadViewModel
    {
        public int MatchId { get; set; }
        public List<DashBoardPlayerSquadViewModel> HomeSquad { get; set; }
        public List<DashBoardPlayerSquadViewModel> AwaySquad { get; set; }
    }

    public class DashBoardPlayerSquadViewModel
    {
        public int PlayerId { get; set; }
        public int TeamId { get; set; }
        public int ShirtNumber { get; set; }
        public bool IsStarting11 { get; set; }
        public bool IsSubstitute { get; set; }
        public string? PlayerImage { get; set; }
        public string PlayerName { get; set; }
        public float? TopPosition { get; set; } // Decimal veya Float kullanmak daha doğru
        public float? LeftPosition { get; set; }
    }
}
