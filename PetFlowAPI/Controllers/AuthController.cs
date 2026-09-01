using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetFlowAPI.Data;
using PetFlowAPI.Security;

namespace PetFlowAPI.Controllers;

public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(string AccessToken, DateTime ExpiresAt, long TutorId, string Name, string Email);

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly PetFlowContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly JwtOptions _jwtOptions;

    public AuthController(PetFlowContext context, IPasswordService passwordService, ITokenService tokenService, Microsoft.Extensions.Options.IOptions<JwtOptions> jwtOptions)
    {
        _context = context;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _jwtOptions = jwtOptions.Value;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var tutor = await _context.Tutors.SingleOrDefaultAsync(t => t.Email == request.Email);
        if (tutor is null || !_passwordService.Verify(request.Password, tutor.PasswordHash))
            return Unauthorized(new { message = "E-mail ou senha inválidos." });

        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);
        return Ok(new LoginResponse(_tokenService.CreateToken(tutor), expiresAt, tutor.Id, tutor.Name, tutor.Email));
    }
}
