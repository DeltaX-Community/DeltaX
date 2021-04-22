namespace DeltaX.Modules.Shift.Dtos
{
    using System;

    public class HolidayDto
    { 
        public string Name { get; set; } 
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
    }
}
