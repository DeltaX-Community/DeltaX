using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaX.Process.RealTimeHistoricDB.Configuration
{
    public class RealTimeHistoryDBConfiguration
    {
        public string ConnectionString { get; set; }
        public int? CheckTagChangeIntervalMilliseconds { get; set; }
        public int? SaveChangeIntervalSeconds { get; set; }
        public int? DaysPresistence { get; set; }
        public bool UseSwagger { get; set; } 
        public string[] Cors { get; set; }
    }
}
