namespace DeltaX.Modules.Shift.Dtos
{
    using System;

    public class ShiftProfileRecord
    {
        public int IdShiftProfile { get; set; }
        public string Name { get; set; } 
        public int CycleDays { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool Enable { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
