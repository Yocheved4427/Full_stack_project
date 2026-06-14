namespace DTOs
{
    public class DashboardStatsDTO
    {
        public int TotalOrders { get; set; }
        public int TotalRevenue { get; set; }
        public int TotalCustomers { get; set; }
        public int PendingOrders { get; set; }
        public List<MonthStatDTO> SalesByMonth { get; set; } = new();
        public List<StatusStatDTO> OrdersByStatus { get; set; } = new();
    }

    public class MonthStatDTO
    {
        public int Month { get; set; }
        public int Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class StatusStatDTO
    {
        public string Status { get; set; } = "";
        public int Count { get; set; }
    }
}
