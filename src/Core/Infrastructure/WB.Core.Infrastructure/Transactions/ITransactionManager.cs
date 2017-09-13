﻿using System.Data;

namespace WB.Core.Infrastructure.Transactions
{
    public interface ITransactionManager
    {
        void BeginCommandTransaction();
        void CommitCommandTransaction();
        void RollbackCommandTransaction();

        void BeginQueryTransaction();
        void RollbackQueryTransaction();
        bool IsQueryTransactionStarted { get; }
        IDbTransaction GetTransaction();
    }
}