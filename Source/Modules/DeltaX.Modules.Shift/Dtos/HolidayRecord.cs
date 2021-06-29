namespace DeltaX.Modules.Shift.Dtos
{
    using System;

    public class HolidayRecord
    {
        public int IdHoliday { get; set; }
        public int IdShiftProfile { get; set; }
        public string Name { get; set; }  
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
        public bool Enable { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
