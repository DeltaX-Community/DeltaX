namespace DeltaX.Domain.Common.Entities
{
    using DeltaX.Domain.Common.Events;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    public abstract class AggregateRoot<TKey> : Entity<TKey>, IAggregateRoot
    {
        public EventStore DomainEvent { get; } = new EventStore();
    }
}