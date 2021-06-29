
namespace DeltaX.Modules.Shift
{
    using DeltaX.Modules.Shift.Shared.Dtos;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IShiftService : Shared.IShiftService
    {
        event EventHandler<ShiftCrewDto> PublishShiftCrew;

        Task ExecuteAsync(CancellationToken? cancellation); 
    }
}