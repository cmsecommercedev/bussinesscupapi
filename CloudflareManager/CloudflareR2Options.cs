namespace BussinessCupApi.CloudflareManager
{
    public class CloudflareR2Options
    {
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string AccountId { get; set; }
        public string BucketName { get; set; }
        public string Region { get; set; } = "auto";
        public string Endpoint { get; set; }
        public string PublicURL { get; set; }
    }

}
