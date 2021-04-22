namespace DeltaX.Modules.Shift.Dtos
{
    using System;

    public class ShiftHistoryRecord
    {
        public int IdShiftHistory { get; set; }
        public int IdShiftProfile { get; set; }
        public int IdShift { get; set; }
        public int? IdCrew { get; set; } 
        public DateTime Start { get; set; }
        public DateTime End { get; set; } 
        public DateTime CreatedAt { get; set; }
    }
}
