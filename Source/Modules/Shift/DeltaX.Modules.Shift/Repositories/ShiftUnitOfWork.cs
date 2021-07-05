namespace DeltaX.Modules.Shift.Repositories
{ 
    using DeltaX.Modules.Shift.Configuration;
    using DeltaX.Modules.Shift.Shared;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options; 
    using System;
    using System.Data;


    class ShiftUnitOfWork : IShiftUnitOfWork
    {
        IDbConnection connection = null;
        IDbTransaction transaction = null;
        Guid id = Guid.Empty; 

        public ShiftUnitOfWork(
            IOptions<ShiftConfiguration> options,
            IDbConnection connection,
            ILogger<ShiftUnitOfWork> logger)
        {
            this.id = Guid.NewGuid();
            this.connection = connection;
        } 

        public IDbConnection Connection
        {
            get { return connection; }
        }

        public IDbTransaction Transaction
        {
            get { return transaction; }
        }

        public Guid Id
        {
            get { return id; }
        }

        public void BeginTransaction()
        {
            transaction = connection.BeginTransaction();
        }

        public void CommitTransaction()
        {
            transaction?.Commit(); 
        }

        public void RollbackTransaction()
        {
            transaction?.Rollback(); 
        }

        public void Dispose()
        {
            transaction?.Dispose();
            transaction = null;

            connection?.Dispose();
            connection = null;
        }

        public bool IsInTransaction()
        {
            return transaction != null;
        }
    }
}
