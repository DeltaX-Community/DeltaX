using DeltaX.Process.RealTimeHistoricDB.Records;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeltaX.Process.RealTimeHistoricDB.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class HistoryController : ControllerBase
    {
        private readonly RealTimeHistoricDbService service;

        public HistoryController(RealTimeHistoricDbService service)
        {
            this.service = service;
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
}
