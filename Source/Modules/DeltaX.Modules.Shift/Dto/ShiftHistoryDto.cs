using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaX.Modules.Shift.Dto
{
      
    public class ShiftHistoryDto
    {
        public string Name { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
    }
}
