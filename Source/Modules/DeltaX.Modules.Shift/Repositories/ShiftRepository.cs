namespace DeltaX.Modules.Shift.Repositories
{
    using Dapper;
    using DeltaX.Database;
    using DeltaX.LinSql.Query;
    using DeltaX.Modules.DapperRepository;
    using DeltaX.Modules.Shift.Configuration;
    using DeltaX.Modules.Shift.Dtos;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using MySqlConnector;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;

    class ShiftRepository : DapperRepositoryBase, IShiftRepository
    {
        private readonly ShiftConfiguration configuration;

        public ShiftRepository(ShiftTableQueryFactory queryFactory, IOptions<ShiftConfiguration> options, ILoggerFactory loggerFactory)
            : base(
                 new Database<MySqlConnection>(new[] { options.Value.ConnectionString }, loggerFactory),
                 queryFactory,
                 loggerFactory.CreateLogger("ShifRepository")
                 )
        {
            this.configuration = options.Value;
            this.queryFactory = queryFactory;
            this.logger = loggerFactory.CreateLogger("ShifRepository");

            this.CreateTables();
        }

        public ShiftHistoryRecord GetLastShiftHistory(string profileName)
        {
            (var sql, var param) = new QueryBuilder<ShiftHistoryRecord>()
                .Join<ShiftProfileRecord>((h, p) => h.IdShiftProfile == p.IdShiftProfile)
                .Where((h, p) => p.Name == profileName)
                .Select<ShiftHistoryRecord>()
                .OrderBy(h => h.Start, false)
                .GetSqlParameters(queryFactory);

            return db.RunSync(conn => conn.QueryFirstOrDefault<ShiftHistoryRecord>(sql, param));
        }

        public List<ShiftHistoryRecord> GetShiftHistory(string profileName, DateTime begin, DateTime? end)
        {
            end ??= DateTime.Now;

            (var sql, var param) = new QueryBuilder<ShiftHistoryRecord>()
                .Join<ShiftProfileRecord>((h, p) => h.IdShiftProfile == p.IdShiftProfile)
                .Where((h, p) => p.Name == profileName && h.Start >= begin && h.Start < end)
                .Select<ShiftHistoryRecord>()
                .GetSqlParameters(queryFactory);

            return db.RunSync(conn => conn.Query<ShiftHistoryRecord>(sql, param).ToList());
        }

        public ShiftCrewDto GetShiftCrew(string profileName, DateTime now)
        {
            var query = new QueryBuilder<ShiftHistoryRecord>()
                .Join<ShiftProfileRecord>((h, p) => h.IdShiftProfile == p.IdShiftProfile)
                .Where((h, p) => p.Name == profileName && p.Enable == true && h.Start <= now && h.End > now)
                .Select<ShiftHistoryRecord>()
                .GetSqlParameters();

            var shiftHistory = db.RunSync(conn => conn.QuerySingleOrDefault<ShiftHistoryRecord>(query.sql, query.parameters));
            if (shiftHistory == null)
            {
                return null;
            }
            var shift = base.Get<ShiftRecord>(new { IdShift = shiftHistory.IdShift });
            var crew = base.Get<CrewRecord>(new { IdCrew = shiftHistory.IdCrew });

            return new ShiftCrewDto
            {
                IdShiftHistory = shiftHistory.IdShiftHistory,
                NameCrew = crew?.Name,
                IdCrew = crew?.IdCrew,
                NameShift = shift.Name,
                IdShift = shift.IdShift,
                NameShiftProfile = profileName,
                IdShiftProfile = shiftHistory.IdShiftProfile,
                Start = shiftHistory.Start,
                End = shiftHistory.End
            };
        }

        public ShiftProfileRecord GetShiftProfile(string profileName)
        {
            var now = DateTime.Now;
            (var sql, var param) = new QueryBuilder<ShiftProfileRecord>()
                .Where(p => p.Name == profileName && p.Enable == true && p.Start <= now && p.End > now)
                .SelectAll()
                .OrderBy(p => p.Start, false)
                .Limit(0, 1)
                .GetSqlParameters(queryFactory);

            return db.RunSync(conn => conn.QuerySingleOrDefault<ShiftProfileRecord>(sql, param));
        }

        public List<ShiftRecord> GetShifts(string profileName, bool enable = true)
        {
            (var sql, var param) = new QueryBuilder<ShiftRecord>()
                .Join<ShiftProfileRecord>((s, p) => s.IdShiftProfile == p.IdShiftProfile)
                .Where((s, p) => p.Name == profileName && p.Enable && s.Enable == enable)
                .Select<ShiftRecord>()
                .GetSqlParameters(queryFactory);

            return db.RunSync(conn => conn.Query<ShiftRecord>(sql, param).ToList());
        }

        public int InsertShiftHistory(IEnumerable<ShiftHistoryRecord> shifts)
        {
            return db.Run(conn => InsertShiftHistory(conn, shifts));
        }

        private int InsertShiftHistory(IDbConnection conn, IEnumerable<ShiftHistoryRecord> shifts)
        {
            var sql = queryFactory.GetInsertQuery<ShiftHistoryRecord>();
            logger.LogDebug("InsertShiftHistory sql:{sql} count:{@count}", sql, shifts.Count());
            return conn.Execute(sql, shifts);
        }

        public int InsertShiftHistoryFromPattern(string profileName, IEnumerable<CrewPatternDto> patterns)
        {
            return db.Run(conn =>
            { 
                (string sql, object param) = new QueryBuilder<ShiftProfileRecord>()
                    .Where(sp => sp.Name == profileName && sp.Enable == true)
                    .SelectAll()
                    .OrderBy(sp => sp.IdShiftProfile, false)
                    .OrderBy(sp => sp.CreatedAt, true)
                    .Limit(0, 1)
                    .GetSqlParameters(queryFactory);
                var profile = conn.QuerySingle<ShiftProfileRecord>(sql, param);

                (sql, param) = new QueryBuilder<ShiftRecord>()
                    .Where(p => p.IdShiftProfile == profile.IdShiftProfile && p.Enable == true)
                    .SelectAll()
                    .GetSqlParameters(queryFactory);
                var shifts = conn.Query<ShiftRecord>(sql, param).ToList();

                (sql, param) = new QueryBuilder<CrewRecord>()
                    .Where(p => p.IdShiftProfile == profile.IdShiftProfile && p.Enable == true)
                    .SelectAll()
                    .GetSqlParameters(queryFactory);
                var crews = conn.Query<CrewRecord>(sql, param).ToList();

                var histories = patterns.Select(p =>
                {
                    var shift = shifts.First(s => s.Name == p.Shift);
                    var crew = crews.FirstOrDefault(c => c.Name == p.Crew);
                    var start = profile.Start.Date.AddDays(p.Day - 1) + shift.Start;
                    var end = profile.Start.Date.AddDays(p.Day - 1) + shift.End;
                    if (end < start)
                    {
                        end = end.AddDays(1);
                    }

                    return new ShiftHistoryRecord
                    {
                        IdShiftProfile = profile.IdShiftProfile,
                        IdShift = shift.IdShift,
                        IdCrew = crew?.IdCrew,
                        Start = start,
                        End = end
                    };
                }).ToList();

                return InsertShiftHistory(conn, histories);
            });
        }


        public int InsertShiftProfile(ShiftProfileDto profile)
        {
            var x = Transaction.Current;
            return db.Run(conn =>
            {
                var rows = 0;

                x = Transaction.Current;
                // Insert Profile
                var idShiftProfile = InsertAsync<ShiftProfileRecord, int>(new ShiftProfileRecord
                {
                    Name = profile.Name,
                    CycleDays = profile.CycleDays,
                    Start = profile.Start.LocalDateTime,
                    End = profile.End.LocalDateTime,
                    Enable = true,
                }).Result;
                rows += 1;

                // Insert Shifts
                var shifts = profile.Shifts.Select(s => new ShiftRecord
                {
                    IdShiftProfile = idShiftProfile,
                    Name = s.Name,
                    Start = s.Start,
                    End = s.End,
                    Enable = true
                }).ToList();
                var sql = queryFactory.GetInsertQuery<ShiftRecord>();
                var affected = conn.Execute(sql, shifts);
                rows += affected;

                // Insert Crews
                var crews = profile.Crews.Select(s => new CrewRecord
                {
                    IdShiftProfile = idShiftProfile,
                    Name = s.Name,
                    Enable = true
                }).ToList();
                sql = queryFactory.GetInsertQuery<CrewRecord>();
                affected = conn.Execute(sql, crews);
                rows += affected;

                // Insert Holidays
                var holidays = profile.Holidays.Select(h => new HolidayRecord
                {
                    IdShiftProfile = idShiftProfile,
                    Name = h.Name,
                    Start = h.Start.LocalDateTime,
                    End = h.End.LocalDateTime,
                    Enable = true
                }).ToList();
                sql = queryFactory.GetInsertQuery<HolidayRecord>();
                affected = conn.Execute(sql, holidays);
                rows += affected;

                return rows;
            });
        }

        private int CreateTables()
        {
            return db.Run(conn =>
            {
                logger.LogInformation("Executing CreateTables if not exist...");
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = ShiftQueries.sqlCreateTables;
                    var result = command.ExecuteNonQuery();
                    logger.LogInformation("CreateDatabase Execute result {result}", result);

                    return result;
                }
            });
        }
    }
}
