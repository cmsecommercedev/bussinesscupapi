using System;
using System.Collections.Generic;

namespace BussinessCupApi.Models
{
    public enum NewsCategory
    {
        Story = 0,
        News = 1
    }

    public class MatchNews
    {
        public int Id { get; set; }

        // Ana fotoğraf
        public string? MatchNewsMainPhoto { get; set; } = string.Empty;

        // Ana haber mi?
        public bool IsMainNews { get; set; }

        // Yayınlanma durumu
        public bool Published { get; set; } = true;

        // Kategori
        public NewsCategory Category { get; set; } = NewsCategory.News;

        // Oluşturulma tarihi
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Fotoğraflar
        public virtual ICollection<MatchNewsPhoto> Photos { get; set; } = new List<MatchNewsPhoto>();

        // Çok dilli içerikler
        public virtual ICollection<MatchNewsContent> Contents { get; set; } = new List<MatchNewsContent>();
    }
}