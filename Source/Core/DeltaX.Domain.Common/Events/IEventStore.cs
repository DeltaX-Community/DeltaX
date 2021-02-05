using System.Collections.Generic;

namespace DeltaX.Domain.Common.Events
{
    public interface IEventStore
    {
        void Add(INotificationEto eventItem);

        void Remove(INotificationEto eventItem);

        void Clear();

        IEnumerable<INotificationEto> ToArray();
    }

    public interface IEventStore<TEntity> : 
        IEventStore where TEntity : class
    {
    }
}

