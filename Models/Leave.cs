namespace StaffSyncWeb.Models
{
    public class Leave
    {
        public int leave_id {  get; set; }
        public string employee_email { get; set;}
        public string leave_type { get; set;}
        public DateTime start_date { get; set;}
        public DateTime end_date { get; set; }
        public string information {  get; set;}
        public bool approved { get; set;}
    }
}
