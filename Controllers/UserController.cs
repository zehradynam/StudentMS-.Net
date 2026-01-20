using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentApi.Data;
using StudentApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace StudentApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        // Constructor injects the database context (Dependency Injection)
        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // -------------------- GET ALL USERS --------------------
        [HttpGet]
        public IActionResult GetUsers()
        {
            var users = _context.Users.ToList(); // Fetch all users from database
            return Ok(users); // Return 200 OK with data
        }

        // -------------------- LOGIN --------------------
        // Internal class to handle login request
        public class LoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Validate incoming data
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                return BadRequest("Email and Password are required.");

            // Check user in database
            var user = _context.Users
                .FirstOrDefault(u => u.Email == request.Email && u.Password == request.Password);

            if (user == null)
                return Unauthorized("Invalid credentials");

            // -------------------- JWT TOKEN GENERATION --------------------
            var key = Encoding.UTF8.GetBytes("THIS_IS_MY_SUPER_SECRET_KEY_123456"); // 32+ chars

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("Email", user.Email),
                    new Claim("UserId", user.Id.ToString()),

                    new Claim(ClaimTypes.Role, user.Role)  // <-- Add this line

                }),
                Expires = DateTime.UtcNow.AddHours(1),           // Token expiration
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(token);

            return Ok(new { token = jwtToken }); // Return token to client

        }
    }
}
