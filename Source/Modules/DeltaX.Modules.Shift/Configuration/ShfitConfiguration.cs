using System.Collections.Generic;

namespace DeltaX.Modules.Shift.Configuration
{

    public class ShfitConfiguration
    {
        public Shfit[] Shifts { get; set; }
        public Crew[] Crews { get; set; }
        public Holiday[] Holidays { get; set; }
        public Dictionary<string, CrewProfile>  CrewProfiles { get; set; }
    }
}
