using System.ComponentModel.DataAnnotations;

public class NotificationViewModel
{ 

    [Required(ErrorMessage = "Türkçe başlık zorunludur")]
    [Display(Name = "Türkçe Başlık")]
    public string TitleTr { get; set; } 

    [Required(ErrorMessage = "Türkçe mesaj zorunludur")]
    [Display(Name = "Türkçe Mesaj")]
    public string MessageTr { get; set; }
} 