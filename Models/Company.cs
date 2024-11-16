namespace StaffSyncWeb.Models
{
    public class Company
    {
        public int company_id { get; set; }
        public string month { get; set; }
        public int employees_hired { get; set; }
        public int employees_fired { get; set; }
        public decimal turnover_rate { get; set; }
        public int profits { get; set; }
        public int expenses { get; set; }
        public decimal net_profit_loss { get; set; }
        public int current_headcount { get; set; }
    }

}
