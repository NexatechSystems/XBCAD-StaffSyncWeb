using Dapper;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using iText.Kernel.Pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StaffSyncWeb.Models;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace StaffSyncWeb.Controllers
{
    public class FinanceController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _serviceAccountPath = "C:\\Users\\hoque\\source\\repos\\StaffSyncWeb\\staffsyncv3-firebase-adminsdk-73og5-96aa16e999.json";

        public FinanceController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        public async Task<IActionResult> Salary()
        {
            using (var connection = CreateConnection())
            {
                string sql = "SELECT employee_id, amount, position FROM Salaries";
                var salaries = await connection.QueryAsync<Salary>(sql);
                return View(salaries);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            using (var connection = CreateConnection())
            {
                string sql = "SELECT employee_id, amount, position FROM Salaries WHERE employee_id = @Id";

                var salary = await connection.QuerySingleOrDefaultAsync<Salary>(sql, new { Id = id });

                if (salary == null)
                {
                    return NotFound();
                }

                return View(salary);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Salary updatedSalary)
        {
            using (var connection = CreateConnection())
            {
                string sql = "UPDATE Salaries SET amount = @Amount, position = @Position WHERE employee_id = @EmployeeID";
                string sqlLog = "INSERT INTO Logs (log, date, employee_email) " +
                "VALUES (@Log, GETDATE(), @LoggedInEmail);";

                await connection.ExecuteAsync(sql, new
                {
                    Amount = updatedSalary.amount,
                    Position = updatedSalary.position,
                    EmployeeID = updatedSalary.employee_id
                });
                await connection.ExecuteAsync(sqlLog, new
                {
                    Log = $"{updatedSalary.employee_id} salary changed by {GlobalVariables.LoggedInUserEmail}.",
                    LoggedInEmail = GlobalVariables.LoggedInUserEmail
                });

                return RedirectToAction("Salary"); 
            }
        }

        public async Task<IActionResult> Payslip()
        {
            using (var connection = CreateConnection())
            {
                string sql = "SELECT employee_email FROM Employees";
                var employeeEmails = await connection.QueryAsync<string>(sql);
                return View(employeeEmails);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSalary(string email)
        {
            using (var connection = CreateConnection())
            {
                string sql = "SELECT amount FROM Salaries WHERE employee_id = @Email";
                var salary = await connection.QuerySingleOrDefaultAsync<decimal>(sql, new { Email = email });

                if (salary == default)
                    return NotFound();

                return Json(new { salary });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GeneratePayslip(string employeeEmail, string month, decimal bonus)
        {
            using (var connection = CreateConnection())
            {
                
                string salarySql = "SELECT amount FROM Salaries WHERE employee_id = @Email";
                var salary = await connection.QuerySingleOrDefaultAsync<decimal>(salarySql, new { Email = employeeEmail });

                if (salary == default)
                    return NotFound("Employee salary not found.");

                
                decimal total = salary + bonus;

                
                string insertSql = @"
                    INSERT INTO Payslip (month, salary, bonus, employee_id)
                    VALUES (@Month, @Salary, @Bonus, @EmployeeEmail);
                ";

                try
                {
                    await connection.ExecuteAsync(insertSql, new
                    {
                        Month = month,
                        Salary = salary,
                        Bonus = bonus,
                        EmployeeEmail = employeeEmail
                    });

                    string fetchEmailsSql = "SELECT employee_email FROM Employees";
                    var employeeEmails = await connection.QueryAsync<string>(fetchEmailsSql);

                    // Set ViewBag values for confirmation message
                    ViewBag.Message = "Payslip successfully generated and stored in the database.";
                    ViewBag.Month = month;
                    ViewBag.EmployeeEmail = employeeEmail;
                    ViewBag.Salary = salary;
                    ViewBag.Bonus = bonus;
                    ViewBag.Total = total;

                    return View("Payslip", employeeEmails);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }
        }
    }
}
