using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bakery.DTOs;

namespace Bakery.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Policy = "RequireAdminRole")]  
public class SearchLogController : ControllerBase
{
    private readonly IMongoCollection<BsonDocument> _logsCollection;
    private readonly ILogger<SearchLogController> _logger;

    public SearchLogController(IMongoClient mongoClient, ILogger<SearchLogController> logger)
    {
        _logger = logger;
        var database = mongoClient.GetDatabase("BakeryLoggingDatabase");
        _logsCollection = database.GetCollection<BsonDocument>("Logs");
    }
    
    [HttpGet(Name="search")]
    public async Task<IActionResult> SearchLogs([FromQuery] LogSearchDto filters)
    {
        var builder = Builders<BsonDocument>.Filter;
        var filter = builder.Empty; // Start with an empty filter

        // Filter by user if provided
        if (!string.IsNullOrEmpty(filters.User))
        {
            filter &= builder.Eq("User", filters.User);
        }

        // Filter by time interval if both start and end times are provided
        if (filters.StartTime.HasValue && filters.EndTime.HasValue)
        {
            filter &= builder.Gte("Timestamp", filters.StartTime.Value) & builder.Lte("Timestamp", filters.EndTime.Value);
        }

        // Filter by operation type if provided
        if (!string.IsNullOrEmpty(filters.Operation))
        {
            filter &= builder.Eq("Operation", filters.Operation);
        }

        // Perform the search in the MongoDB collection
        var logs = await _logsCollection.Find(filter).ToListAsync();

        if (logs.Count == 0)
        {
            return NotFound("No logs found matching the specified criteria.");
        }

        return Ok(logs);
    }
}
    