using MongoDB.Bson.Serialization.Attributes;

namespace Bakery.DTOs;

public class LogsDto
{
    public IdObject _id { get; set; }
    public string Level { get; set; }
    public DateObject UtcTimeStamp { get; set; }
    public LogProperties Properties { get; set; }

}

public class IdObject
{
    [BsonElement("$oid")]
    public string Oid { get; set; }
}

public class DateObject
{
    [BsonElement("$date")]
    public string Date { get; set; }
}

public class LogProperties
{
    public LogInfo LogInfo { get; set; }
}

public class LogInfo
{
    public string Operation { get; set; }
    public string User { get; set; }
    public string Timestamp { get; set; }
}
