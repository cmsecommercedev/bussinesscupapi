namespace BussinessCupApi.Models.Api
{
    public class TranslateMatchNewsRequest
    {
        public string Text { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
        public string? SourceLanguage { get; set; }
    }

    public class TranslateMatchNewsMultipleRequest
    {
        public string Text { get; set; } = string.Empty;
        public List<string> TargetLanguages { get; set; } = new List<string>();
        public string? SourceLanguage { get; set; }
    }
} 