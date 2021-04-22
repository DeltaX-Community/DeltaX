using DeltaX.Modules.Shift.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaX.Modules.Shift.Repositories
{
    public interface IShiftRepository
    {
        List<ShiftHistoryRecord> GetShiftHistory(string profileName, DateTime begin, DateTime? end);
        ShiftHistoryRecord GetLastShiftHistory(string profileName);
        ShiftCrewDto GetShiftCrew(string profileName, DateTime now);
        int InsertShiftHistory(IEnumerable<ShiftHistoryRecord> shifts);
        ShiftProfileRecord GetShiftProfile(string profileName);
        int InsertShiftHistoryFromPattern(string profileName, IEnumerable<CrewPatternDto> patterns);
        int InsertShiftProfile(ShiftProfileDto profile);
        List<ShiftRecord> GetShifts(string profileName, bool enable = true);
    }
}
