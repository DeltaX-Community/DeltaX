using System;

namespace DeltaX.Modules.Shift.Configuration
{
    public class Holiday
    {
        public string Name { get; set; } 
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
    }
}
