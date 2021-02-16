using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaX.Process.RealTimeHistoricDB.Records
{
    public class HistoricTagRecord
    {
        public int Id { get; set; }
        public string TagName { get; set; } 
        public DateTime CreatedAt { get; set; }
        public bool Enable { get; set; }
    }
}
