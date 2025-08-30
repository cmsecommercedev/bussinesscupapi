using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BussinessCupApi.ViewModels.Captain
{
    public class CaptainSquadViewModel
    {
        public int MatchId { get; set; }
        public string MatchDescription { get; set; }
        public int CaptainTeamId { get; set; }
        public string CaptainTeamName { get; set; }

        // Takımdaki tüm oyuncular (seçim için)
        public List<CaptainPlayerViewModel> AvailablePlayers { get; set; } = new List<CaptainPlayerViewModel>();

        // Maç için seçilmiş mevcut kadro (GET isteğinde doldurulur)
        public List<CaptainPlayerSquadViewModel> CurrentSquad { get; set; } = new List<CaptainPlayerSquadViewModel>();

        // Formdan POST edilecek seçili oyuncu bilgileri
        // View tarafında bu listeyi dolduracak inputlar oluşturulacak
        public List<CaptainPlayerSquadViewModel> SelectedSquadPlayers { get; set; } = new List<CaptainPlayerSquadViewModel>();

    }

    public class CaptainPlayerViewModel
    {
        public int PlayerID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Number { get; set; }
        public string FullName => $"{FirstName} {LastName}";
    }

     // Hem mevcut kadroyu göstermek hem de POST edilecek veriyi tutmak için
    public class CaptainPlayerSquadViewModel
    {
        public int PlayerId { get; set; }
        public int TeamId { get; set; } // Validasyon için gerekebilir
        public int ShirtNumber { get; set; }
        public bool IsStarting11 { get; set; }
        public bool IsSubstitute { get; set; } // Gerekirse eklenebilir
    }
} 