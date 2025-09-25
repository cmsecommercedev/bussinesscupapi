using System;

namespace BussinessCupApi.Models
{
	public class PhotoGallery
	{
		public int Id { get; set; }
		public string Category { get; set; } // e.g., "2019", "2020" ... "2025"
		public string FileName { get; set; }
		public string FilePath { get; set; } // relative path under wwwroot
		public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
	}
}


