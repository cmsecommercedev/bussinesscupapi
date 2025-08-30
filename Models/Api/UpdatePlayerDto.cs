namespace BussinessCupApi.Models.Api
{
    public class UpdatePlayerDto
    {
        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public int? CityID { get; set; }
        public IFormFile? ProfilePicture { get; set; }
        // Player'a özel alanlar
        public string? Nationality { get; set; }
        public string? Position { get; set; }
        public int? Number { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Height { get; set; }
        public string? Weight { get; set; }
        public string? PreferredFoot { get; set; }
    }
     
        public class UpdatePlayerSubscriptionDto
        {
            public bool? isSubscribed { get; set; }
            public DateTime? SubscriptionExpireDate { get; set; }
        }
    
}
