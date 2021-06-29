namespace DeltaX.Modules.Shift.Repositories
{
    using DeltaX.LinSql.Table;
    using DeltaX.Modules.Shift.Shared.Dtos;

    public class ShiftTableQueryFactory : TableQueryFactory
    {
        public ShiftTableQueryFactory(DialectType dialectType = DialectType.MySQL) : base(dialectType)
        {
            this.ConfigureTable<ShiftProfileRecord>("ShiftProfile", cfg =>
            {
                cfg.Identifier = "p";
                cfg.AddColumn(c => c.IdShiftProfile, null, true, true);
                cfg.AddColumn(c => c.Name);
                cfg.AddColumn(c => c.CycleDays);
                cfg.AddColumn(c => c.Start);
                cfg.AddColumn(c => c.End);
                cfg.AddColumn(c => c.Enable);
                cfg.AddColumn(c => c.CreatedAt, p => { p.IgnoreInsert = true; p.IgnoreUpdate = true; });
            });

            this.ConfigureTable<CrewRecord>("Crew", cfg =>
            {
                cfg.Identifier = "c";
                cfg.AddColumn(c => c.IdCrew, null, true, true);
                cfg.AddColumn(c => c.IdShiftProfile);
                cfg.AddColumn(c => c.Name);
                cfg.AddColumn(c => c.Enable);
                cfg.AddColumn(c => c.CreatedAt, p => { p.IgnoreInsert = true; p.IgnoreUpdate = true; });
            });

            this.ConfigureTable<ShiftRecord>("Shift", cfg =>
            {
                cfg.Identifier = "s";
                cfg.AddColumn(c => c.IdShift, null, true, true);
                cfg.AddColumn(c => c.IdShiftProfile);
                cfg.AddColumn(c => c.Name);
                cfg.AddColumn(c => c.Start);
                cfg.AddColumn(c => c.End);
                cfg.AddColumn(c => c.Enable);
                cfg.AddColumn(c => c.CreatedAt, p => { p.IgnoreInsert = true; p.IgnoreUpdate = true; });
            });

            this.ConfigureTable<HolidayRecord>("Holiday", cfg =>
            {
                cfg.Identifier = "h";
                cfg.AddColumn(c => c.IdHoliday, null, true, true);
                cfg.AddColumn(c => c.IdShiftProfile);
                cfg.AddColumn(c => c.Name);
                cfg.AddColumn(c => c.Start);
                cfg.AddColumn(c => c.End);
                cfg.AddColumn(c => c.Enable);
                cfg.AddColumn(c => c.CreatedAt, p => { p.IgnoreInsert = true; p.IgnoreUpdate = true; });
            });

            this.ConfigureTable<ShiftHistoryRecord>("ShiftHistory", cfg =>
            {
                cfg.Identifier = "sh";
                cfg.AddColumn(c => c.IdShiftHistory, null, true, true);
                cfg.AddColumn(c => c.IdShiftProfile, p => { p.IgnoreUpdate = true; });
                cfg.AddColumn(c => c.IdShift, p => { p.IgnoreUpdate = true; });
                cfg.AddColumn(c => c.IdCrew, p => { p.IgnoreUpdate = true; });
                cfg.AddColumn(c => c.Start);
                cfg.AddColumn(c => c.End);
                cfg.AddColumn(c => c.CreatedAt, p => { p.IgnoreInsert = true; p.IgnoreUpdate = true; });
            });
        }
    }
}
