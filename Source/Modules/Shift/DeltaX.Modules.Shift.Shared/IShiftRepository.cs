namespace DeltaX.Modules.Shift.Shared
{
    using DeltaX.Modules.DapperRepository;
    using DeltaX.Modules.Shift.Shared.Dtos;
    using System;
    using System.Collections.Generic;

    public interface IShiftRepository: IDapperRepository
    {
        List<ShiftHistoryRecord> GetShiftHistory(string profileName, DateTime begin, DateTime? end);
        ShiftHistoryRecord GetLastShiftHistory(string profileName);
        ShiftCrewDto GetShiftCrew(string profileName, DateTime now);
        int InsertShiftHistory(IEnumerable<ShiftHistoryRecord> shifts);
        ShiftProfileRecord GetShiftProfile(string profileName);
        List<ShiftProfileRecord> GetShiftProfiles(bool enable = true); 
        int InsertShiftProfile(ShiftProfileDto profile);
        List<ShiftRecord> GetShifts(string profileName, bool enable = true);
        List<CrewRecord> GetSCrews(string profileName, bool enable = true);
        int CreateTables();
        void DisableShiftProfile(ShiftProfileRecord shiftProfile);
    }
}
