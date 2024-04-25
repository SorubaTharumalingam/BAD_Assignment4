namespace Bakery.DTOs;

public class LogSearchDto
{
    public string User { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Operation { get; set; }  // POST, PUT, DELETE
}
