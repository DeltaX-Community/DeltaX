namespace DeltaX.Domain.Common.Repositories
{
    using DeltaX.Domain.Common.Entities;
    using DeltaX.Domain.Common.Events;
    using System.Collections.Generic;
    using System.Data;


    public interface IUnitOfWork
    { 
        IDbConnection DbConnection { get; }

        IDbTransaction DbTransaction { get; }

        IEnumerable<IAggregateRoot> ChangeTracker { get; }

        void AddChangeTracker(IAggregateRoot entity);

        bool IsInTransaction();

        void BeginTransaction();

        void CommitTransaction();

        void RollbackTransaction();

        void SaveChanges();
    }
}
