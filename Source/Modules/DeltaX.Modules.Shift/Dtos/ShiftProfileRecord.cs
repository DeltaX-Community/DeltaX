namespace DeltaX.Modules.Shift.Dtos
{
    using System;

    public class ShiftProfileRecord
    {
        public int IdShiftProfile { get; set; }
        public string Name { get; set; } 
        public int CycleDays { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
        public bool Enable { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
