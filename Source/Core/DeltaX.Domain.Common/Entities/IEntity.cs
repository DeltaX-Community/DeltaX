namespace DeltaX.Domain.Common.Entities
{
    public interface IEntity
    {

    }

    public interface IEntity<TKey> : IEntity
    {
        TKey Id { get; }
    }
}
