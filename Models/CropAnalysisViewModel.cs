using System.ComponentModel.DataAnnotations;

namespace Agriculture.Models;

public class CropAnalysisViewModel
{
    [Required(ErrorMessage = "Please upload a crop leaf image.")]
    [Display(Name = "Crop Leaf Image")]
    public IFormFile? ImageFile { get; set; }

    [Required(ErrorMessage = "Please select a crop type.")]
    [Display(Name = "Crop Type")]
    public string CropType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your country.")]
    [Display(Name = "Country")]
    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;

    [Display(Name = "State / Region")]
    [MaxLength(100)]
    public string? State { get; set; }
}
