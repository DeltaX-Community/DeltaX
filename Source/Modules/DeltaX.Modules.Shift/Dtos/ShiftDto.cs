namespace DeltaX.Modules.Shift.Dtos
{
    using System;

    public class ShiftDto
    { 
        public string Name { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
    }
}
