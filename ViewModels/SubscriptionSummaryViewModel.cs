namespace BussinessCupApi.ViewModels
{

    // ViewModels/SubscriptionSummaryViewModel.cs
    public class SubscriptionSummaryViewModel
    {
        public int ToplamOyuncu { get; set; }
        public int UyeOlanOyuncu { get; set; }
        public int UyeOlmayanOyuncu { get; set; }
        public List<SubscriptionPlayerViewModel> Oyuncular { get; set; }
    }

    public class SubscriptionPlayerViewModel
    {
        public int PlayerID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Position { get; set; }
        public int? Number { get; set; }
        public int TeamID { get; set; }
        public string TakimAdi { get; set; }
        public bool? isSubscribed { get; set; }
        public DateTime? SubscriptionExpireDate { get; set; }
        public string UserEmail { get; set; }
        public string Icon { get; set; } // <-- yeni eklendi
    }
}
