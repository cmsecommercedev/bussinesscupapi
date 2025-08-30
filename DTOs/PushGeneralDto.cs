// Path: ViewModels/PushGeneralDto.cs
namespace BussinessCupApi.DTOs
{
    // DTO'lar:
    public class PushGeneralDto
    {
        public string Title { get; set; }
        public string Detail { get; set; }
    }
    public class PushTeamDto
    {
        public int TeamId { get; set; }
        public string Title { get; set; }
        public string Detail { get; set; }
    }
}
