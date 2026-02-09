using System.ComponentModel.DataAnnotations;

namespace Cartrade.Models.ViewModels;

public class AdminUserEditViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    [Display(Name = "Name")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string Role { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    [Display(Name = "New password")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "New password and confirmation do not match.")]
    [Display(Name = "Confirm new password")]
    public string? ConfirmNewPassword { get; set; }
}
