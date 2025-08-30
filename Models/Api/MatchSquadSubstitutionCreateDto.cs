namespace BussinessCupApi.Models.Api
{
    public class MatchSquadSubstitutionCreateDto
    {
        public int MatchID { get; set; }
        public int PlayerInID { get; set; }
        public int PlayerOutID { get; set; }
        public int Minute { get; set; }
    }
}
