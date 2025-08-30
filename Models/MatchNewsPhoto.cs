namespace BussinessCupApi.Models
{
    public class MatchNewsPhoto
    {
        // Birincil anahtar (Primary Key)
        public int Id { get; set; }

        // Fotoğrafın URL'si veya dosya yolu
        public string PhotoUrl { get; set; } // Veya PhotoPath, saklama yönteminize göre değişir

        // Yabancı anahtar (Foreign Key) - Hangi habere ait olduğunu belirtir
        public int MatchNewsId { get; set; }

        // İlişkili haber için navigation property
        public virtual MatchNews MatchNews { get; set; }
    }
}
