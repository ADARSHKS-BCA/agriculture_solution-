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

    [Required(ErrorMessage = "Please enter your city or use current location.")]
    [Display(Name = "City")]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
