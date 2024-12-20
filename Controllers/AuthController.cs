﻿using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StaffSyncWeb.Models;
using System.Data;
using System.Threading.Tasks;

namespace StaffSyncWeb.Controllers
{
    public class AuthController : Controller
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            using (var connection = CreateConnection())
            {
                // Check if the email contains "admin"
                if (!email.ToLower().Contains("admin"))
                {
                    ViewBag.ErrorMessage = "Access restricted. Only admin accounts are allowed to login.";
                    return View();
                }

                // Validate the email and password
                string sql = "SELECT password FROM Employees WHERE employee_email = @Email";
                var storedPassword = await connection.QuerySingleOrDefaultAsync<string>(sql, new { Email = email });

                if (storedPassword == null || storedPassword != password)
                {
                    ViewBag.ErrorMessage = "Invalid email or password.";
                    return View();
                }

                // Store the email globally
                GlobalVariables.LoggedInUserEmail = email;

                return RedirectToAction("Dashboard", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logs()
        {
            using (var connection = CreateConnection())
            {
                string sql = "SELECT * FROM Logs ORDER BY date DESC"; // Query to fetch all logs

                var logs = await connection.QueryAsync<LogEntry>(sql);

                return View(logs);
            }
        }
    }
}
