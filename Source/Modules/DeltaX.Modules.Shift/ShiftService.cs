using DeltaX.Modules.Shift.Configuration;
using DeltaX.Modules.Shift.Repositories;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaX.Modules.Shift
{

    public class ShiftService
    {
        private readonly ShfitConfiguration configuration;
        private readonly IShiftRepository repository;
        private Shfit[] shifts;

        public ShiftService(IShiftRepository repository, IOptions<ShfitConfiguration> options)
        {
            this.repository = repository;
            this.configuration = options.Value;
            
            ValidateShifts();
        }

        public Shfit GetShift(DateTime now)
        {
            return configuration.Shifts.FirstOrDefault(s =>
            {
                var start = now.Date + s.Start;
                var end = now.Date + s.End;

                if (end < start)
                {
                    end = end.AddDays(1);
                }
                return start <= now && now < end;
            });
        }

        public void ValidateShifts()
        { 
            var hours = configuration.Shifts.Sum(s =>
            {
                if (s.End < s.Start)
                {
                    // turno de 18:00 a 02:00 ==>> 24 - (18 - 2) = 24 - 16 = 8 
                    return 24 - (s.Start - s.End).TotalHours;
                }
                // turno de 12:00 a 18:00 => 18 - 12 = 6
                return (s.End - s.Start).TotalHours;
            });

            if (Math.Abs(hours -24) > 0.1)
            {
                throw new Exception($"Shift bad configuration! total hours is {hours} > 24 HH");
            } 
        }

        public Crew GetCrew(DateTime now)
        {
            var isHoliday = configuration.Holidays.Any(h => h.Start <= now && h.End > now);
            if (isHoliday)
            {
                return null;
            }

            var crews = configuration.Crews
                .Where(c => c.Start <= now && (c.End == null || c.End > now))
                .OrderByDescending(c => c.Start)
                .ToList();

            return crews.FirstOrDefault(c =>
            {
                var profile = configuration.CrewProfiles[c.Profile];

                var totalDaysProfile = profile.WorkDays + profile.FreeDays;
                var daysOfCurrentProfile = (now - c.Start).TotalDays % totalDaysProfile;

                if (daysOfCurrentProfile > profile.WorkDays)
                    return false;

                var startShiftToday = now.AddDays(-daysOfCurrentProfile % 1);
                var endShiftToday = startShiftToday.AddMinutes(profile.MinutesByShift);

                return startShiftToday <= now && endShiftToday > now;
            });
        }


        public void UpdateShift()
        {
            var lastShift = repository.GetLastShiftHistory();
            var lastShiftEnd = lastShift.End;

            if (lastShiftEnd < DateTime.Now)
            {
                var t = lastShiftEnd.AddSeconds(1);
                var s = GetShift(t.DateTime);
                // lastShiftEnd = s.End; 
            }
        }


        public Task ExecuteAsync(CancellationToken? cancellation)
        {  
            cancellation ??= CancellationToken.None;
            return Task.Run(async () =>
            { 
                while (!cancellation.Value.IsCancellationRequested)
                {
                    UpdateShift();
                    var t = TimeSpan.FromMinutes(10 - DateTime.Now.Minute % 10);                    
                    await Task.Delay(t, cancellation.Value); 
                }
            });
        }
    }
}
