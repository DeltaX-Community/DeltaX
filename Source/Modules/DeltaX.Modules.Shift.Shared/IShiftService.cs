namespace DeltaX.Modules.Shift.Shared
{
    using DeltaX.Modules.Shift.Shared.Dtos;
    using System;

    public interface IShiftService
    {  
        ShiftCrewDto GetShiftCrew(string profileName, DateTime now);
        ShiftProfileDto GetShiftProfiles(string profileName, DateTime? start = null, DateTime? end = null); 
    }
}