using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaX.CommonExtensions
{
    public static class DateTimeExtensions
    { 
        public static double ToUnixTimestamp(this DateTime date, bool toUtc = true)
        {
            if (toUtc)
                date = date.ToUniversalTime();
            else
                date = date.ToLocalTime();
            return date.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;
        } 

        public static DateTime FromUnixTimestamp(this double unixTimeStamp, bool fromUtc = true)
        {
            DateTime dateBase = new DateTime(1970, 1, 1, 0, 0, 0, 0, fromUtc ? DateTimeKind.Utc : DateTimeKind.Local);

            return dateBase.AddSeconds(unixTimeStamp).ToLocalTime();
        }
    }
}
