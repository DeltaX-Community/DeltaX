namespace DeltaX.Modules.Shift.Repositories
{
    using DeltaX.Database;
    using DeltaX.Modules.Shift.Configuration;
    using DeltaX.Modules.Shift.Shared;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using MySqlConnector;
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

        public void Begin()
        {
            transaction = connection.BeginTransaction();
        }

        public void Commit()
        {
            transaction?.Commit(); 
        }

        public void Rollback()
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
    }
}
