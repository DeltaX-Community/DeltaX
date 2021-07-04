namespace DeltaX.Modules.Shift
{
    using DeltaX.Modules.Shift.Configuration;
    using DeltaX.Modules.Shift.Shared.Dtos;
    using Microsoft.Extensions.DependencyInjection;
    using DeltaX.RealTime;
    using DeltaX.RealTime.Interfaces;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using DeltaX.Modules.Shift.Shared;

    public class ShiftService : IShiftService
    {
        private Dictionary<string, ShiftCrewDto> currentShiftCrew = new Dictionary<string, ShiftCrewDto>();
        private ManualResetEvent startedEvent;
        private readonly ShiftConfiguration configuration;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<ShiftService> logger;
        private readonly IShiftNotification notification;
        private readonly IRtConnector connector;
        private Dictionary<string, List<ShiftHistoryRecord>> cacheHistoryPatterns = new Dictionary<string, List<ShiftHistoryRecord>>();
        private Dictionary<string, List<ShiftRecord>> cacheShifts = new Dictionary<string, List<ShiftRecord>>();
        private Dictionary<string, List<CrewRecord>> cacheCrews = new Dictionary<string, List<CrewRecord>>();

        public event EventHandler<ShiftCrewDto> PublishShiftCrew;

        public ShiftService(
            IOptions<ShiftConfiguration> options,
            IRtConnector connector = null,
            IServiceProvider serviceProvider = null,
            ILogger<ShiftService> logger = null,
            IShiftNotification notification = null)
        {
            this.configuration = options.Value;
            this.connector = connector;
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            this.notification = notification;
            this.startedEvent = new ManualResetEvent(false);
        }


        public ShiftCrewDto GetShiftCrew(
            string profileName,
            DateTime now)
        {
            ShiftCrewDto result = null;

            if (!startedEvent.WaitOne(TimeSpan.FromMinutes(1)))
            {
                logger.LogWarning("Not Initialized shift service daemon");
                return null;
            }

            if (GetShiftProfile(profileName, now) == null)
            {
                logger.LogWarning("Not profile available with Name:{0} and Date:{1}", profileName, now);
                return null;
            }

            lock (this) Scoped((scope, repository) =>
            {
                result = repository.GetShiftCrew(profileName, now);
                if (result == null)
                {
                    UpdateShift(repository);
                    result = repository.GetShiftCrew(profileName, now);
                }
            });
            return result;
        }

        public ShiftProfileDto GetShiftProfile(
            string profileName,
            DateTime? start = null,
            DateTime? end = null)
        {
            start ??= DateTime.Now;
            end ??= DateTime.Now;
            return configuration.ShiftProfiles
                .Where(p => p.Name == profileName && p.Start <= start && p.End > end)
                .OrderByDescending(p => p.Start)
                .First();
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

        private void TryFillCache(
            IShiftRepository repository,
            ShiftProfileDto profile)
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


        private int GenerateHistoryFromPattern(
            IShiftRepository repository,
            ShiftProfileDto profile)
        {
            TryFillCache(repository, profile);

            var profileName = profile.Name;
            var shiftsToInsert = new List<ShiftHistoryRecord>();
            var shiftsRecords = cacheShifts[profileName];
            var crewRecords = cacheCrews[profileName];

            foreach (var p in profile.CrewPatterns)
            {
                var shiftRecord = shiftsRecords.FirstOrDefault(s => s.Name == p.Shift);
                var crewRecord = crewRecords.FirstOrDefault(c => c.Name == p.Crew);
                var start = profile.Start.Date.AddDays(p.Day - 1) + shiftRecord.Start;
                var _end = profile.Start.Date.AddDays(p.Day - 1) + shiftRecord.End;
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

                cacheHistoryPatterns.Remove(profileName);
            }

            return repository.InsertShiftHistory(shiftsToInsert);
        }

        private int GenerateHistory(
            IShiftRepository repository,
            ShiftProfileDto profile,
            DateTime? begin,
            DateTime? end = null)
        {
            TryFillCache(repository, profile);

            begin ??= DateTime.Now;
            end ??= DateTime.Now;
            var profileName = profile.Name;

            var shiftsToInsert = new List<ShiftHistoryRecord>();
            var historyRecords = cacheHistoryPatterns[profileName];
            var shiftsRecords = cacheShifts[profileName]; 

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

                /// La escuadra (crew) se detecta mediante el patternHistory, que sería una parte 
                /// del historico que fue cargada en base a la configuracion
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

        private void InsertShiftProfile(
            IShiftRepository repository,
            ShiftProfileDto profile)
        {
            var profileRecord = repository.GetShiftProfile(profile.Name);
            if (profileRecord == null || profileRecord.Start != profile.Start)
            {
                repository.InsertShiftProfile(profile);
            }

            if (profile.CrewPatterns?.Any() == true)
            {
                GenerateHistoryFromPattern(repository, profile);
            }
        }

        private void UpdateShiftProfiles(IShiftRepository repository)
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
                    InsertShiftProfile(repository, profile);
                }
            }
        }

        private void UpdateShift(IShiftRepository repository)
        {
            var now = DateTime.Now;
            foreach (var profile in configuration.ShiftProfiles.Where(p => p.Start <= now && p.End > now))
            {
                var shiftCrew = repository.GetShiftCrew(profile.Name, now);
                var lastShift = repository.GetLastShiftHistory(profile.Name);
                if (lastShift == null)
                {
                    InsertShiftProfile(repository, profile);
                    lastShift = repository.GetLastShiftHistory(profile.Name);
                }

                // Add next shift 
                if (lastShift.Start < now)
                {
                    var end = lastShift.End < now ? now.AddDays(1) : lastShift.End.DateTime.AddMinutes(1);
                    GenerateHistory(repository, profile, lastShift.End.DateTime, end);
                    shiftCrew = repository.GetShiftCrew(profile.Name, now);
                }

                var lastShiftEnd = shiftCrew?.End ?? profile.Start;
                if (lastShiftEnd < now)
                {
                    GenerateHistory(repository, profile, lastShiftEnd.DateTime);
                    shiftCrew = repository.GetShiftCrew(profile.Name, now);
                }

                if (currentShiftCrew.GetValueOrDefault(profile.Name)?.Start != shiftCrew.Start)
                {
                    currentShiftCrew[profile.Name] = shiftCrew;
                    if (connector != null && !string.IsNullOrWhiteSpace(profile.TagPublish))
                    {
                        connector.SetJson(profile.TagPublish, shiftCrew);
                    }
                    PublishShiftCrew?.Invoke(this, shiftCrew);
                    notification?.OnUpdateShiftCrew(shiftCrew);
                }
            }
        }

        private void Scoped(Action<IServiceScope, IShiftRepository> action)
        {
            using var scope = this.serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetService<IShiftRepository>();
            try
            {
                repository.UnitOfWork.Begin();
                action.Invoke(scope, repository);
                repository.UnitOfWork.Commit();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Rollback transaction");
                repository.UnitOfWork.Rollback();
                throw;
            }
        }

        public Task ExecuteAsync(CancellationToken? cancellation)
        {
            cancellation ??= CancellationToken.None;
            return Task.Run(async () =>
            {
                lock (this) Scoped((scope, repository) =>
                {
                    repository.CreateTables();
                    UpdateShiftProfiles(repository);
                    UpdateShift(repository);
                });

                logger.LogInformation("Initailized");
                startedEvent.Set();

                while (!cancellation.Value.IsCancellationRequested)
                {
                    lock (this) Scoped((scope, repository) =>
                    {
                        UpdateShift(repository);
                    });

                    var interval = configuration.CheckShiftIntervalMinutes;
                    var timeWait = TimeSpan.FromMinutes(interval - DateTime.Now.Minute % interval);
                    await Task.Delay(timeWait, cancellation.Value);
                }
            });
        }
    }
}
