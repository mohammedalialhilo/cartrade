namespace Cartrade.Models.ViewModels;

public class AdminUserListItemViewModel
{
    public string Email { get; set; } = string.Empty;
    public string Roles { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
}
