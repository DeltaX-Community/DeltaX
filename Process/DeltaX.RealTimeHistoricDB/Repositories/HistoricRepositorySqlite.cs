using Dapper;
using DeltaX.CommonExtensions;
using DeltaX.Database;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq; 


class HistoricRepositorySqlite : IHistoricRepository
{
    private RealTimeHistoryDBConfiguration configuration;
    private Database<SqliteConnection> dbHistory;
    private ILogger logger;

    public HistoricRepositorySqlite(IOptions<RealTimeHistoryDBConfiguration> options, ILoggerFactory loggerFactory)
    {
        this.configuration = options.Value;
        this.logger = loggerFactory.CreateLogger(nameof(HistoricRepositorySqlite));
        this.dbHistory = new Database<SqliteConnection>(new[] { this.configuration.ConnectionString }, loggerFactory);
    }

    public List<HistoricTagRecord> GetListHistoricTags()
    {
        return dbHistory.Run((conn) =>
        {
            return conn
                .Query<HistoricTagRecord>(QueriesSqlite.sqlSelectHistoricTag)
                .ToList();
        });
    }

    public List<HistoricTagRecord> GetInsertTags(IEnumerable<string> tagsToAdd)
    {
        lock (dbHistory) return dbHistory.RunTransaction((conn, transaction) =>
        {
            var existentTag = conn
                .Query<HistoricTagRecord>(QueriesSqlite.sqlSelectHistoricTag)
                .Select(t => t.TagName).ToList();

            var param = tagsToAdd
                .Where(t => !existentTag.Contains(t))
                .Select(t => new { TagName = t, Enable = 1 });

            if (param.Any())
            {
                conn.Execute(QueriesSqlite.sqlInsertHistoricTag, param);
            }

            var allTags = conn
                .Query<HistoricTagRecord>(QueriesSqlite.sqlSelectHistoricTag)
                .ToList();

            return allTags.Where(t => tagsToAdd.Contains(t.TagName)).ToList();
        });
    }

    public int SaveHistoricTagValues(List<HistoricTagValueRecord> tagsValues)
    {
        lock (dbHistory) return dbHistory.RunTransaction((conn, transaction) =>
        {
            try
            {
                var watch = Stopwatch.StartNew();
                    // var transaction = conn.BeginTransaction();
                    var ret = conn.Execute(QueriesSqlite.sqlInsertHistoricTagValue, tagsValues);
                    // transaction.Commit();
                    watch.Stop();

                logger.LogInformation("Execute affect rows:{0} ElapsedSeconds:{1}", ret, watch.ElapsedMilliseconds / 1000.0);
                    // tagsGenerator.DatabaseStatics("SaveHistoricTagValues", $"status|1|ElapsedMilliseconds|{watch.ElapsedMilliseconds}|rows|{ret}");
                    return ret;
            }
            catch (Exception e)
            {
                logger.LogError(e, "SaveHistoricTagValues Error");
                    // tagsGenerator.DatabaseStatics("SaveHistoricTagValues", $"status|0|Error|Exception");
                }
            return -1;
        });
    }

    public int CreateTables()
    {
        lock (dbHistory) return dbHistory.Run((conn) =>
        {
            logger.LogInformation("Executing CreateTables if not exist...");

            using (var command = conn.CreateCommand())
            {
                command.CommandText = QueriesSqlite.sqlCreateTables;
                var result = command.ExecuteNonQuery();
                logger.LogInformation("CreateDatabase Execute result {result}", result);

                return result;
            }
        });
    }


