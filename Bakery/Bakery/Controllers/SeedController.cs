using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Bakery.Data;
using Bakery.Context;
using Bakery.Models;
using Microsoft.AspNetCore.Identity;

namespace Bakery.Controllers;

[ApiController]
[Route("[controller]")]
public class SeedController : ControllerBase
{
    private readonly MyDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public SeedController(MyDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPut(Name = "Seed")]
    public void Put()
    {
        SeedUsers(_userManager);

        var seedData = new SeedData();
        seedData.Seed(_context);
    }
    
    public static void SeedUsers(UserManager<ApplicationUser> userManager)
    {
        SeedUser(userManager, "Admin@localhost", "Secret7$", "Admin");
        SeedUser(userManager, "Manager@localhost", "Secret7$", "Manager");
        SeedUser(userManager, "Baker@localhost", "Secret7$", "Baker");
        SeedUser(userManager, "Driver@localhost", "Secret7$", "Driver");
    }
    private static void SeedUser(UserManager<ApplicationUser> userManager, string email, string password, string role)
    {
        if (userManager.FindByNameAsync(email).Result == null)
        {
            var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            IdentityResult result = userManager.CreateAsync(user, password).Result;

            if (result.Succeeded)
            {
                var newUser = userManager.FindByNameAsync(email).Result;
                var claim = new Claim($"Is{role}", "true");
                var claimAdded = userManager.AddClaimAsync(newUser, claim).Result;
            }
        }
    }
}
