namespace Cartrade.Models.ViewModels;

public class AdminUserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Roles { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
}
