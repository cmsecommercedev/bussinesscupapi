using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BussinessCupApi.Models
{
	public class Story
	{
		public int Id { get; set; }

		[Required]
		[MaxLength(200)]
		public string Title { get; set; } = string.Empty;

		public bool Published { get; set; } = true;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

		public ICollection<StoryContent> Contents { get; set; } = new List<StoryContent>();
	}

	public class StoryContent
	{
		public int Id { get; set; }
		public int StoryId { get; set; }
		public Story Story { get; set; }

		[Required]
		[MaxLength(500)]
		public string MediaUrl { get; set; } = string.Empty;

		// e.g. image/png, image/jpeg, video/mp4
		[MaxLength(100)]
		public string? ContentType { get; set; }

		public int DisplayOrder { get; set; } = 0;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}


