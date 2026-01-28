using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using StudentApi.Data;
using StudentApi.Models;
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

        [HttpPost("signup")]

        public IActionResult Signup([FromBody] User user)
        {
            // 1. Basic validation
            if (user == null ||
                string.IsNullOrWhiteSpace(user.Name) ||
                string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(user.Password))
            {
                return BadRequest("Name, Email, and Password are required.");
            }

            string email = user.Email.Trim().ToLower();

            // 2. Check if user already exists
            bool userExists = _context.Users.Any(u => u.Email == email);
            if (userExists)
            {
                Console.WriteLine($"User already exists with email: {email}");
                return Conflict("User already exists.");
            }

            // 3. Create new user
            var newUser = new User
            {
                Name = user.Name.Trim(),
                Email = email,
                Password = user.Password, // TODO: Hash password
                Role = user.Role.ToLower()
            };

            // 4. Save to database
            _context.Users.Add(newUser);
            _context.SaveChanges();

            return Ok(new
            {
                message = "User registered successfully",
                //userId = newUser.Id
            });
        }



    }



}
