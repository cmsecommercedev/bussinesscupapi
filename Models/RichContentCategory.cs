using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BussinessCupApi.Models
{
	public class RichContentCategory
	{
		public int Id { get; set; }
		
		[Required]
		[StringLength(100)]
		public string Name { get; set; }
		
		[StringLength(50)]
		public string Code { get; set; }
		
		[StringLength(500)]
		public string? Description { get; set; }
		
		public bool IsActive { get; set; } = true;
		
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
		
		// Navigation property
		public virtual ICollection<RichStaticContent> RichStaticContents { get; set; } = new List<RichStaticContent>();
	}
}
