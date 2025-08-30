// Path: ViewModels/ManageFixtureViewModel.cs
using BussinessCupApi.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BussinessCupApi.ViewModels
{
    public class ManageFixtureViewModel
    {
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public LeagueType LeagueType { get; set; }
        public List<Team> Teams { get; set; } = new List<Team>();
        public int TeamSquadCount { get; set; } // Ligin gerektirdiği kadro sayısı
        public DateTime LeagueStartDate { get; set; }
        public DateTime LeagueEndDate { get; set; }

        // Fikstür oluşturma ayarları için kullanıcı girdileri
        [Display(Name = "Grup Sayısı")]
        // Grup sayısı, lig tipine göre gösterilip gizlenebilir veya zorunlu olabilir.
        // Şimdilik nullable (int?) yapalım ki formda boş bırakılabilsin.
        public int? NumberOfGroups { get; set; }

        [Display(Name = "Maçlar Rövanşlı Oynansın")]
        public bool PlayReciprocalMatches { get; set; } = true; // Varsayılan olarak true (karşılıklı)

        [Display(Name = "Haftalar Otomatik Oluşturulsun")]
        public bool AutoGenerateWeeks { get; set; } = true; // Varsayılan olarak işaretli

        // Mevcut fikstür bileşenleri hakkında bilgi
        public List<Week> ExistingWeeks { get; set; } = new List<Week>();
        public List<Group> ExistingGroups { get; set; } = new List<Group>();

        // Fikstürün daha önce oluşturulup oluşturulmadığını anlamak için basit bir kontrol
        public bool IsFixtureGenerated => ExistingWeeks.Any() || (LeagueType == LeagueType.GroupLeagueThenKnockout && ExistingGroups.Any());

        // Takım sayısı (View'da göstermek için kolaylık)
        public int NumberOfTeams => Teams.Count;
    }
    // ~/ViewModels/GenerateFixtureInputModel.cs (Örnek Tanım)
    public class GenerateFixtureInputModel
    {
        [Required]
        public int LeagueId { get; set; }

        [Required]
        public string SeasonName { get; set; }

        [Required]
        public List<int> SelectedTeamIds { get; set; } = new List<int>();

        public bool PlayReturnMatches { get; set; } // Rövanşlı mı?

        public int NumberOfGroups { get; set; } // 0 veya 1 ise grupsuz, >1 ise gruplu
        public bool IsGroupLeague { get; set; } // YENİ: Gruplu lig mi?
         

        // Gruplu sistemde takımların gruplara nasıl dağıtılacağı bilgisi de eklenebilir.
        // Örneğin: public Dictionary<int, List<int>> GroupTeamAssignments { get; set; } // Key: GroupIndex, Value: TeamIds
        // Şimdilik basit tutalım ve takımların eşit dağıtıldığını varsayalım.
    }
} 


