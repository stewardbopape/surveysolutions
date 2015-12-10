using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sqo;
using Sqo.Transactions;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;

namespace WB.Core.SharedKernels.Enumerator.Implementation.Services
{
    public class SiaqodbPlainStorage<TEntity> : IAsyncPlainStorage<TEntity> where TEntity: class, IPlainStorageEntity
    {
        protected readonly ISiaqodb Storage;

        public SiaqodbPlainStorage(ISiaqodb storage)
        {
            this.Storage = storage;
        }

        public virtual TEntity GetById(string id)
        {
            return this.Query(entities => entities.FirstOrDefault(entity => entity.Id == id));
        }

        public async Task RemoveAsync(string id)
        {
            TEntity entity = await Task.FromResult(this.GetById(id));

            await this.RemoveAsync(new[] { entity });
        }

        public virtual async Task RemoveAsync(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities.Where(entity => entity != null))
            {
                await this.Storage.DeleteAsync(entity);
            }
        }

        public async Task StoreAsync(TEntity entity)
        {
            await this.StoreAsync(new[] { entity });
        }

        public virtual async Task StoreAsync(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities.Where(entity => entity != null))
            {
                await this.Storage.StoreObjectAsync(entity);
            }
        }

        public TResult Query<TResult>(Func<IQueryable<TEntity>, TResult> query)
        {
            var queryable = this.Storage.Cast<TEntity>().AsQueryable();
            var result = query.Invoke(queryable);
            return result;
        }
    }
}