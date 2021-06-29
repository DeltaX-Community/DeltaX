namespace DeltaX.Modules.Shift.Shared.Dtos
{
    using System;

    public class ShiftProfileDto
    { 
        public string Name { get; set; }
        public string TagPublish{ get; set; }
        public int CycleDays { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; } 
        public ShiftDto[] Shifts { get; set; } 
        public CrewDto[] Crews { get; set; } 
        public CrewPatternDto[] CrewPatterns { get; set; } 
        public HolidayDto[] Holidays { get; set; }        
    }
}
