using System; // DateTime için
using System.Collections.Generic;
using System.ComponentModel; // DefaultValue için

namespace BussinessCupApi.Models
{
    public class MatchNews
    {
        // Birincil anahtar (Primary Key)
        public int Id { get; set; }

        // Haber Başlığı
        public string Title { get; set; } = string.Empty; // Null olmaması için

        // Haber Alt Başlığı
        public string Subtitle { get; set; } = string.Empty; // Null olmaması için
        public string? MatchNewsMainPhoto { get; set; } = string.Empty; // Null olmaması için

        // Detay Başlığı
        public string DetailsTitle { get; set; } = string.Empty; // Null olmaması için

        // Detay İçeriği
        public string Details { get; set; } = string.Empty; // Null olmaması için

        public int? MatchID { get; set; }

        // Şehir bilgisi
        public int? CityID { get; set; }
        public virtual City City { get; set; }

        // Takım bilgisi
        public int? TeamID { get; set; }
        public virtual Team Team { get; set; }

        public bool IsMainNews { get; set; }

        // Yayınlanma Durumu
        [DefaultValue(true)] // Varsayılan olarak true
        public bool Published { get; set; } = true;

        // Oluşturulma Tarihi
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow; // Varsayılan olarak şu anki UTC zamanı

        // İlişkili fotoğraflar için navigation property
        // Bir haberin birden fazla fotoğrafı olabilir (One-to-Many ilişki)
        public virtual ICollection<MatchNewsPhoto> Photos { get; set; } = new List<MatchNewsPhoto>();
    }
}
