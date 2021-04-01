using DeltaX.Modules.Shift.Dto;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaX.Modules.Shift.Repositories
{
    public interface IShiftRepository
    {
        List<ShiftHistoryDto> GetShiftHistory(DateTimeOffset begin, DateTimeOffset? end);

        ShiftHistoryDto GetLastShiftHistory();

        void InsertShiftHistory(ShiftHistoryDto shift);
    }
}
