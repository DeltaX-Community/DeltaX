namespace DeltaX.Modules.Shift.Configuration
{
    using DeltaX.LinSql.Table;
    using DeltaX.Modules.Shift.Shared.Dtos;

    public class ShiftConfiguration
    {
        public DialectType DatabaseDialectType { get; set; }
        public string DatabaseConnectionFactory { get; set; }
        public int CheckShiftIntervalMinutes { get; set; } = 10;
        public ShiftProfileDto[] ShiftProfiles { get; set; }
    }
}
