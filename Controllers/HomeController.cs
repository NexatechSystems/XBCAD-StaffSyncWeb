using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StaffSyncWeb.Models;
using System.Data;
using System.Diagnostics;

namespace StaffSyncWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        public async Task<IActionResult> Dashboard()
        {
            using (var connection = CreateConnection())
            {
                string sql = "SELECT month, employees_hired, employees_fired, profits, expenses, current_headcount FROM Company ORDER BY company_id";

                var companyData = await connection.QueryAsync<Company>(sql);
               
                ViewBag.Months = companyData.Select(d => d.month).ToList();
                ViewBag.EmployeesHired = companyData.Select(d => d.employees_hired).ToList();
                ViewBag.EmployeesFired = companyData.Select(d => d.employees_fired).ToList();
                ViewBag.Profits = companyData.Select(d => d.profits).ToList();
                ViewBag.Expenses = companyData.Select(d => d.expenses).ToList();
                ViewBag.Headcounts = companyData.Select(d => d.current_headcount).ToList();
            }

            return View();
        }


        public async Task<IActionResult> Recruitment()
        {
            using (var connection = CreateConnection())
            {              
                string jobListingSql = "SELECT listing_id, job_title, job_description, salary, benefits" +
                                       " FROM Listings";

                var jobListings = await connection.QueryAsync<JobListing>(jobListingSql);

                // Fetch Applicants
                string applicantSql = "SELECT applicant_email, listing_id, name, surname" +
                                      " FROM Applicants";

                var applicants = await connection.QueryAsync<Applicant>(applicantSql);

                // Pass data to the view using a ViewModel
                var model = new RecruitmentViewModel
                {
                    JobListings = jobListings,
                    Applicants = applicants
                };

                return View(model);
            }
        }

        public IActionResult NewListing()
        {
           return View();
        }

        public async Task<IActionResult> CompanyRecords()
        {
            using (var connection = CreateConnection())
            {
                string sql = "SELECT * FROM Company";
                var records = await connection.QueryAsync<Company>(sql);

                var viewModel = new CompanyRecordsViewModel
                {
                    CompanyRecords = records
                };

                return View(viewModel);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCompanyRecord(Company newCompanyRecord)
        {
            using (var connection = CreateConnection())
            {
                string sql = @"
            INSERT INTO Company (month, employees_hired, employees_fired, turnover_rate, profits, expenses, net_profit_loss, current_headcount)
            VALUES (@Month, @EmployeesHired, @EmployeesFired, @TurnoverRate, @Profits, @Expenses, @NetProfitLoss, @CurrentHeadcount)";

                await connection.ExecuteAsync(sql, new
                {
                    newCompanyRecord.month,
                    newCompanyRecord.employees_hired,
                    newCompanyRecord.employees_fired,
                    newCompanyRecord.turnover_rate,
                    newCompanyRecord.profits,
                    newCompanyRecord.expenses,
                    newCompanyRecord.net_profit_loss,
                    newCompanyRecord.current_headcount
                });
            }

            return RedirectToAction("CompanyRecords");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
