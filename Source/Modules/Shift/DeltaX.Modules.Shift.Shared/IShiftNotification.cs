namespace DeltaX.Modules.Shift.Shared
{
    using DeltaX.Modules.Shift.Shared.Dtos;

    public interface IShiftNotification
    {
        void OnUpdateShiftCrew(ShiftCrewDto shiftCrew);
    }
}