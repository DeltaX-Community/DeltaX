namespace DeltaX.Modules.Shift.Repositories
{
    using Dapper;
    using DeltaX.LinSql.Query;
    using DeltaX.Modules.DapperRepository;
    using DeltaX.Modules.Shift.Shared.Dtos;
    using DeltaX.Modules.Shift.Shared;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    class ShiftRepository : DapperRepositoryBase, IShiftRepository
    {
        public ShiftRepository(
            ShiftTableQueryFactory queryFactory,
            IShiftUnitOfWork uow,
            ILogger<ShiftRepository> logger)
            : base(uow, queryFactory, logger)
        {
        }

        public ShiftHistoryRecord GetLastShiftHistory(string profileName)
        {
            (var sql, var param) = new QueryBuilder<ShiftHistoryRecord>()
                .Join<ShiftProfileRecord>((h, p) => h.IdShiftProfile == p.IdShiftProfile)
                .Where((h, p) => p.Name == profileName && p.Enable)
                .Select<ShiftHistoryRecord>()
                .OrderBy(h => h.Start, false)
                .GetSqlParameters(queryFactory);

            return Conn.QueryFirstOrDefault<ShiftHistoryRecord>(sql, param, Tran);
        }

        public List<ShiftHistoryRecord> GetShiftHistory(
            string profileName,
            DateTime begin,
            DateTime? end)
        {
            end ??= DateTime.Now;

            (var sql, var param) = new QueryBuilder<ShiftHistoryRecord>()
                .Join<ShiftProfileRecord>((h, p) => h.IdShiftProfile == p.IdShiftProfile)
                .Where((h, p) => p.Name == profileName && p.Enable && h.Start >= begin && h.Start < end)
                .Select<ShiftHistoryRecord>()
                .GetSqlParameters(queryFactory);

            return Conn.Query<ShiftHistoryRecord>(sql, param, Tran).ToList();
        }

        public ShiftCrewDto GetShiftCrew(
            string profileName,
            DateTime now)
        {
            var query = new QueryBuilder<ShiftHistoryRecord>()
                .Join<ShiftProfileRecord>((h, p) => h.IdShiftProfile == p.IdShiftProfile)
                .Join<CrewRecord>((h, p, c) => h.IdCrew == c.IdCrew, JoinType.LeftJoin)
                .Join<ShiftRecord>((h, p, c, s) => h.IdShift == s.IdShift)
                .Where((h, p, c, s) => p.Name == profileName && p.Enable == true && h.Start <= now && h.End > now)
                .Select<ShiftHistoryRecord>()
                .Select((h, p, c, s) => p.Name, nameof(ShiftCrewDto.NameShiftProfile))
                .Select((h, p, c, s) => s.Name, nameof(ShiftCrewDto.NameShift))
                .Select((h, p, c, s) => c.Name, nameof(ShiftCrewDto.NameCrew))
                .Limit(0, 1)
                .GetSqlParameters();

            // logger.LogDebug("GetShiftCrew {@sql}\n{@param}", query.sql, query.parameters);
            return Conn.QuerySingleOrDefault<ShiftCrewDto>(query.sql, query.parameters, Tran);
        }

        public void DisableShiftProfile(ShiftProfileRecord shiftProfile)
        {
            shiftProfile.Enable = false;
            this.UpdateAsync(shiftProfile);
        }

        public ShiftProfileRecord GetShiftProfile(string profileName)
        {
            var now = DateTime.Now;
            (var sql, var param) = new QueryBuilder<ShiftProfileRecord>()
                .Where(p => p.Name == profileName && p.Enable && p.Start <= now && p.End > now)
                .SelectAll()
                .OrderBy(p => p.Start, false)
                .Limit(0, 1)
                .GetSqlParameters(queryFactory);

            return Conn.QuerySingleOrDefault<ShiftProfileRecord>(sql, param, Tran);
        }

        public List<ShiftRecord> GetShifts(
            string profileName,
            bool enable = true)
        {
            (var sql, var param) = new QueryBuilder<ShiftRecord>()
                .Join<ShiftProfileRecord>((s, p) => s.IdShiftProfile == p.IdShiftProfile)
                .Where((s, p) => p.Name == profileName && p.Enable && s.Enable == enable)
                .Select<ShiftRecord>()
                .GetSqlParameters(queryFactory);

            return Conn.Query<ShiftRecord>(sql, param, Tran).ToList();
        }

        public List<ShiftProfileRecord> GetShiftProfiles()
        {
            (var sql, var param) = new QueryBuilder<ShiftProfileRecord>()
                .Where((p) => p.Enable)
                .Select<ShiftProfileRecord>()
                .GetSqlParameters(queryFactory);

            return Conn.Query<ShiftProfileRecord>(sql, param, Tran).ToList();
        }

        public List<CrewRecord> GetSCrews(
            string profileName,
            bool enable = true)
        {
            (var sql, var param) = new QueryBuilder<CrewRecord>()
                .Join<ShiftProfileRecord>((s, p) => s.IdShiftProfile == p.IdShiftProfile)
                .Where((s, p) => p.Name == profileName && p.Enable && s.Enable == enable)
                .Select<CrewRecord>()
                .GetSqlParameters(queryFactory);

            return Conn.Query<CrewRecord>(sql, param, Tran).ToList();
        }

        public int InsertShiftHistory(IEnumerable<ShiftHistoryRecord> shifts)
        {
            var sql = queryFactory.GetInsertQuery<ShiftHistoryRecord>();
            logger.LogDebug("InsertShiftHistory sql:{sql} count:{@count}", sql, shifts.Count());
            return Conn.Execute(sql, shifts, Tran);
        }

        public int InsertShiftProfile(ShiftProfileDto profile)
        {
            var rows = 0;

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
            var affected = Conn.Execute(sql, shifts, Tran);
            rows += affected;

            // Insert Crews
            var crews = profile.Crews.Select(s => new CrewRecord
            {
                IdShiftProfile = idShiftProfile,
                Name = s.Name,
                Enable = true
            }).ToList();
            sql = queryFactory.GetInsertQuery<CrewRecord>();
            affected = Conn.Execute(sql, crews, Tran);
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
            affected = Conn.Execute(sql, holidays, Tran);
            rows += affected;

            return rows;
        }

        public int CreateTables()
        {
            logger.LogInformation("Executing CreateTables if not exist...");
            using (var command = Conn.CreateCommand())
            {
                command.Transaction = Tran;
                command.CommandType = CommandType.Text;
                command.CommandText = ShiftQueries.sqlCreateTables;
                var result = command.ExecuteNonQuery();
                logger.LogInformation("CreateDatabase Execute result {result}", result);

                return result;
            }
        }
    }
}
