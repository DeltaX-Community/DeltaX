public class RealTimeHistoryDBConfiguration
{
    public string ConnectionString { get; set; }
    public int? CheckTagChangeIntervalMilliseconds { get; set; }
    public int? SaveChangeIntervalSeconds { get; set; }
    public int? DaysPresistence { get; set; }
    public bool UseSwagger { get; set; }
    public string[] CorsUrls { get; set; }
}
