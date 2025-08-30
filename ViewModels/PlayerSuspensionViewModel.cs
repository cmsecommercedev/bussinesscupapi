using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BussinessCupApi.Models; // Enum için eklendi
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BussinessCupApi.ViewModels
{
    public class PlayerSuspensionViewModel
    {
        // Dropdownlar için
        public int SelectedLeagueId { get; set; }
        public int SelectedSeasonId { get; set; }
        public int SelectedWeekId { get; set; }

        public SelectList? Leagues { get; set; }
        // Sezonlar ve Haftalar AJAX ile yüklenecek

        // Yeni Ceza Ekleme Formu için
        [Required(ErrorMessage = "Oyuncu seçimi zorunludur")]
        public int SelectedPlayerId { get; set; }
        public SelectList? Players { get; set; } // AJAX ile yüklenecek

        [Required(ErrorMessage = "Ceza türü zorunludur")]
        public SuspensionType SuspensionType { get; set; }

        [Required(ErrorMessage = "Ceza süresi zorunludur")]
        [Range(1, 99)]
        public int GamesSuspended { get; set; } = 1; // Varsayılan 1 maç

        public string? Notes { get; set; }

        // Mevcut Cezaları Listelemek için
        public List<ExistingSuspensionViewModel> ExistingSuspensions { get; set; } = new List<ExistingSuspensionViewModel>();
    }

    public class ExistingSuspensionViewModel
    {
        public int PlayerSuspensionID { get; set; }
        public int PlayerID { get; set; }
        public string PlayerFullName { get; set; } = string.Empty;
        public string SuspensionTypeDisplay { get; set; } = string.Empty; // Enum'ın DisplayName'i
        public SuspensionType SuspensionType { get; set; }
        public int GamesSuspended { get; set; }
        public string? Notes { get; set; }
    }
} 