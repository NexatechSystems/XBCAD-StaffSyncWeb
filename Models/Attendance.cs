namespace StaffSyncWeb.Models
{
    public class Attendance
    {
        public string employee_email { get; set; }
        public string name { get; set; }
        public string surname { get; set; }
        public bool clocked_in { get; set; }
    }
}
