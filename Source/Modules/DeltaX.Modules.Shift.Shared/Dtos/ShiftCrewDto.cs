namespace DeltaX.Modules.Shift.Shared.Dtos
{
    using System;

    public class ShiftCrewDto : ShiftHistoryRecord
    {
        public string NameShiftProfile { get; set; }
        public string NameShift { get; set; }
        public string NameCrew { get; set; } 
    }
}
