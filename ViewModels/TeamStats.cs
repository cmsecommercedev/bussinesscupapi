using BussinessCupApi.Models;

namespace BussinessCupApi.ViewModels
{
    public class TeamStats
    {
        public int TeamID { get; set; }
        public string TeamName { get; set; }
        public int Played { get; set; }
        public int Won { get; set; }
        public int Drawn { get; set; }
        public int Lost { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }

        public int Points
        {
            get { return Won * 3 + Drawn; }
        }

        public int GoalDifference
        {
            get { return GoalsFor - GoalsAgainst; }
        }

        public List<Player> Players { get; set; } = new List<Player>();
    }
}