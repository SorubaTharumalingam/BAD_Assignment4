using Bakery.Context;
using Bakery.DTOs;
using Bakery.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bakery.Controllers;
[ApiController]
[Route("[controller]")]
[Authorize]
public class IngredientsController : ControllerBase
{
    private readonly MyDbContext _context;
    private readonly ILogger<IngredientsController> _logger;
    
    public IngredientsController(MyDbContext context, ILogger<IngredientsController> logger)
    {
        _context = context;
        _logger = logger;
    }


    //minimum query #1 from assignment 2
    [HttpGet("ingredients")]
    [Authorize(Policy = "RequireAdminManagerOrBakerRole")]
    public IActionResult GetAllIngredientsWithQuantities()
    {
        var ingredients = _context.Ingredients
            .Select(i => new IngredientDto
            {
                Name = i.Name,
                Quantity = i.Quantity,
                Allergens = i.IngredientAllergens
                    .Select(ia => ia.Allergen.Name)
                    .ToList()
            })
            .ToList();

        return Ok(ingredients);
    }

    //minimum query #4 from assignment 2
    [HttpGet("batch/{batchId}")]
    [Authorize(Policy = "RequireAdminManagerOrBakerRole")]
    public IActionResult GetIngredientsForBatch(int batchId)
    {
        var batchIngredients = _context.BatchIngredients
            .Where(bi => bi.BatchId == batchId)
            .Include(bi => bi.Ingredient)
            .ThenInclude(i => i.IngredientAllergens)
            .ThenInclude(ia => ia.Allergen)
            .ToList();

        if (batchIngredients == null || !batchIngredients.Any())
        {
            return NotFound("No ingredients found for the batch.");
        }

        var ingredientDtos = batchIngredients.Select(bi =>
        {
            var allergens = bi.Ingredient.IngredientAllergens
                .Select(ia => ia.Allergen.Name)
                .ToList();

            return new IngredientDto
            {
                Name = bi.Ingredient.Name,
                Quantity = bi.Ingredient.Quantity,
                Allergens = allergens
            };
        }).ToList();

        return Ok(ingredientDtos);
    }


    [HttpGet("stock/{ingredientName}")]
    [Authorize(Policy = "RequireAdminManagerOrBakerRole")]
    public IActionResult GetIngredientStock(string ingredientName)
    {
        var ingredient = _context.Ingredients
            .Where(i => i.Name.ToLower() == ingredientName.ToLower())
            .Select(i => new { i.Name, i.Quantity })
            .FirstOrDefault();
        if (ingredient == null)
        {
            return NotFound($"Ingredient with name {ingredientName} was not found");
        }

        return Ok(ingredient);
    }

    // C. update ingredient in stock
    [HttpPut("update-quantity-by-name/{name}")]
    [Authorize(Policy = "RequireAdminRole")]
    public IActionResult UpdateIngredient(string name, [FromBody] IngredientQuantityDto ingredientDto)
    {
        var timestamp = new DateTimeOffset(DateTime.UtcNow);
        var userIdentity = User.Identity.IsAuthenticated ? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value : "anonymous";
        var logInfo = new { Operation = "Put" , User =  userIdentity, Timestamp = timestamp};
        
        _logger.LogInformation("Put update quantity of ingredients by name called {@LogInfo} ", logInfo);
        
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validate that the new quantity is non-negative
        if (ingredientDto.Quantity < 0)
        {
            return BadRequest("Quantity must be non-negative.");
        }

        // Find the ingredient by name using EF.Functions.Like for case-insensitive comparison
        var ingredient = _context.Ingredients
            .FirstOrDefault(i => EF.Functions.Like(i.Name, name));

        if (ingredient == null)
        {
            // If the ingredient does not exist, consider creating a new one or return NotFound
            // For this scenario, let's return a NotFound response
            return NotFound($"Ingredient with name {name} not found.");
        }

        // If found, update the existing ingredient's quantity to the new value
        ingredient.Quantity = ingredientDto.Quantity;

        _context.SaveChanges();
        return Ok(ingredientDto);
    }

    

    // C. Add new ingredient and quantity to the stock
    [HttpPost]
    [Authorize(Policy = "RequireAdminRole")]
    public IActionResult AddIngredient([FromBody] IngredientNameQuantityDto ingredientDto)
    {  
        var timestamp = new DateTimeOffset(DateTime.UtcNow);
        var userIdentity = User.Identity.IsAuthenticated ? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value : "anonymous";
        var logInfo = new { Operation = "Post" , User =  userIdentity, Timestamp = timestamp};
        
        _logger.LogInformation("Post AddIngredient  {@LogInfo} ", logInfo);

   
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (ingredientDto == null || string.IsNullOrWhiteSpace(ingredientDto.Name))
        {
            return BadRequest("Invalid ingredient data.");
        }

        // Normalize the ingredient name to lowercase for case-insensitive comparison
        var normalizedName = ingredientDto.Name.ToLower();

        // Check if an existing ingredient with the same name exists (case-insensitive)
        var existingIngredient = _context.Ingredients
            .Any(i => i.Name.ToLower() == normalizedName);

        if (existingIngredient)
        {
            return BadRequest($"An ingredient with the name {ingredientDto.Name} already exists.");
        }

        var newIngredient = new Ingredients
        {
            Name = ingredientDto.Name,
            Quantity = ingredientDto.Quantity
        };
        _context.Ingredients.Add(newIngredient);
        _context.SaveChanges();

        // Create a response DTO excluding BatchIngredients and IngredientAllergens
        var responseDto = new IngredientNameQuantityDto
        {
            Name = newIngredient.Name,
            Quantity = newIngredient.Quantity
        };

        return CreatedAtAction(nameof(GetIngredientStock), new { ingredientName = responseDto.Name }, responseDto);
    }


    // C. Delete ingredient from stock
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    public IActionResult DeleteIngredient(int id)
    {
        var timestamp = new DateTimeOffset(DateTime.UtcNow);
        var userIdentity = User.Identity.IsAuthenticated ? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value : "anonymous";
        var logInfo = new { Operation = "Delete" , User =  userIdentity, Timestamp = timestamp};
        
        _logger.LogInformation("Delete DeleteIngredient  {@LogInfo} ", logInfo);
        
        var ingredient = _context.Ingredients.Find(id);
        if (ingredient == null)
        {
            return NotFound();
        }

        _context.Ingredients.Remove(ingredient);
        _context.SaveChanges();
        return Ok($"Ingredient {ingredient} stock deleted successfully.");
    }



}