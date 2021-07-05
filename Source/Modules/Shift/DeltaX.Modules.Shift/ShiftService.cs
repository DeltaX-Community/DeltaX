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

            ShiftCrewDto result = null;
            lock (this) Scoped((_, shiftScope) =>
            {
                result = shiftScope.GetShiftCrew(profileName, now);
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

        private void Scoped(Action<IServiceScope, ShiftServiceScoped> action)
        {
            using var scope = this.serviceProvider.CreateScope();

            var repository = scope.ServiceProvider.GetService<IShiftRepository>();
            var shiftScope = scope.ServiceProvider.GetService<ShiftServiceScoped>();
            shiftScope.PublishShiftCrew += ShiftScopedPublishShiftCrew;
            try
            {
                repository.UnitOfWork.BeginTransaction();
                action.Invoke(scope, shiftScope);
                repository.UnitOfWork.CommitTransaction();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Rollback transaction");
                repository.UnitOfWork.RollbackTransaction();
                throw;
            }
            finally
            {
                shiftScope.PublishShiftCrew -= ShiftScopedPublishShiftCrew;
                scope.Dispose();
            }
        }

        private void ShiftScopedPublishShiftCrew(object sender, ShiftCrewDto shiftCrew)
        {
            var profile = GetShiftProfile(shiftCrew.NameShiftProfile);
            if (profile != null && connector != null && !string.IsNullOrWhiteSpace(profile.TagPublish))
            {
                connector.SetJson(profile.TagPublish, shiftCrew);
            }

            PublishShiftCrew?.Invoke(this, shiftCrew);
            notification?.OnUpdateShiftCrew(shiftCrew);
        }

        public Task ExecuteAsync(CancellationToken? cancellation)
        {
            cancellation ??= CancellationToken.None;
            return Task.Run(async () =>
            {
                lock (this) Scoped((scope, shiftScope) =>
                {
                    var repository = scope.ServiceProvider.GetService<IShiftRepository>();
                    repository.CreateTables();
                    shiftScope.UpdateShiftProfiles();
                    shiftScope.UpdateShift(); 
                });

                logger.LogInformation("Initailized");
                startedEvent.Set();

                while (!cancellation.Value.IsCancellationRequested)
                {
                    lock (this) Scoped((scope, shiftScope) =>
                    {
                        shiftScope.UpdateShift();
                    });

                    var interval = configuration.CheckShiftIntervalMinutes;
                    var timeWait = TimeSpan.FromMinutes(interval - DateTime.Now.Minute % interval);
                    await Task.Delay(timeWait, cancellation.Value);
                }
            });
        }
    }
}
