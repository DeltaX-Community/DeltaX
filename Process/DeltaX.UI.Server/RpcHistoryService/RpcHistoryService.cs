using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;

public class RpcHistoryService
{
    UIConfiguration settings;
    private HttpClient httpClient;
    private Uri baseUri;
    private ILogger logger;

    public RpcHistoryService(
        IOptions<UIConfiguration> options, 
        ILogger<WorkerService> logger)
    {
        this.settings = options.Value;
        this.logger = logger;
        this.httpClient = new HttpClient();
        this.baseUri = new Uri(settings.RealTimeHistoryBasePath ?? "http://127.0.0.1:5088");
    }

    public List<HistoricTagValueDto> GetTopicHistory(
        string tagName,
        DateTimeOffset? beginDateTime = null,
        DateTimeOffset? endDateTime = null,
        int? maxPoints = null,
        int? lastSeconds = null,
        bool? strictMode = null)
    {
        var param = new List<string>();

        if (beginDateTime.HasValue) param.Add($"beginDateTime={beginDateTime.Value}");
        if (endDateTime.HasValue) param.Add($"endDateTime={endDateTime.Value}");
        if (maxPoints.HasValue) param.Add($"maxPoints={maxPoints.Value}");
        if (lastSeconds.HasValue) param.Add($"lastSeconds={lastSeconds.Value}");
        if (strictMode.HasValue) param.Add($"strictMode={strictMode.Value}");

        string relativeUri = $"/api/v1/History/topic/{tagName}?{string.Join("&", param)}";
        var uri = new Uri(baseUri, relativeUri.TrimEnd('?'));

        logger.LogInformation("GetFromJsonAsync Uri:{uri}", uri);
        return httpClient.GetFromJsonAsync<List<HistoricTagValueDto>>(uri).Result;
    }
}

