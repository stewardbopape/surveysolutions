﻿using System;
using System.Threading.Tasks;
using NHibernate;

namespace WB.Infrastructure.Native.Storage.Postgre
{
    public interface IUnitOfWork : IDisposable
    {
        void AcceptChanges();
        Task AcceptChangesAsync();
        void DiscardChanges();
        ISession Session { get; }
    }
}
