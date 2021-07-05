namespace DeltaX.Modules.Shift
{
    using DeltaX.Modules.Shift.Configuration;
    using DeltaX.Modules.Shift.Shared.Dtos;
    using DeltaX.RealTime;
    using DeltaX.RealTime.Interfaces;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using DeltaX.Modules.Shift.Shared;

    public class ShiftServiceScoped
    {
        private Dictionary<string, ShiftCrewDto> currentShiftCrew = new Dictionary<string, ShiftCrewDto>();
        private readonly ShiftConfiguration configuration;
        private readonly IShiftRepository repository;
        private readonly ILogger<ShiftService> logger;  
        private static Dictionary<string, List<ShiftHistoryRecord>> cacheHistoryPatterns = new Dictionary<string, List<ShiftHistoryRecord>>();
        private static Dictionary<string, List<ShiftRecord>> cacheShifts = new Dictionary<string, List<ShiftRecord>>();
        private static Dictionary<string, List<CrewRecord>> cacheCrews = new Dictionary<string, List<CrewRecord>>();

        public event EventHandler<ShiftCrewDto> PublishShiftCrew;

        public ShiftServiceScoped(
            IOptions<ShiftConfiguration> options,
            IShiftRepository repository, 
            ILogger<ShiftService> logger = null)
        {
            this.configuration = options.Value;
            this.repository = repository; 
            this.logger = logger; 
        }


        public ShiftCrewDto GetShiftCrew(
            string profileName,
            DateTime now)
        {
            var result = repository.GetShiftCrew(profileName, now);
            if (result == null)
            {
                UpdateShift();
                result = repository.GetShiftCrew(profileName, now);
            }
            return result;
        }

        private (DateTime start, DateTime end)? GetShiftDate(
            ShiftDto shift,
            DateTime now)
        {
            var start = now.Date + shift.Start;
            var end = now.Date + shift.End;

            if (start > end && start <= now)
            {
                end = end.AddDays(1);
            }
            if (start > end && end > now)
            {
                start = start.AddDays(-1);
            }

            if (start <= now && now < end)
            {
                return (start, end);
            }
            return null;
        }

        private void TryFillCache(ShiftProfileDto profile)
        {
            var profileName = profile.Name;
            var begin = profile.Start.LocalDateTime;
            var end = profile.Start.LocalDateTime.AddDays(profile.CycleDays);

            if (!cacheHistoryPatterns.ContainsKey(profileName))
            {
                cacheHistoryPatterns[profileName] = repository.GetShiftHistory(profileName, begin, end);
            }
            if (!cacheShifts.ContainsKey(profileName))
            {
                cacheShifts[profileName] = repository.GetShifts(profileName, true);
            }
            if (!cacheCrews.ContainsKey(profileName))
            {
                cacheCrews[profileName] = repository.GetSCrews(profileName, true);
            }
        }

        private int GenerateHistoryFromPattern(ShiftProfileDto profile)
        {
            TryFillCache(profile);

            var shiftsToInsert = new List<ShiftHistoryRecord>();
            var shiftsRecords = cacheShifts[profile.Name];
            var crewRecords = cacheCrews[profile.Name];

            foreach (var pattern in profile.CrewPatterns)
            {
                var shiftRecord = shiftsRecords.FirstOrDefault(s => s.Name == pattern.Shift);
                var crewRecord = crewRecords.FirstOrDefault(c => c.Name == pattern.Crew);
                var start = profile.Start.Date.AddDays(pattern.Day - 1) + shiftRecord.Start;
                var _end = profile.Start.Date.AddDays(pattern.Day - 1) + shiftRecord.End;
                if (_end < start)
                {
                    _end = _end.AddDays(1);
                }
                shiftsToInsert.Add(new ShiftHistoryRecord
                {
                    Start = start,
                    End = _end,
                    IdCrew = crewRecord?.IdCrew,
                    IdShift = shiftRecord.IdShift,
                    IdShiftProfile = shiftRecord.IdShiftProfile
                });

                cacheHistoryPatterns.Remove(profile.Name);
                cacheShifts.Remove(profile.Name);
                cacheCrews.Remove(profile.Name);
            }

            return repository.InsertShiftHistory(shiftsToInsert);
        }

        private int GenerateHistory(
            ShiftProfileDto profile,
            DateTime? begin,
            DateTime? end = null)
        {
            TryFillCache(profile);

            begin ??= DateTime.Now;
            end ??= DateTime.Now;

            var shiftsToInsert = new List<ShiftHistoryRecord>();
            var historyRecords = cacheHistoryPatterns[profile.Name];
            var shiftsRecords = cacheShifts[profile.Name];

            var date = begin.Value;
            while (date < end)
            {
                // El Turno (sifht) está dado por comparacion de horas en el día
                var shift = profile.Shifts.FirstOrDefault(s => GetShiftDate(s, date) != null);
                var shiftRecord = shiftsRecords.FirstOrDefault(s => s.Name == shift?.Name);
                if (shift == null || shiftRecord == null)
                {
                    date = date.AddMinutes(10);
                    continue;
                }

                // La escuadra (crew) se detecta mediante el patternHistory, que sería una parte 
                // del historico que fue cargada en base a la configuracion
                var patternDay = (date - profile.Start).TotalDays % profile.CycleDays;
                var historyDate = profile.Start.LocalDateTime.AddDays(patternDay);
                var pattern = historyRecords?.FirstOrDefault(h => h.Start <= historyDate && h.End > historyDate);

                var shiftDate = GetShiftDate(shift, date).Value;
                shiftsToInsert.Add(new ShiftHistoryRecord
                {
                    Start = shiftDate.start,
                    End = shiftDate.end,
                    IdCrew = pattern?.IdCrew,
                    IdShift = shiftRecord.IdShift,
                    IdShiftProfile = shiftRecord.IdShiftProfile
                });

                date = shiftDate.end;
            }

            return repository.InsertShiftHistory(shiftsToInsert);
        }

        private void InsertShiftProfile(ShiftProfileDto profile)
        {
            var profileRecord = repository.GetShiftProfile(profile.Name);
            if (profileRecord == null || profileRecord.Start != profile.Start)
            {
                repository.InsertShiftProfile(profile);
            }

            if (profile.CrewPatterns?.Any() == true)
            {
                GenerateHistoryFromPattern(profile);
            }
        }

        public void UpdateShiftProfiles()
        {
            var now = DateTime.Now;
            var dbProfiles = repository.GetShiftProfiles();
            foreach (var profile in configuration.ShiftProfiles.Where(p => p.Start <= now && p.End > now))
            {
                var profiles = dbProfiles.Where(p => p.Name == profile.Name);
                var disableProfile = profiles.Where(p => p.Start != profile.Start);
                if (disableProfile.Any())
                {
                    foreach (var p in disableProfile)
                    {
                        repository.DisableShiftProfile(p);
                    }
                    profiles = null;
                }

                if (profiles == null || !profiles.Any())
                {
                    InsertShiftProfile(profile);
                }
            }
        }

        public void UpdateShift()
        {
            var now = DateTime.Now;
            foreach (var profile in configuration.ShiftProfiles.Where(p => p.Start <= now && p.End > now))
            {
                var shiftCrew = repository.GetShiftCrew(profile.Name, now);
                var lastShift = repository.GetLastShiftHistory(profile.Name);
                if (lastShift == null)
                {
                    InsertShiftProfile(profile);
                    lastShift = repository.GetLastShiftHistory(profile.Name);
                }

                // Add next shift 
                if (lastShift.Start < now)
                {
                    var end = lastShift.End < now ? now.AddDays(1) : lastShift.End.DateTime.AddMinutes(1);
                    GenerateHistory(profile, lastShift.End.DateTime, end);
                    shiftCrew = repository.GetShiftCrew(profile.Name, now);
                }

                var lastShiftEnd = shiftCrew?.End ?? profile.Start;
                if (lastShiftEnd < now)
                {
                    GenerateHistory(profile, lastShiftEnd.DateTime);
                    shiftCrew = repository.GetShiftCrew(profile.Name, now);
                }

                if (currentShiftCrew.GetValueOrDefault(profile.Name)?.Start != shiftCrew.Start)
                {
                    currentShiftCrew[profile.Name] = shiftCrew; 
                    PublishShiftCrew?.Invoke(this, shiftCrew); 
                }
            }
        }
    }
}
