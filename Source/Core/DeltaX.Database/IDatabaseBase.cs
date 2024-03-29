﻿namespace DeltaX.Database
{
    using System;
    using System.Data;
    using System.Threading.Tasks;

    public interface IDatabaseBase
    {
        IDbConnection DbConnection { get; }
        bool IsConnected { get; }

        event EventHandler<string> OnConnect;
        event EventHandler<string> OnDisconnect;

        void Close();

        IDbConnection GetConnection(); 

        TResult Run<TResult>(Func<IDbConnection, TResult> method);

        Task<TResult> RunAsync<TResult>(Func<IDbConnection, Task<TResult>> method);

        TResult RunSync<TResult>(Func<IDbConnection, TResult> method);

        TResult RunTransaction<TResult>(Func<IDbConnection, IDbTransaction, TResult> method);

        Task<TResult> RunTransactionAsync<TResult>(Func<IDbConnection, IDbTransaction, Task<TResult>> method);

        bool TryReconnect();
    }
}