using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HCSN.Identity.Public;
using System.Security.Claims;

namespace HCSN.Identity.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IIdentityModule _identityModule;
    
    public AuthController(IIdentityModule identityModule)
    {
        _identityModule = identityModule;
    }
    
    [HttpPost("register")]
    public async Task<ActionResult<AuthResult>> Register(RegisterRequest request)
    {
        var result = await _identityModule.RegisterAsync(request);
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }
    
    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login(LoginRequest request)
    {
        var result = await _identityModule.LoginAsync(request);
        if (!result.Success)
            return Unauthorized(result);
            
        return Ok(result);
    }
    
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();
            
        var user = await _identityModule.GetUserByIdAsync(userId);
        if (user == null)
            return NotFound();
            
        return Ok(user);
    }
    
    [Authorize]
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid userId)
    {
        var user = await _identityModule.GetUserByIdAsync(userId);
        if (user == null)
            return NotFound();
            
        return Ok(user);
    }
    
    [Authorize]
    [HttpGet("user/by-email/{email}")]
    public async Task<ActionResult<UserDto>> GetUserByEmail(string email)
    {
        var user = await _identityModule.GetUserByEmailAsync(email);
        if (user == null)
            return NotFound();
            
        return Ok(user);
    }
    
    [Authorize]
    [HttpGet("my-systems")]
    public async Task<ActionResult<List<string>>> GetMyAccessibleSystems()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();
            
        var systems = await _identityModule.GetUserAccessibleSystemsAsync(userId);
        return Ok(systems);
    }
}