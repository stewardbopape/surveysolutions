﻿using WB.Core.GenericSubdomains.Portable.Services;

namespace WB.Core.Infrastructure.PlainStorage
{
    public interface IEntitySerializer<TEntity> : IAtomicSerializer<TEntity> where TEntity: class
    {
    }
}
