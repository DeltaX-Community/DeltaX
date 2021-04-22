﻿

namespace DeltaX.Modules.Shift
{
    using DeltaX.Modules.Shift.Configuration;
    using DeltaX.Modules.Shift.Dtos;
    using DeltaX.Modules.Shift.Repositories;
    using DeltaX.RealTime;
    using DeltaX.RealTime.Interfaces;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks; 

    class ShiftService : IShiftService
    {
        private readonly ShiftConfiguration configuration;
        private readonly IShiftRepository repository;
        private readonly IRtConnector connector;
        private Dictionary<string, List<ShiftHistoryRecord>> cacheHistoryPatterns = new Dictionary<string, List<ShiftHistoryRecord>>();
        private Dictionary<string, List<ShiftRecord>> cacheShifts = new Dictionary<string, List<ShiftRecord>>();

        public event EventHandler<ShiftCrewDto> PublishShiftCrew;

        public ShiftService(IShiftRepository repository, IOptions<ShiftConfiguration> options, IRtConnector connector = null)
        {
            this.repository = repository;
            this.configuration = options.Value;
            this.connector = connector;
        }

        private (DateTime start, DateTime end)? GetShiftDate(ShiftDto shift, DateTime now)
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

        private void GenerateHistory(string profileName, DateTime begin, DateTime? end = null)
        {
            end ??= DateTime.Now;
            var profile = GetShiftProfiles(profileName, begin, end);

            if (!cacheHistoryPatterns.ContainsKey(profileName))
            {
                cacheHistoryPatterns[profileName] = repository.GetShiftHistory(profile.Name, 
                    profile.Start.LocalDateTime, profile.Start.LocalDateTime.AddDays(profile.CycleDays));
            }
            if (!cacheShifts.ContainsKey(profileName))
            {
                cacheShifts[profileName] = repository.GetShifts(profileName, true).OrderByDescending(s => s.Start).ToList();
            }

            var shiftsToInsert = new List<ShiftHistoryRecord>();
            var historyRecords = cacheHistoryPatterns[profileName];
            var shiftsRecords = cacheShifts[profileName];
            var now = begin;
            while (now < end)
            {
                // El Turno (sifht) está dado por comparacion de horas en el día
                var shift = profile.Shifts.FirstOrDefault(s => GetShiftDate(s, now) != null);
                var shiftRecord = shiftsRecords.FirstOrDefault(s => s.Name == shift.Name);
                if (shift == null || shiftRecord == null)
                {
                    now = now.AddMinutes(10);
                    continue;
                }

                /// La escuadra (crew) se detecta mediante el patternHistory, que sería una parte 
                /// del historico que fue cargada en base a la configuracion
                var patternDay = (now - profile.Start).TotalDays % profile.CycleDays;
                var historyDate = profile.Start.LocalDateTime.AddDays(patternDay);
                var pattern = historyRecords.FirstOrDefault(h => h.Start <= historyDate && h.End > historyDate);

                var shiftDate = GetShiftDate(shift, now).Value;
                shiftsToInsert.Add(new ShiftHistoryRecord
                {
                    Start = shiftDate.start,
                    End = shiftDate.end,
                    IdCrew = pattern?.IdCrew,
                    IdShift = shiftRecord.IdShift,
                    IdShiftProfile = shiftRecord.IdShiftProfile
                });

                now = shiftDate.end;
            }

            lock (repository) repository.InsertShiftHistory(shiftsToInsert);
        }

        private void InsertShiftProfile(ShiftProfileDto profile)
        {
            lock (repository)
            {
                var profileRecord = repository.GetShiftProfile(profile.Name);
                if (profileRecord == null || profileRecord.Start != profile.Start)
                {
                    repository.InsertShiftProfile(profile);
                }

                repository.InsertShiftHistoryFromPattern(profile.Name, profile.CrewPatterns);
            }
        }

        public ShiftCrewDto GetShiftCrew(string profileName, DateTime now)
        {
            var result = repository.GetShiftCrew(profileName, now);

            if (result == null)
            {
                UpdateShift();
                result = repository.GetShiftCrew(profileName, now);
            }
            return result;
        }

        public ShiftProfileDto GetShiftProfiles(string profileName, DateTime? start = null, DateTime? end = null)
        {
            start ??= DateTime.Now;
            end ??= DateTime.Now;
            return configuration.ShiftProfiles
                .Where(p => p.Name == profileName && p.Start <= start && p.End > end)
                .OrderByDescending(p => p.Start)
                .First();
        }

        private ShiftCrewDto currentShiftCrew;

        public void UpdateShift()
        {
            lock (this)
            {
                var now = DateTime.Now;
                foreach (var profile in configuration.ShiftProfiles.Where(p => p.Start <= now && p.End > now))
                {
                    var shiftCrew = repository.GetShiftCrew(profile.Name, now);
                    if (shiftCrew == null)
                    {
                        var lastShift = repository.GetLastShiftHistory(profile.Name);
                        if (lastShift == null)
                        {
                            InsertShiftProfile(profile);
                            shiftCrew = repository.GetShiftCrew(profile.Name, now);
                        }
                    }

                    var lastShiftEnd = shiftCrew?.End ?? profile.Start;
                    if (lastShiftEnd < now)
                    {
                        GenerateHistory(profile.Name, lastShiftEnd.DateTime);
                        shiftCrew = repository.GetShiftCrew(profile.Name, now);
                    }

                    if (currentShiftCrew == null || currentShiftCrew.Start != shiftCrew.Start)
                    {
                        currentShiftCrew = shiftCrew;
                        if (connector != null && !string.IsNullOrWhiteSpace(profile.TagPublish))
                        {
                            connector.SetJson(profile.TagPublish, shiftCrew);
                        }
                        PublishShiftCrew?.Invoke(this, shiftCrew);
                    }
                }
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
                    var interval = configuration.CheckShiftIntervalMinutes;
                    var timeWait = TimeSpan.FromMinutes(interval - DateTime.Now.Minute % interval);
                    await Task.Delay(timeWait, cancellation.Value);
                }
            });
        }
    }
}
