using BussinessCupApi.Models;

namespace BussinessCupApi.ViewModels
{
    public class MatchNewsIndexViewModel
    {
        public MatchNewsInputModel NewMatchNews { get; set; } = new MatchNewsInputModel();
        public List<MatchNewsWithContentDto> MatchNewsList { get; set; } = new List<MatchNewsWithContentDto>();
        public string Culture { get; set; } = "tr";
    }

    public class MatchNewsWithContentDto
    {
        public MatchNews MatchNews { get; set; }
        public MatchNewsContent? Content { get; set; }
    }
} 