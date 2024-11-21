namespace StaffSyncWeb.Models
{
    public class LogEntry
    {
        public int log_id { get; set; }
        public string log { get; set; }
        public DateTime date { get; set; }
        public string employee_email { get; set; }
    }
}
