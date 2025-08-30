using BussinessCupApi.Models;

namespace BussinessCupApi.ViewModels
{
    public class MatchDetailsViewModel
    {
        public Match Match { get; set; }
        public IEnumerable<Player> HomePlayers { get; set; }
        public IEnumerable<Player> AwayPlayers { get; set; }
    }
} 