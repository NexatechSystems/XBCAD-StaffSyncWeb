using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient; // Update this namespace based on your project structure
using StaffSyncWeb.Models;
using System.Data;
using System.Reflection;

namespace StaffSyncWeb.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IConfiguration _configuration;

        public EmployeeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        public async Task<IActionResult> Employees()
        {
            using (var connection = CreateConnection())
            {
                var sql = "SELECT employee_email, name, surname, email_personal, mobile, position, access_level\r\n  FROM Employees ";
                var employees = await connection.QueryAsync<Employee>(sql);

                return View("Employees", employees);
            }
        }

        public async Task<IActionResult> Attendance()
        {
            using (var connection = CreateConnection())
            {
                string sql = "SELECT employee_email, name, surname, clocked_in" +
                             " FROM Attendance" +
                             " WHERE clocked_in = 1";
                var attendanceList = await connection.QueryAsync<Attendance>(sql);
                return View("Attendance", attendanceList);
            }
        }

        public async Task<IActionResult> Leave()
        {
            using (var connection = CreateConnection())
            {
                string sql = "SELECT *" +
                             " FROM Leave";
                var leaveRequests = await connection.QueryAsync<Leave>(sql);
                return View("Leave", leaveRequests);
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        public async Task<IActionResult> AddEmployee(string email, string password, string name, string surname, string emailPersonal, string mobile, string position, string accessLevel, decimal salary)
        {
            using (var connection = CreateConnection())
            {
                // SQL to add employee details
                string sql = "INSERT INTO Employees (employee_email, password, name, surname, email_personal, mobile, position, access_level) " +
                             "VALUES (@Email, @Password, @Name, @Surname, @EmailPersonal, @Mobile, @Position, @AccessLevel); " +

                             "INSERT INTO Salaries (employee_id, amount, position) " +
                             "VALUES (@Email, @Salary, @Position); " +

                             "INSERT INTO Attendance (employee_email, name, surname, clocked_in) " +
                             "VALUES (@Email, @Name, @Surname, 0);";

                // SQL to add welcome message
                string sqlMessage = "INSERT INTO Message (subject, message, employee_id) " +
                                    "VALUES (@Subject, @Message, @Email);";

                // SQL to log the action in Logs table
                string sqlLog = "INSERT INTO Logs (log, date, employee_email) " +
                                "VALUES (@Log, GETDATE(), @LoggedInEmail);";

                // Parameters for inserting employee details
                var parameters = new
                {
                    Email = email,
                    Password = password,
                    Name = name,
                    Surname = surname,
                    EmailPersonal = emailPersonal,
                    Mobile = mobile,
                    Position = position,
                    AccessLevel = accessLevel,
                    Salary = salary
                };

                // Execute employee-related queries
                await connection.ExecuteAsync(sql, parameters);

                // Execute message query
                await connection.ExecuteAsync(sqlMessage, new
                {
                    Subject = "Welcome",
                    Message = $"Welcome to the company \nHere are your credentials, keep them safe: \nEmail: {email} \nPassword: {password}",
                    Email = email
                });

                // Execute log query
                await connection.ExecuteAsync(sqlLog, new
                {
                    Log = $"Employee with email {email} has been added by {GlobalVariables.LoggedInUserEmail}.",
                    LoggedInEmail = GlobalVariables.LoggedInUserEmail
                });
            }

            return RedirectToAction("Employees");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string email)
        {
            using (var connection = CreateConnection())
            {
                string sql = "SELECT employee_email, password, name, surname, email_personal, mobile, position, access_level " +
                             "FROM Employees WHERE employee_email = @Email";

                var employee = await connection.QuerySingleOrDefaultAsync<Employee>(sql, new { Email = email });

                if (employee == null)
                {
                    return NotFound();
                }

                return View(employee);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Employee updatedEmployee)
        {
            using (var connection = CreateConnection())
            {
                string sql = "UPDATE Employees SET password = @Password, name = @Name, surname = @Surname, email_personal = @EmailPersonal, " +
                             "mobile = @Mobile, position = @Position, access_level = @AccessLevel " +
                             "WHERE employee_email = @EmployeeEmail; " +

                             "UPDATE Attendance SET name = @Name, surname = @Surname " +
                             "WHERE employee_email = @EmployeeEmail;";

                // SQL to log the action in Logs table
                string sqlLog = "INSERT INTO Logs (log, date, employee_email) " +
                                "VALUES (@Log, GETDATE(), @LoggedInEmail);";


                await connection.ExecuteAsync(sql, new
                {
                    Password = updatedEmployee.password,
                    Name = updatedEmployee.name,
                    Surname = updatedEmployee.surname,
                    EmailPersonal = updatedEmployee.email_personal,
                    Mobile = updatedEmployee.mobile,
                    Position = updatedEmployee.position,
                    AccessLevel = updatedEmployee.access_level,
                    EmployeeEmail = updatedEmployee.employee_email
                });

                await connection.ExecuteAsync(sqlLog, new
                {
                    Log = $"{updatedEmployee.employee_email} details have been edited by {GlobalVariables.LoggedInUserEmail}.",
                    LoggedInEmail = GlobalVariables.LoggedInUserEmail
                });

                return RedirectToAction("Employees");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string email)
        {
            using (var connection = CreateConnection())
            {
                string sql = "DELETE FROM Salaries WHERE employee_id = @EmployeeEmail; " +
                             "DELETE FROM Attendance WHERE employee_email = @EmployeeEmail; " +
                             "DELETE FROM Leave WHERE employee_email = @EmployeeEmail; " +
                             "DELETE FROM Payslip WHERE employee_id = @EmployeeEmail; " +
                             "DELETE FROM Programs WHERE employee_email = @EmployeeEmail; " +
                             "DELETE FROM Message WHERE employee_id = @EmployeeEmail " +
                             "DELETE FROM Employees WHERE employee_email = @EmployeeEmail;";

                // SQL to log the action in Logs table
                string sqlLog = "INSERT INTO Logs (log, date, employee_email) " +
                                "VALUES (@Log, GETDATE(), @LoggedInEmail);";

                await connection.ExecuteAsync(sql, new { EmployeeEmail = email });

                await connection.ExecuteAsync(sqlLog, new
                {
                    Log = $"Employee with email {email} has been deleted by {GlobalVariables.LoggedInUserEmail}.",
                    LoggedInEmail = GlobalVariables.LoggedInUserEmail
                });
            }
            return RedirectToAction("Employees");
        }

        [HttpPost]
        public async Task<IActionResult> ApproveLeave(int id)
        {
            using (var connection = CreateConnection())
            {
                string getEmailSql = "SELECT employee_email FROM Leave WHERE leave_id = @LeaveId";
                var employeeEmail = await connection.QuerySingleOrDefaultAsync<string>(getEmailSql, new { LeaveId = id });

                string getLeaveType = "SELECT leave_type FROM Leave Where leave_id = @LeaveId";
                var leaveType = await connection.QuerySingleOrDefaultAsync<string>(getLeaveType, new { LeaveId = id });

                if (employeeEmail != null)
                {
                    string insertMessageSql = "INSERT INTO Message (subject, message, employee_id) " +
                                              "VALUES (@Subject, @Message, @EmployeeEmail)";

                    string sqlLog = "INSERT INTO Logs (log, date, employee_email) " +
                                    "VALUES (@Log, GETDATE(), @LoggedInEmail);";

                    await connection.ExecuteAsync(insertMessageSql, new
                    {
                        Subject = "Leave Request Approved",
                        Message = $"Your leave request for {leaveType} has been approved.",
                        EmployeeEmail = employeeEmail
                    });

                    await connection.ExecuteAsync(sqlLog, new
                    {
                        Log = $"Leave request for {employeeEmail} has been approved by {GlobalVariables.LoggedInUserEmail}.",
                        LoggedInEmail = GlobalVariables.LoggedInUserEmail
                    });

                    string sql = "UPDATE Leave SET approved = 1 WHERE leave_id = @LeaveId";
                    await connection.ExecuteAsync(sql, new { LeaveId = id });
                }
                return RedirectToAction("Leave");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeclineLeave(int id)
        {
            using (var connection = CreateConnection())
            {
                
                string getEmailSql = "SELECT employee_email FROM Leave WHERE leave_id = @LeaveId";
                var employeeEmail = await connection.QuerySingleOrDefaultAsync<string>(getEmailSql, new { LeaveId = id });

                string getLeaveType = "SELECT leave_type FROM Leave Where leave_id = @LeaveId";
                var leaveType = await connection.QuerySingleOrDefaultAsync<string>(getLeaveType, new { LeaveId = id });

                if (employeeEmail != null)
                {
                    
                    string insertMessageSql = "INSERT INTO Message (subject, message, employee_id) " +
                                              "VALUES (@Subject, @Message, @EmployeeEmail)";

                    string sqlLog = "INSERT INTO Logs (log, date, employee_email) " +
                                    "VALUES (@Log, GETDATE(), @LoggedInEmail);";

                    await connection.ExecuteAsync(insertMessageSql, new
                    {
                        Subject = "Leave Request Declined",
                        Message = $"We regret to inform you that your leave request for {leaveType} has been declined. Please try again or contact HR",
                        EmployeeEmail = employeeEmail
                    });

                    await connection.ExecuteAsync(sqlLog, new
                    {
                        Log = $"Leave request for {employeeEmail} has been declined by {GlobalVariables.LoggedInUserEmail}.",
                        LoggedInEmail = GlobalVariables.LoggedInUserEmail
                    });


                    string deleteLeaveSql = "DELETE FROM Leave WHERE leave_id = @LeaveId";
                    await connection.ExecuteAsync(deleteLeaveSql, new { LeaveId = id });
                }
            }

            return RedirectToAction("Leave");
        }


    }
}
