using System.ComponentModel.DataAnnotations;

public class NotificationViewModel
{
    [Required(ErrorMessage = "İngilizce başlık zorunludur")]
    [Display(Name = "İngilizce Başlık")]
    public string TitleEn { get; set; }

    [Required(ErrorMessage = "Türkçe başlık zorunludur")]
    [Display(Name = "Türkçe Başlık")]
    public string TitleTr { get; set; }

    [Required(ErrorMessage = "İngilizce mesaj zorunludur")]
    [Display(Name = "İngilizce Mesaj")]
    public string MessageEn { get; set; }

    [Required(ErrorMessage = "Türkçe mesaj zorunludur")]
    [Display(Name = "Türkçe Mesaj")]
    public string MessageTr { get; set; }
} 