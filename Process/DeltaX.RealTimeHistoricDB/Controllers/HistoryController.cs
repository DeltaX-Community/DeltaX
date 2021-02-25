using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;


[Route("api/v1/[controller]")]
[ApiController]
public class HistoryController : ControllerBase
{
    private readonly RealTimeHistoricDbService service;

    public HistoryController(RealTimeHistoricDbService service)
    {
        this.service = service;
    }

    [HttpGet("topics")]
    public List<HistoricTagRecord> GetTopics()
    {
        return service.GetTopics();
    }

     
    [HttpGet("topic/{*tagName}")]
    public List<HistoricTagValueDto> GetTopicHistory(
        string tagName,
        [FromQuery] DateTimeOffset? beginDateTime,
        [FromQuery] DateTimeOffset? endDateTime,
        [FromQuery] int maxPoints = 1000,
        [FromQuery] int lastSeconds = 30,
        [FromQuery] bool strictMode = false
        )
    {
        tagName = tagName.Replace("%2F", "/");
        if (!endDateTime.HasValue)
        {
            endDateTime = DateTime.Now;
        }
        if (!beginDateTime.HasValue)
        {
            beginDateTime = endDateTime.Value.AddSeconds(-lastSeconds);
        }
        if (beginDateTime >= endDateTime)
        {
            throw new ArgumentException("Bad Parameters: beginDateTime >= endDateTime");
        }

        return this.service.GetTagHistory(tagName, beginDateTime.Value.LocalDateTime, endDateTime.Value.LocalDateTime, maxPoints, null, strictMode);
    }
}