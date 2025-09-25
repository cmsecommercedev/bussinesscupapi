using System;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessCupApi.Models
{
	public class RichStaticContent
	{
		public int Id { get; set; }
		public string? CategoryCode { get; set; } // e.g., "flags", "home_hero"
		public string? Culture { get; set; } // e.g., "tr", "en", "de", "ru" (or any you add)
		public string? ImageUrl { get; set; } // optional
		public string? VideoUrl { get; set; } // optional
		public string? Text { get; set; } // rich/plain text
		public string? AltText { get; set; } // image alt text
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
		public bool Published { get; set; } = true;

		[NotMapped]
		public IFormFile? ImageFile { get; set; } // upload (optional)
	}
}