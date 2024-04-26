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
        _logsCollection = database.GetCollection<BsonDocument>("logs");
    }
    
    [HttpGet(Name="search")]
    public async Task<IActionResult> SearchLogs([FromQuery] LogSearchDto filters)
    {
        var builder = Builders<BsonDocument>.Filter;
        var filter = builder.Empty; // Start with an empty filter

        // Filter by user if provided
        if (!string.IsNullOrEmpty(filters.User))
        {
            filter &= builder.Eq("Properties.LogInfo.User", filters.User);
        }

        // Filter by time interval if both start and end times are provided
        if (filters.StartTime.HasValue && filters.EndTime.HasValue)
        {
            filter &= builder.Gte("Properties.LogInfo.Timestamp", filters.StartTime.Value) & builder.Lte("Properties.LogInfo.Timestamp", filters.EndTime.Value);
        }
        else
        {
            // If only one of the two times is provided, log an error and return a 400 Bad Request
            if (filters.StartTime.HasValue || filters.EndTime.HasValue)
            {
                _logger.LogError("Both start and end times must be provided to filter by time interval.");
                return BadRequest("Both start and end times must be provided to filter by time interval.");
            }
        }

        // Filter by operation type if provided
        if (!string.IsNullOrEmpty(filters.Operation))
        {
            filter &= builder.Eq("Properties.LogInfo.Operation", filters.Operation);
        }

        // Perform the search in the MongoDB collection
        var logs = await _logsCollection.Find(filter).ToListAsync();
        
        // Convert each BsonDocument to LogsDto
        var log = logs.Select(log => new LogsDto
        {
            _id = new IdObject { Oid = log["_id"].AsObjectId.ToString() },
            Level = log["Level"].AsString,
            UtcTimeStamp = new DateObject { Date = log["UtcTimeStamp"].AsDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ") },
            Properties = new LogProperties
            {
                LogInfo = new LogInfo
                {
                    Operation = log["Properties"]["LogInfo"]["Operation"].AsString,
                    User = log["Properties"]["LogInfo"]["User"].AsString,
                    Timestamp = log["Properties"]["LogInfo"]["Timestamp"].AsString
                }
            }
        }).ToList();
        
        if (logs.Count == 0)
        {
            return NotFound("No logs found matching the specified criteria.");
        }

        return Ok(log);
    }
}
    