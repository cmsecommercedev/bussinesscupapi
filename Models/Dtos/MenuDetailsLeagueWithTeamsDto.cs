namespace BussinessCupApi.Models.Dtos
{

    // Yeni DTO sınıfları
    public class MenuDetailsLeagueDto
    {
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public string LeagueIcon { get; set; }
    }

    // MenuDetailsTeamDto sınıfını genişletelim
    public class MenuDetailsTeamDto
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string TeamIcon { get; set; }
        public string Manager { get; set; }
        public bool IsFavourite { get; set; } // yeni eklendi
        public bool TeamIsFree { get; set; } // yeni eklendi

    }
}
