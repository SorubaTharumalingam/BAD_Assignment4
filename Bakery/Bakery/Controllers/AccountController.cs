using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Bakery.Context;
using Bakery.DTOs;
using Bakery.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Bakery.Controllers;
[Authorize]
[ApiController]
[Route("[Controller]")]
public class AccountController : ControllerBase
{
    private readonly MyDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(MyDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration,  ILogger<AccountController> logger)
    {
        _context = context;
        _configuration = configuration;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;

    }
    
    [HttpPost]
    [Route("Register")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult> Register(RegisterDTO input)
    {
        var timestamp = new DateTimeOffset(DateTime.UtcNow);
        var userIdentity = User.Identity.IsAuthenticated ? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value : "anonymous";
        var logInfo = new { Operation = "Post" , User =  userIdentity, Timestamp = timestamp};
        
        _logger.LogInformation("Post Register  {@LogInfo} ", logInfo);

        try
        {
            if (ModelState.IsValid)
            {
                var newUser = new ApplicationUser();
                newUser.UserName = input.Email;
                newUser.Email = input.Email;
                newUser.FullName = input.FullName;
                var result = await _userManager.CreateAsync(
                    newUser, input.Password);
                if (result.Succeeded)
                {
                    // _logger.LogInformation(
                    //     "User {userName} ({email}) has been created.",
                    //    newUser.UserName, newUser.Email);
                    return StatusCode(201,
                        $"User '{newUser.UserName}' has been created.");
                }
                else
                    throw new Exception(
                        string.Format("Error: {0}", string.Join(" ",
                            result.Errors.Select(e => e.Description))));
            }
            else
            {
                var details = new ValidationProblemDetails(ModelState);
                details.Type =
                    "https:/ /tools.ietf.org/html/rfc7231#section-6.5.1";
                details.Status = StatusCodes.Status400BadRequest;
                return new BadRequestObjectResult(details);
            }
        }
        catch (Exception e)
        {
            var exceptionDetails = new ProblemDetails();
            exceptionDetails.Detail = e.Message;
            exceptionDetails.Status =
                StatusCodes.Status500InternalServerError;
            exceptionDetails.Type =
                "https:/ /tools.ietf.org/html/rfc7231#section-6.6.1";
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                exceptionDetails);
        }
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("Login")]
    public async Task<ActionResult> Login(LoginDTO input)
    {
        var timestamp = new DateTimeOffset(DateTime.UtcNow);
        var userIdentity = User.Identity.IsAuthenticated ? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value : "anonymous";
        var logInfo = new { Operation = "Post" , User =  userIdentity, Timestamp = timestamp};
        
        _logger.LogInformation("Post Login  {@LogInfo} ", logInfo);
        
        try
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(input.UserName);
                if (user == null || !await _userManager.CheckPasswordAsync(user, input.Password))
                throw new Exception("Invalid login attempt");
                else
                {
                    var signingCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(
                            System.Text.Encoding.UTF8.GetBytes(_configuration["JWT:SigningKey"])),
                        SecurityAlgorithms.HmacSha256);
                    var claims = new List<Claim>();
                    claims.Add(new Claim(ClaimTypes.Name, user.UserName));
                    
                    // Fetch the claims from the AspNetUserClaims table
                    var userClaims = await _userManager.GetClaimsAsync(user);
                    claims.AddRange(userClaims);
                    
                    var jwtObject = new JwtSecurityToken(
                        issuer: _configuration["JWT:Issuer"],
                        audience: _configuration["JWT:Audience"],
                        claims: claims,
                        expires: DateTime.Now.AddSeconds(300),
                        signingCredentials: signingCredentials);
                    var jwtString = new JwtSecurityTokenHandler()
                        .WriteToken(jwtObject);
                    return StatusCode(StatusCodes.Status200OK, jwtString);
                }
            }
            else
            {
                var details = new ValidationProblemDetails(ModelState);
                details.Type =
                    "https:/ /tools.ietf.org/html/rfc7231#section-6.5.1";
                details.Status = StatusCodes.Status400BadRequest;
                return new BadRequestObjectResult(details);
            }
        }
        catch (Exception e)
        {
            var exceptionDetails = new ProblemDetails();
            exceptionDetails.Detail = e.Message;
            exceptionDetails.Status =
                StatusCodes.Status401Unauthorized;
            exceptionDetails.Type =
                "https:/ /tools.ietf.org/html/rfc7231#section-6.6.1";
            return StatusCode(
                StatusCodes.Status401Unauthorized, exceptionDetails);
        }
    }
}