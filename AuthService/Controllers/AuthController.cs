using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login(LoginRequest request)
        {
            var configuredUsername =
                _configuration["Login:Username"];

            var configuredPassword =
                _configuration["Login:Password"];

            if (request.Username != configuredUsername ||
                request.Password != configuredPassword)
            {
                return Unauthorized(new
                {
                    message = "Invalid username or password"
                });
            }

            var claims = new[]
            {
                new Claim(
                    ClaimTypes.Name,
                    request.Username),

                new Claim(
                    ClaimTypes.Role,
                    "User")
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _configuration["Jwt:Key"]!));

            var credentials =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    int.Parse(
                        _configuration["Jwt:ExpiryMinutes"]!)),
                signingCredentials: credentials);

            var tokenString =
                new JwtSecurityTokenHandler()
                    .WriteToken(token);

            return Ok(new
            {
                accessToken = tokenString,
                tokenType = "Bearer",
                expiresIn = 3600
            });
        }
    }
}