using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using StaffSyncWeb.Models;
using System.Data;

namespace StaffSyncWeb.Controllers
{
	public class CommsController : Controller
	{
        private readonly IConfiguration _configuration;

        public CommsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        public async Task<IActionResult> Announcements()
        {
            using (var connection = CreateConnection())
            {
                string sql = "SELECT announcement_id , subject, message" +
                             " FROM Announcements";

                var announcements = await connection.QueryAsync<Announcement>(sql);
                return View("Announcements", announcements);
            }
        }

        public async Task<IActionResult> Programs()
        {
            using (var connection = CreateConnection())
            {
                string sql = "SELECT program_id, employee_email, subject, description, link" +
                             " FROM Programs";
                var programs = await connection.QueryAsync<Programs>(sql);
                return View("Programs", programs);
            }
        }

        public async Task<IActionResult> Messages()
        {
            using (var connection = CreateConnection())
            {
                string sql = "SELECT employee_email " +
                             "FROM Employees";

                var employeeEmails = await connection.QueryAsync<string>(sql);
                ViewBag.EmployeeEmails = employeeEmails;
            }
            return View();
        }

        public async Task<IActionResult> SendMessage(string employeeEmail, string subject, string messageText)
        {
            using (var connection = CreateConnection())
            {
                string sql = "INSERT INTO Message (employee_id, subject, message) VALUES (@EmployeeEmail, @Subject, @Message)";

                string sqlLog = "INSERT INTO Logs (log, date, employee_email) " +
                "VALUES (@Log, GETDATE(), @LoggedInEmail);";

                var parameters = new { EmployeeEmail = employeeEmail, Subject = subject, Message = messageText };

                await connection.ExecuteAsync(sql, parameters);

                await connection.ExecuteAsync(sqlLog, new
                {
                    Log = $"Message sent to {employeeEmail} by {GlobalVariables.LoggedInUserEmail}.",
                    LoggedInEmail = GlobalVariables.LoggedInUserEmail
                });
            }
            return RedirectToAction("Messages");
        }


        public IActionResult CreateAnnouncement()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement(string subject, string message)
        {
            using (var connection = CreateConnection())
            {
                string sql = "INSERT INTO Announcements (subject, message) VALUES (@Subject, @Message)";

                string sqlLog = "INSERT INTO Logs (log, date, employee_email) " +
                "VALUES (@Log, GETDATE(), @LoggedInEmail);";

                await connection.ExecuteAsync(sql, new { Subject = subject, Message = message });

                await connection.ExecuteAsync(sqlLog, new
                {
                    Log = $"{GlobalVariables.LoggedInUserEmail} Sent out an announcement.",
                    LoggedInEmail = GlobalVariables.LoggedInUserEmail
                });
            }

            return RedirectToAction("Announcements");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            using (var connection = CreateConnection())
            {
                string sql = "DELETE FROM Announcements WHERE announcement_id = @Id";

                string sqlLog = "INSERT INTO Logs (log, date, employee_email) " +
                "VALUES (@Log, GETDATE(), @LoggedInEmail);";

                await connection.ExecuteAsync(sql, new { Id = id });

                await connection.ExecuteAsync(sqlLog, new
                {
                    Log = $"{GlobalVariables.LoggedInUserEmail} deleted an announcement.",
                    LoggedInEmail = GlobalVariables.LoggedInUserEmail
                });

            }
            return RedirectToAction("Announcements");
        }

        [HttpGet]
        public async Task<IActionResult> CreateProgram()
        {
            using (var connection = CreateConnection())
            {
                var employeeEmails = await connection.QueryAsync<string>("SELECT employee_email FROM Employees");

                ViewBag.EmployeeEmails = new SelectList(employeeEmails);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateProgram(Programs program)
        {
            using (var connection = CreateConnection())
            {
                string sql = "INSERT INTO Programs (employee_email, subject, description, link) " +
                             "VALUES (@EmployeeEmail, @Subject, @Description, @Link)";

                string sqlLog = "INSERT INTO Logs (log, date, employee_email) " +
                "VALUES (@Log, GETDATE(), @LoggedInEmail);";

                await connection.ExecuteAsync(sql, new
                {
                    EmployeeEmail = program.employee_email,
                    Subject = program.subject,
                    Description = program.description,
                    Link = program.link
                });

                await connection.ExecuteAsync(sqlLog, new
                {
                    Log = $"Program added for {program.employee_email} by {GlobalVariables.LoggedInUserEmail}.",
                    LoggedInEmail = GlobalVariables.LoggedInUserEmail
                });
            }

            return RedirectToAction("Programs");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProgram(int id)
        {
            using (var connection = CreateConnection())
            {
                string sql = "DELETE FROM Programs WHERE program_id = @Id";
                await connection.ExecuteAsync(sql, new { Id = id });
            }

            return RedirectToAction("Programs");
        }


    }
}
