namespace DeltaX.Modules.Shift.Dtos
{
    using System;

    public class ShiftCrewDto
    {
        public int IdShiftHistory { get; set; }
        public string NameShiftProfile { get; set; }
        public int IdShiftProfile { get; set; }
        public string NameShift { get; set; }
        public int IdShift { get; set; }
        public string NameCrew { get; set; }
        public int? IdCrew { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
    }
}
