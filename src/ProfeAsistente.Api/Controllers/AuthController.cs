using ProfeAsistente.Api.Services.Auth;
using ProfeAsistente.Api.Services.Authorization;
using ProfeAsistente.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ProfeAsistente.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _auth;
    private readonly ICurrentUserService _current;
    private readonly IHostEnvironment _env;

    public AuthController(IAuthenticationService auth, ICurrentUserService current, IHostEnvironment env)
    {
        _auth = auth;
        _current = current;
        _env = env;
    }

    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _auth.LoginAsync(request, Ip(), Ua(), ct));
        }
        catch (AuthException ex) { return Unauthorized(new { error = ex.Message }); }
    }

    [AllowAnonymous]
    [EnableRateLimiting("refresh")]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthenticationResponse>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        try { return Ok(await _auth.RefreshAsync(request, Ip(), Ua(), ct)); }
        catch (AuthException ex) { return Unauthorized(new { error = ex.Message }); }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        await _auth.LogoutAsync(request.RefreshToken, Ip(), ct);
        return NoContent();
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        if (_current.UserId is null) return Unauthorized();
        await _auth.LogoutAllAsync(_current.UserId.Value, ct);
        return NoContent();
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        if (_current.UserId is null) return Unauthorized();
        try
        {
            await _auth.ChangePasswordAsync(_current.UserId.Value, request, ct);
            return NoContent();
        }
        catch (AuthException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [AllowAnonymous]
    [EnableRateLimiting("password-reset")]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ForgotPasswordResponse>> Forgot([FromBody] ForgotPasswordRequest request, CancellationToken ct)
        => Ok(await _auth.ForgotPasswordAsync(request, _env.IsDevelopment(), ct));

    [AllowAnonymous]
    [EnableRateLimiting("password-reset")]
    [HttpPost("reset-password")]
    public async Task<IActionResult> Reset([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        try
        {
            await _auth.ResetPasswordAsync(request, ct);
            return NoContent();
        }
        catch (AuthException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserSessionDto>> Me(CancellationToken ct)
    {
        if (_current.UserId is null) return Unauthorized();
        return Ok(await _auth.GetMeAsync(_current.UserId.Value, _current.ActiveInstitutionId, ct));
    }

    [Authorize]
    [HttpGet("sessions")]
    public async Task<ActionResult<IReadOnlyList<AuthSessionDto>>> Sessions(CancellationToken ct)
    {
        if (_current.UserId is null) return Unauthorized();
        return Ok(await _auth.GetSessionsAsync(_current.UserId.Value, null, ct));
    }

    [Authorize]
    [HttpDelete("sessions/{sessionId:guid}")]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken ct)
    {
        if (_current.UserId is null) return Unauthorized();
        try
        {
            await _auth.RevokeSessionAsync(_current.UserId.Value, sessionId, ct);
            return NoContent();
        }
        catch (AuthException ex) { return NotFound(new { error = ex.Message }); }
    }

    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
    private string? Ua() => Request.Headers.UserAgent.ToString();
}
