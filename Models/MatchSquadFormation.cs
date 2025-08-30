namespace BussinessCupApi.Models
{
    public class MatchSquadFormation
    {
        public int MatchSquadFormationID { get; set; }
        public int MatchID { get; set; } 
        public int TeamID { get; set; }
        public string? FormationImage { get; set; }
    }
}
