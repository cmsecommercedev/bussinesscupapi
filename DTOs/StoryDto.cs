public class StoryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool Published { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Type { get; set; } = string.Empty; // "video" | "image" | "mixed"
    public List<StoryContentDto> Contents { get; set; } = new();
}

public class StoryContentDto
{
    public int Id { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
