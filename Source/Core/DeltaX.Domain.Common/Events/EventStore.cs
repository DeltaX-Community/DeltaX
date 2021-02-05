namespace DeltaX.Domain.Common.Events
{  
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    public class EventStore : IEventStore
    {
        private ICollection<INotificationEto> collection;

        public EventStore()
        {
            collection = new Collection<INotificationEto>();
        }

        public void Add(INotificationEto eventItem)
        {
            lock (collection)
            {
                collection.Add(eventItem);
            }
        }

        public void Clear()
        {
            lock (collection)
            {
                collection.Clear();
            }
        }

        public void Remove(INotificationEto eventItem)
        {
            lock (collection)
            {
                collection.Remove(eventItem);
            }
        }

        public IEnumerable<INotificationEto> ToArray()
        {
            return collection.ToArray();
        }
    }
}
