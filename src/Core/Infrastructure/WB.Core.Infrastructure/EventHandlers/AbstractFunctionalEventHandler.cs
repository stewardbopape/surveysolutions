using System;
using System.Collections.Generic;
using Ncqrs.Eventing.ServiceModel.Bus;
using WB.Core.Infrastructure.EventBus;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernels.SurveySolutions;

namespace WB.Core.Infrastructure.EventHandlers
{
    public abstract class AbstractFunctionalEventHandler<TEntity, TStorage> 
        : FunctionalEventHandlerBase<TEntity>, IFunctionalEventHandler, IEventHandler
        where TEntity : class, IReadSideRepositoryEntity
        where TStorage : class, IReadSideStorage<TEntity>
    {
        private readonly TStorage readSideStorage;

        protected AbstractFunctionalEventHandler(TStorage readSideStorage)
        {
            this.readSideStorage = readSideStorage;
        }
        
        public string Name => this.GetType().Name;

        protected void Handle(IPublishableEvent evt)
        {
            this.Handle(evt, this.readSideStorage);
        }

        public void Handle(IEnumerable<IPublishableEvent> publishableEvents)
        {
            foreach (var publishableEvent in publishableEvents)
            {
                this.Handle(publishableEvent, readSideStorage);
            }
        }

        private void Handle(IPublishableEvent evt, IReadSideStorage<TEntity> storage)
        {
            if (!this.Handles(evt))
                return;
            
            TEntity currentState = GetViewById(evt.EventSourceId, storage);

            var newState = ApplyEventOnEntity(evt, currentState);

            if (newState != null)
            {
                SaveView(evt.EventSourceId, newState, storage);
            }
            else
            {
                RemoveView(evt.EventSourceId, storage);
            }
        }

        private static void RemoveView(Guid id, IReadSideStorage<TEntity> storage)
        {
            storage.Remove(id);
        }

        private static void SaveView(Guid id, TEntity newState, IReadSideStorage<TEntity> storage)
        {
            storage.Store(newState, id);
        }

        protected virtual TEntity GetViewById(Guid id, IReadSideStorage<TEntity> storage)
        {
            return storage.GetById(id);
        }
    }
}
