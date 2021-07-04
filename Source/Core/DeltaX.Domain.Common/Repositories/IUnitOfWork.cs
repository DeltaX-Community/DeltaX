namespace DeltaX.Domain.Common.Repositories
{
    using DeltaX.Domain.Common.Entities;
    using System;
    using System.Collections.Generic;
    using System.Data;

    public interface IUnitOfWork : IDisposable
    {
        Guid Id { get; }
        IDbConnection Connection { get; }
        IDbTransaction Transaction { get; }
        bool IsInTransaction();
        void BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();
    }

    public interface IUnitOfWorkTracker : IUnitOfWork
    {  

        IEnumerable<IAggregateRoot> ChangeTracker { get; }

        void AddChangeTracker(IAggregateRoot entity);
    }
}
