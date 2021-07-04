namespace DeltaX.Modules.Shift
{
    using DeltaX.Modules.Shift.Shared.Dtos;

    public interface IShiftNotification
    {
        void OnUpdateShiftCrew(ShiftCrewDto shiftCrew);
    }
}