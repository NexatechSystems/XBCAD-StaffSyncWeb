namespace StaffSyncWeb.Models
{
    public class JobListing
    {
        public int listing_id { get; set; }
        public string job_title { get; set; }
        public string job_description { get; set; }
        public string salary { get; set; }
        public string benefits { get; set; }
    }
}
