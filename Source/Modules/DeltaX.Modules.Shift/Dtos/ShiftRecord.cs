namespace DeltaX.Modules.Shift.Dtos
{
    using System;

    public class ShiftRecord
    {
        public int IdShift { get; set; }
        public int IdShiftProfile { get; set; }
        public string Name { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public bool Enable { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
