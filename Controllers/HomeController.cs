using Dapper;
using FirebaseAdmin.Messaging;
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
                string jobListingSql = "SELECT listing_id, job_title, job_description, salary, benefits, status, contact_email" +
                                       " FROM Listings";

                var jobListings = await connection.QueryAsync<JobListing>(jobListingSql);             

                return View(jobListings);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            using (var connection = CreateConnection())
            {
                // SQL to update the status of the job listing
                string sql = "UPDATE Listings SET status = @Status WHERE listing_id = @Id";

                await connection.ExecuteAsync(sql, new { Id = id, Status = status });
            }

            return RedirectToAction("Recruitment");
        }


        public IActionResult NewListing()
        {
           return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateListing(JobListing newJobListing)
        {
            using (var connection = CreateConnection())
            {
                string sql = @"
                            INSERT INTO Listings (job_title, job_description, salary, benefits, status, contact_email)
                            VALUES (@JobTitle, @JobDescription, @Salary, @Benefits, @Status, @ContactEmail)";

                string sqlLog = "INSERT INTO Logs (log, date, employee_email) " +
                                "VALUES (@Log, GETDATE(), @LoggedInEmail);";

                string sqlAnn = "INSERT INTO Announcements (subject, message) VALUES (@Subject, @Message)";

                await connection.ExecuteAsync(sqlAnn, new
                {
                    Subject = $"New position opened ({newJobListing.job_title})",
                    Message = $"Contact {newJobListing.contact_email} for more information"
                }) ;


                await connection.ExecuteAsync(sql, new
                {
                    JobTitle = newJobListing.job_title,
                    JobDescription = newJobListing.job_description,
                    Salary = newJobListing.salary,
                    Benefits = newJobListing.benefits,
                    Status = newJobListing.status,
                    ContactEmail = newJobListing.contact_email
                });

                await connection.ExecuteAsync(sqlLog, new
                {
                    Log = $"{GlobalVariables.LoggedInUserEmail} created a new job listing ({newJobListing.job_title}).",
                    LoggedInEmail = GlobalVariables.LoggedInUserEmail
                });
            }

            return RedirectToAction("Recruitment");
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
