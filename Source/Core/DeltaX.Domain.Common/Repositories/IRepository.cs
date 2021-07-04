namespace DeltaX.Domain.Common.Repositories
{
    public interface IRepository 
    {
        IUnitOfWork UnitOfWork { get; } 
    }
}
