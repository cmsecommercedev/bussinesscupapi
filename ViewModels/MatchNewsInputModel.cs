namespace BussinessCupApi.ViewModels
{
    public class MatchNewsInputModel
    {
        public int? Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string DetailsTitle { get; set; }
        public string Details { get; set; }
        public int? CityID { get; set; }
        public bool IsMainNews { get; set; }
        public string? MatchNewsMainPhoto { get; set; }
        public bool Published { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? TeamID { get; set; }
    }
}
