namespace BussinessCupApi.Dtos
{
    public class ValuablePlayerDto
    {
        public int PlayerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Position { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string TeamIcon { get; set; }
        public string PlayerIcon { get; set; }
        public int? PlayerValue { get; set; }
        public string PreferredFoot { get; set; }
    }
}