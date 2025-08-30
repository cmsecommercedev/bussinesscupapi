using System.ComponentModel.DataAnnotations;

namespace BussinessCupApi.Models
{
    public class MatchNewsContent
    {
        public int Id { get; set; }

        // Hangi habere ait
        public int MatchNewsId { get; set; }
        public virtual MatchNews MatchNews { get; set; }

        // Dil kodu (ör: "tr", "en", "de", "fr")
        [MaxLength(5)]
        public string Culture { get; set; }

        // Başlık
        public string Title { get; set; }

        // Alt başlık
        public string Subtitle { get; set; }

        // Detay başlığı
        public string DetailsTitle { get; set; }

        // Detay içeriği
        public string Details { get; set; }
    }
}