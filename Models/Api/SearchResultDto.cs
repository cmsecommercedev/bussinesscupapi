namespace BussinessCupApi.Models.Api
{
    public class SearchResultDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LogoUrl { get; set; }
        public string Type { get; set; } // "player", "team", "league"
    }
}
