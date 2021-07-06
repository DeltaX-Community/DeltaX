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
            DateTime? date=null)
        {
            date ??= DateTime.Now;

            if (!startedEvent.WaitOne(TimeSpan.FromMinutes(1)))
            {
                logger.LogWarning("Not Initialized shift service daemon");
                return null;
            }

            if (GetShiftProfile(profileName, date, date) == null)
            {
                logger.LogWarning("Not profile available with Name:{0} and Date:{1}", profileName, date);
                return null;
            }

            ShiftCrewDto result = null;
            lock (this) Scoped((_, shiftScope) =>
            {
                result = shiftScope.GetShiftCrew(profileName, date);
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

            var uow = scope.ServiceProvider.GetService<IShiftUnitOfWork>();
            var shiftScope = scope.ServiceProvider.GetService<ShiftServiceScoped>();
            shiftScope.PublishShiftCrew += ShiftScopedPublishShiftCrew;
            try
            {
                uow.BeginTransaction();
                action.Invoke(scope, shiftScope);
                uow.CommitTransaction();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Rollback transaction");
                uow.RollbackTransaction();
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
            logger.LogInformation("PublishShiftCrew {@shiftCrew}", shiftCrew);
            var profile = GetShiftProfile(shiftCrew.NameShiftProfile);
            if (profile != null && connector != null && !string.IsNullOrWhiteSpace(profile.TagPublish))
            {
                connector.SetJson(profile.TagPublish, shiftCrew);
            }

            PublishShiftCrew?.Invoke(this, shiftCrew);
            notification?.OnUpdateShiftCrew(shiftCrew);
        }

        private TimeSpan GetIntervalNextTick()
        {
            var next = DateTime.Now.AddMinutes(configuration.CheckShiftIntervalMinutes - DateTime.Now.Minute % configuration.CheckShiftIntervalMinutes);
            return new DateTime(next.Year, next.Month, next.Day, next.Hour, next.Minute, 0, 0) - DateTime.Now; 
        }

        public Task RunAsync(CancellationToken? cancellation)
        {  
            cancellation ??= CancellationToken.None;
            return Task.Run(async () =>
            {
                lock (this) Scoped((scope, shiftScope) =>
                {
                    var repository = scope.ServiceProvider.GetService<IShiftRepository>();
                    repository.CreateTables();
                    shiftScope.UpdateShiftProfiles(DateTime.Now);
                });

                logger.LogInformation("Initailized");
                startedEvent.Set();

                while (!cancellation.Value.IsCancellationRequested)
                {
                    lock (this) Scoped((scope, shiftScope) =>
                    {
                        var now = DateTime.Now;
                        shiftScope.UpdateShiftProfiles(now);
                        shiftScope.UpdateShift(now);
                    });
                     
                    await Task.Delay(GetIntervalNextTick(), cancellation.Value);
                }
            });
        }
    }
}
