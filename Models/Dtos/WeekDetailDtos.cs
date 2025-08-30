using BussinessCupApi.Extensions;
using BussinessCupApi.Models; // SuspensionType ve Player için
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // DisplayAttribute için

namespace BussinessCupApi.Models.Dtos // Projenizin DTO namespace'i
{
    // Haftanın Cezalı Oyuncuları için DTO
    public class SuspensionDto
    {
        public int PlayerSuspensionID { get; set; }
        public int PlayerID { get; set; }
        public string PlayerFullName { get; set; }
        public int TeamID { get; set; } // Takım ID'si eklendi
        public string TeamName { get; set; } // Takım adı eklendi
        public string PlayerIcon { get; set; } // Takım adı eklendi
        public SuspensionType SuspensionType { get; set; }

        [Display(Name = "Ceza Türü")]
        public string SuspensionTypeDisplay => SuspensionType.GetDisplayName(); // Enum'ın Display adını kullan
        public int GamesSuspended { get; set; }
        public string? Notes { get; set; }
    }

    // Haftanın Takımı için Ana DTO
    public class WeekBestTeamDto
    {
        public int WeekBestTeamID { get; set; }
        public int WeekID { get; set; }
        public int LeagueID { get; set; }
        public int SeasonID { get; set; }
        public BestPlayerInfoDto BestPlayer { get; set; }
        public BestTeamInfoDto BestTeam { get; set; }
        public List<SelectedPlayerInfoDto> SelectedPlayers { get; set; } = new List<SelectedPlayerInfoDto>();
    }

    // Haftanın En İyi Oyuncusu için Alt DTO
    public class BestPlayerInfoDto
    {
        public int PlayerID { get; set; }
        public string FullName { get; set; }
        public int TeamID { get; set; }
        public string TeamName { get; set; }
        public string PlayerIcon { get; set; } // Oyuncu ikonu eklendi
        public string Position{ get; set; } // Oyuncu ikonu eklendi
    }

     // Haftanın Takımı için Alt DTO
    public class BestTeamInfoDto
    {
        public int TeamID { get; set; }
        public string TeamName { get; set; }
        public string TeamManager { get; set; }
        public string TeamLogo { get; set; } // Takım logosu eklendi
    }


    // Haftanın 11'ine Seçilen Oyuncu için Alt DTO
    public class SelectedPlayerInfoDto
    {
        public int PlayerID { get; set; }
        public string FullName { get; set; }
        public int TeamID { get; set; }
        public string TeamName { get; set; }
        public string PlayerIcon { get; set; } // Oyuncu ikonu eklendi
        public string Position { get; set; }
        public int OrderNumber { get; set; } // Sıralama numarası
    }

}