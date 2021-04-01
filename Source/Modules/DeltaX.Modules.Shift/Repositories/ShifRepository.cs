using Dapper;
using DeltaX.Database;
using DeltaX.Modules.Shift.Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeltaX.Modules.Shift.Repositories
{
    class ShifRepository  
    {
        private readonly IDatabaseBase db;

        public ShifRepository(IDatabaseBase db)
        {
            this.db = db;
        }

        public List<ShiftHistoryDto> GetShiftHistory(DateTimeOffset begin, DateTimeOffset? end)
        {
            return db.Run((conn) =>
            {
                return conn
                    .Query<ShiftHistoryDto>(QueriesSqlite.sqlSelectHistoricTag, new {begin, end})
                    .ToList();
            });
        }
    }
}
