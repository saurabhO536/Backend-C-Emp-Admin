using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using EmployeeMng.API.Data;
using EmployeeMng.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeMng.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(
    ApplicationDbContext context,
    IConfiguration configuration)
{
    _context = context;
    _configuration = configuration;
}

    [HttpPost("register")]
public async Task<IActionResult> Register(RegisterRequest request)
{
    var existingUser = await _context.Users
        .FirstOrDefaultAsync(u =>
            u.Username == request.Username ||
            u.Email == request.Email);

    if (existingUser != null)
    {
        if (existingUser.Username == request.Username &&
            existingUser.Email == request.Email)
        {
            return BadRequest("Username and email already exist.");
        }

        if (existingUser.Username == request.Username)
        {
            return BadRequest("Username already exists.");
        }

        return BadRequest("Email already exists.");
    }

    var user = new User
    {
        Username = request.Username,
        Email = request.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        Role = "User"
    };

    _context.Users.Add(user);

    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "User registered successfully.",
        user.Id,
        user.Username,
        user.Email,
        user.Role
    });
}

    [HttpPost("login")]
public async Task<IActionResult> Login(LoginRequest request)
{
    var user = await _context.Users
        .FirstOrDefaultAsync(u => u.Username == request.Username);

    if (user == null)
    {
        return Unauthorized("Invalid username or password.");
    }

    bool passwordValid = BCrypt.Net.BCrypt.Verify(
        request.Password,
        user.PasswordHash);

    if (!passwordValid)
    {
        return Unauthorized("Invalid username or password.");
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"]!));

    var credentials = new SigningCredentials(
        key,
        SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: credentials);

    var tokenString = new JwtSecurityTokenHandler()
        .WriteToken(token);

    return Ok(new
    {
        message = "Login successful.",
        token = tokenString,
        user = new
        {
            user.Id,
            user.Username,
            user.Email,
            user.Role
        }
    });
}
[HttpPost("forgot-password")]
public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
{
    var user = await _context.Users
        .FirstOrDefaultAsync(u => u.Email == request.Email);

    // Do not reveal whether the email exists.
    return Ok(new
    {
        message = "If an account exists with this email, password reset instructions have been sent."
    });
}
}