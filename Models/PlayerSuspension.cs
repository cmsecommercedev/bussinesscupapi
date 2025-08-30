using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessCupApi.Models
{
    public enum SuspensionType
    {
        [Display(Name = "Sarı Kart Sınırı")]
        YellowCardLimit,
        [Display(Name = "Kırmızı Kart")]
        RedCard,
        [Display(Name = "Disiplin Kurulu")]
        Disciplinary
        // Gerektiğinde başka türler eklenebilir
    }

    public class PlayerSuspensions
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PlayerSuspensionID { get; set; }

        [Required]
        public int WeekID { get; set; }
        public virtual Week Week { get; set; } // Navigation property

        [Required]
        public int PlayerID { get; set; }
        [ForeignKey("PlayerID")]

        public virtual Player Player { get; set; } // Navigation property

        // Lig ve Sezon ID'lerini de eklemek sorgulama kolaylığı sağlayabilir
        [Required]
        public int LeagueID { get; set; }
        [ForeignKey("LeagueID")]
        public virtual League League { get; set; } // Navigation property

        [Required]
        public int SeasonID { get; set; }
        [ForeignKey("SeasonID")]
        public virtual Season Season { get; set; } // Navigation property


        [Required(ErrorMessage = "Ceza türü zorunludur")]
        [Display(Name = "Ceza Türü")]
        public SuspensionType SuspensionType { get; set; }

        [Required(ErrorMessage = "Ceza süresi (maç sayısı) zorunludur")]
        [Range(1, 99, ErrorMessage = "Ceza süresi en az 1 maç olmalıdır")]
        [Display(Name = "Ceza Süresi (Maç)")]
        public int GamesSuspended { get; set; }

        [MaxLength(500)]
        [Display(Name = "Notlar")]
        public string? Notes { get; set; } // Opsiyonel not alanı

        // Kaydın ne zaman oluşturulduğunu bilmek faydalı olabilir
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    }
} 