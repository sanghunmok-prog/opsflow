using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpsFlow.Application.Auth;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<AppUser> userManager,
    IJwtTokenService jwtTokenService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }

        var passwordIsValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordIsValid)
        {
            return Unauthorized();
        }

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var token = jwtTokenService.CreateToken(user, roles);

        return Ok(new LoginResponse(
            token.AccessToken,
            "Bearer",
            token.ExpiresAtUtc,
            ToUserSummary(user, roles)));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserResponse>> Me()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }

        var roles = (await userManager.GetRolesAsync(user)).ToArray();

        return Ok(new CurrentUserResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            roles));
    }

    private static UserSummaryDto ToUserSummary(AppUser user, IReadOnlyList<string> roles)
    {
        return new UserSummaryDto(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            roles);
    }
}