    public List<HistoricTagValueDto> GetTagHistory(
        string tagName,
        double beginDateTime,
        double endDateTime,
        int maxPoints = 10000,
        string prevValue = null)
    {
        return dbHistory.Run((conn) =>
        {
            var result = new List<HistoricTagValueDto>();

            try
            {
                var existentTag = conn.QueryFirst<HistoricTagRecord>(QueriesSqlite.sqlSelectHistoricTagByName, new { tagName });

                var watch = System.Diagnostics.Stopwatch.StartNew();
                var paso = Math.Round((endDateTime - beginDateTime) / (maxPoints - 1), 6);
                var endDT = Math.Round(beginDateTime + ((maxPoints - 1) * paso), 6);

                using (var objCommand = conn.CreateCommand())
                {
                    objCommand.CommandText = QueriesSqlite.sqlSelectHistoricTagValue;
                    objCommand.Parameters.AddWithValue("@tagId", existentTag.Id);
                    objCommand.Parameters.AddWithValue("@beginDateTime", beginDateTime);
                    objCommand.Parameters.AddWithValue("@endDateTime", endDT);

                    using (var reader = objCommand.ExecuteReader())
                    {
                        var t = beginDateTime;
                        string value = null;
                        double updated = t;
                        double prevUpdated = t;

                        while (t <= endDT + 0.0001)
                        {
                            if (reader.Read())
                            {
                                updated = reader.GetDouble(0);
                                value = reader.IsDBNull(1) ? null : reader.GetString(1);
                                if (updated <= t)
                                {
                                    prevUpdated = updated;
                                    prevValue = value;
                                    continue;
                                }

                                result.Add(new HistoricTagValueDto { Updated = prevUpdated.FromUnixTimestamp(), Value = prevValue });
                                prevUpdated = updated;
                                prevValue = value;
                            }

                            do
                            {
                                t = Math.Round(t + paso, 3);
                            } while (updated > t && t < endDT);
                        }

                        result.Add(new HistoricTagValueDto { Updated = prevUpdated.FromUnixTimestamp(), Value = prevValue });

                        if (result.Count == 0)
                        {
                            logger.LogWarning("La consulta GetHistoric no trajo resultados");
                        }
                    }
                }
                watch.Stop();
                if (watch.ElapsedMilliseconds > 1000)
                {
                    logger.LogInformation($"GetHistoric tagName:{tagName} ElapsedSeconds:{watch.ElapsedMilliseconds / 1000.0} " +
                        $"from{beginDateTime.FromUnixTimestamp()} To:{endDateTime.FromUnixTimestamp()}");
                }

                    // tagsGenerator.DatabaseStatics("GetHistoric", $"status|1|ElapsedMilliseconds|{watch.ElapsedMilliseconds}" +
                    //     $"|tagId|{tagId}" +
                    //     $"|beginDateTime|{beginDateTime.ToDateTime(true).ToString("o")}" +
                    //     $"|endDateTime|{endDateTime.ToDateTime(true).ToString("o")}" +
                    //     $"|rows|{result.Count}");
                }
            catch (Exception e)
            {
                logger.LogError(e, "GetHistoric Fail");
                    //   tagsGenerator.DatabaseStatics("GetHistoric", $"status|0|Error|Exception");
                }
            return result;
        });
    }


    public int DeleteOldsHistoricTagValues(int daysPresistence)
    {
        lock (dbHistory) return dbHistory.RunTransaction((conn, transaction) =>
        {
            int ret = -1;
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                ret = conn.Execute(QueriesSqlite.sqlDeleteHistoricTagValue, new { daysPresistence });

                watch.Stop();

                logger.LogInformation("Execute DeleteHistoricTagValues deleted rows:{0} ElapsedSeconds:{1}",
                    ret, watch.ElapsedMilliseconds / 1000.0);
                    // tagsGenerator.DatabaseStatics("DeleteHistoricTagValues", $"status|1|ElapsedMilliseconds|{watch.ElapsedMilliseconds}|rows|{ret}");
                }
            catch (Exception e)
            {
                logger.LogError(e, "DeleteHistoricTagValues Error ");
                    // tagsGenerator.DatabaseStatics("DeleteHistoricTagValues", "status|0|Error|Exception");
                }
            return ret;
        });
    }

}