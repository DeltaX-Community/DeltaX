namespace DeltaX.Modules.Shift.Dtos
{
    using System;

    public class CrewRecord
    { 
        public int IdCrew { get; set; }
        public int IdShiftProfile { get; set; }
        public string Name { get; set; }
        public bool Enable { get; set; }
        public DateTimeOffset CreatedAt{ get; set; }
    }
}
