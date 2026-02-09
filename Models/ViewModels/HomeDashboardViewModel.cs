namespace Cartrade.Models.ViewModels;

public class HomeDashboardViewModel
{
    public bool IsAuthenticated { get; set; }

    public int TotalVehicles { get; set; }
    public int AwaitingInspection { get; set; }
    public int AwaitingFinance { get; set; }
    public int ReadyForSale { get; set; }
    public int ActiveAuctions { get; set; }
}
