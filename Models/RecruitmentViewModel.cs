namespace StaffSyncWeb.Models
{
    public class RecruitmentViewModel
    {
        public IEnumerable<JobListing> JobListings { get; set; }
        public IEnumerable<Applicant> Applicants { get; set; }
    }
}
