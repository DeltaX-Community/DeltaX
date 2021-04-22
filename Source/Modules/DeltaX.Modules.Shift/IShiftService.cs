using DeltaX.Modules.Shift.Dtos;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaX.Modules.Shift
{
    public interface IShiftService
    {
        Task ExecuteAsync(CancellationToken? cancellation);
        ShiftCrewDto GetShiftCrew(string profileName, DateTime now);
        ShiftProfileDto GetShiftProfiles(string profileName, DateTime? start = null, DateTime? end = null);
        void UpdateShift();
    }
}