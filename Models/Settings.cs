using System.ComponentModel.DataAnnotations;

namespace BussinessCupApi.Models
{
    public class Settings
    {
        public int ID { get; set; }

        [Display(Name = "iOS Versiyon")]
        public string iOSVersion { get; set; }

        [Display(Name = "Android Versiyon")]
        public string AndroidVersion { get; set; }

        [Display(Name = "Zorunlu Güncelleme")]
        public bool ForceUpdate { get; set; }

        [Display(Name = "Uygulama Durduruldu")]
        public bool AppStop { get; set; }

        [Display(Name = "Durdurma Mesajı")]
        public string? AppStopMessage { get; set; }

        public DateTime LastUpdated { get; set; }
        [Display(Name = "Turnuva Başlangıç Tarihi")]
        public DateTime? TournamentStartDate { get; set; }
    }
}