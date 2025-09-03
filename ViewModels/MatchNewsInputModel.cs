using BussinessCupApi.Models;

namespace BussinessCupApi.ViewModels
{
    public class MatchNewsInputModel
    { 
        // Türkçe
        public string Title_tr { get; set; }
        public string Subtitle_tr { get; set; }
        public string DetailsTitle_tr { get; set; }
        public string Details_tr { get; set; }

        // English
        public string Title_en { get; set; }
        public string Subtitle_en { get; set; }
        public string DetailsTitle_en { get; set; }
        public string Details_en { get; set; }

        // Romanian
        public string Title_ro { get; set; }
        public string Subtitle_ro { get; set; }
        public string DetailsTitle_ro { get; set; }
        public string Details_ro { get; set; }

        // Russian
        public string Title_ru { get; set; }
        public string Subtitle_ru { get; set; }
        public string DetailsTitle_ru { get; set; }
        public string Details_ru { get; set; }

        public NewsCategory Category { get; set; } = NewsCategory.News;

        public bool IsMainNews { get; set; }
        public string? MatchNewsMainPhoto { get; set; }
        public bool Published { get; set; }
        public DateTime CreatedDate { get; set; } 
    }
}
