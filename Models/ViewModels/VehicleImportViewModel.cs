using System.ComponentModel.DataAnnotations;

namespace Cartrade.Models.ViewModels;

public class VehicleImportViewModel
{
    [Required]
    [Display(Name = "Semikolon-separerad fil")]
    public IFormFile? File { get; set; }

    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Warnings { get; set; } = new();
}
