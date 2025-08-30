using System.ComponentModel.DataAnnotations;

namespace BussinessCupApi.ViewModels
{
    public class CreateGroupViewModel
    {
        public int LeagueID { get; set; }
        public int SeasonID { get; set; }
        [Required(ErrorMessage = "Grup adı zorunludur")]
        public string GroupName { get; set; }
        public string Description { get; set; }
    }
}
