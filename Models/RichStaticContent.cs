using System;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessCupApi.Models
{
	public class RichStaticContent
	{
		public int Id { get; set; }
		public int? CategoryId { get; set; } // reference to RichContentCategory
		public string? Culture { get; set; } // e.g., "tr", "en", "de", "ru" (or any you add)
		public int? SeasonId { get; set; } // optional season reference
		public string? MediaUrl { get; set; } // optional
		public string? ProfileImageUrl { get; set; } // video preview image (optional)
		public string? EmbedVideoUrl { get; set; } // optional
		public string? Text { get; set; } // rich/plain text
		public string? AltText { get; set; } // image alt text
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
		public bool Published { get; set; } = true;

		[NotMapped]
		public IFormFile? MediaFile { get; set; } // upload (optional)
		
		[NotMapped]
		public IFormFile? ProfileImageFile { get; set; } // profile image upload (optional)

		// Navigation properties
		public virtual RichContentCategory? Category { get; set; }
		public virtual Season? Season { get; set; }
	}
}