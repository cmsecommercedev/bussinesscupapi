using System;

namespace BussinessCupApi.Models
{
	public class StaticKeyValue
	{
		public int Id { get; set; }
		public string Key { get; set; }
		public string Value { get; set; }
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
	}
}