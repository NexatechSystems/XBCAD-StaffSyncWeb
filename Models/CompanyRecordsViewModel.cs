namespace StaffSyncWeb.Models
{
    public class CompanyRecordsViewModel
    {
        public IEnumerable<Company> CompanyRecords { get; set; }
        public Company NewCompanyRecord { get; set; } = new Company();
    }
}
