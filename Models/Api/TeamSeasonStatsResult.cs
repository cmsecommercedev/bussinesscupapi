namespace BussinessCupApi.Models.Api
{
    public class TeamSeasonStatsResult
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string TeamIcon { get; set; }
        public int SeasonId { get; set; }
        public string SeasonName { get; set; }
        public string Manager { get; set; } // Manager bilgisi

        public int Played { get; set; }
        public int Won { get; set; }
        public int Drawn { get; set; }
        public int Lost { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int Points { get; set; }
        public int GoalDifference => GoalsFor - GoalsAgainst;
        public bool IsFavorite { get; set; } // Favori bilgisi

    }
}
