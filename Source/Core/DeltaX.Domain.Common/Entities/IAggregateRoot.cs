namespace DeltaX.Domain.Common.Entities
{
    using DeltaX.Domain.Common.Events;
    using System.Collections.Generic;

    public interface IAggregateRoot : IEntity
    {
        EventStore DomainEvent { get; }  
    }
}
