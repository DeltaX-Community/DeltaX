namespace DeltaX.Modules.Shift.Shared.Dtos
{
    using System;

    public class ShiftHistoryRecord
    {
        public int IdShiftHistory { get; set; }
        public int IdShiftProfile { get; set; }
        public int IdShift { get; set; }
        public int? IdCrew { get; set; } 
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; } 
        public DateTimeOffset CreatedAt { get; set; }
    }
}
