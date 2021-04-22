namespace DeltaX.Modules.Shift.Configuration
{
    using DeltaX.Modules.Shift.Dtos;

    public class ShiftConfiguration
    {
        public string ConnectionString { get; set; }
        public int CheckShiftIntervalMinutes { get; set; } = 10;
        public ShiftProfileDto[] ShiftProfiles { get; set; }
    }
}
