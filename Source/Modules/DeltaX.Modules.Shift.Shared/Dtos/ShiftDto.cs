namespace DeltaX.Modules.Shift.Shared.Dtos
{
    using System;

    public class ShiftDto
    { 
        public string Name { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
    }
}
