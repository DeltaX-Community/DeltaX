using System;

namespace DeltaX.Modules.Shift.Configuration
{

    public class Crew
    {
        public string Name { get; set; }
        public string Profile { get; set; } 
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
    }
}
