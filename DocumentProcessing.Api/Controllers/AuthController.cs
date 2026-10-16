using DocumentProcessing.Api.DTOs;
using DocumentProcessing.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocumentProcessing.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(
        AuthService authService,
        JwtTokenService jwtTokenService)
    {
        _authService = authService;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = await _authService.RegisterAsync(
            request.Email,
            request.Password);

        if (user is null)
        {
            return Conflict(new
            {
                message = "An account with this email already exists."
            });
        }

        // Return safe account details, never the password or its hash.
        return StatusCode(StatusCodes.Status201Created, new
        {
            user.Id,
            user.Email,
            user.CreatedAt
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _authService.LoginAsync(
            request.Email,
            request.Password);

        if (user is null)
        {
            return Unauthorized(new
            {
                message = "Email or password is incorrect."
            });
        }

        var token = _jwtTokenService.CreateToken(user);

        return Ok(new { token });
    }
}