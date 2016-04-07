﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.ServiceLocation;
using Ncqrs;
using Ncqrs.Domain.Storage;
using Ncqrs.Eventing;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.Aggregates;
using WB.Core.Infrastructure.EventBus.Lite;

namespace WB.Core.Infrastructure.CommandBus.Implementation
{
    internal class CommandService : ICommandService
    {
        private readonly IEventSourcedAggregateRootRepository eventSourcedRepository;
        private readonly IPlainAggregateRootRepository plainAggregateRootRepository;
        private readonly ILiteEventBus eventBus;
        private readonly IAggregateSnapshotter snapshooter;
        private readonly IServiceLocator serviceLocator;

        private int executingCommandsCount = 0;
        private readonly object executionCountLock = new object();
        private TaskCompletionSource<object> executionAwaiter = null;


        public CommandService(IEventSourcedAggregateRootRepository eventSourcedRepository,
            ILiteEventBus eventBus, 
            IAggregateSnapshotter snapshooter,
            IServiceLocator serviceLocator, IPlainAggregateRootRepository plainAggregateRootRepository)
        {
            this.eventSourcedRepository = eventSourcedRepository;
            this.eventBus = eventBus;
            this.snapshooter = snapshooter;
            this.serviceLocator = serviceLocator;
            this.plainAggregateRootRepository = plainAggregateRootRepository;
        }

        public Task ExecuteAsync(ICommand command, string origin, CancellationToken cancellationToken)
        {
            return Task.Run(() => this.Execute(command, origin, cancellationToken));
        }

        public void Execute(ICommand command, string origin)
        {
            this.ExecuteImpl(command, origin, CancellationToken.None);
        }

        private void Execute(ICommand command, string origin, CancellationToken cancellationToken)
        {
            this.RegisterCommandExecution();

            try
            {
                this.ExecuteImpl(command, origin, cancellationToken);
            }
            finally
            {
                this.UnregisterCommandExecution();
            }
        }

        private void RegisterCommandExecution()
        {
            lock (this.executionCountLock)
            {
                this.executingCommandsCount++;
            }
        }

        private void UnregisterCommandExecution()
        {
            lock (this.executionCountLock)
            {
                this.executingCommandsCount--;

                if (this.executingCommandsCount > 0)
                    return;

                if (this.executionAwaiter != null)
                {
                    this.executionAwaiter.SetResult(new object());
                    this.executionAwaiter = null;
                }
            }
        }

        public Task WaitPendingCommandsAsync()
        {
            lock (this.executionCountLock)
            {
                if (this.executingCommandsCount == 0)
                    return Task.FromResult(null as object);

                if (this.executionAwaiter == null)
                {
                    this.executionAwaiter = new TaskCompletionSource<object>();
                }

                return this.executionAwaiter.Task;
            }
        }

        protected virtual void ExecuteImpl(ICommand command, string origin, CancellationToken cancellationToken)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            cancellationToken.ThrowIfCancellationRequested();

            if (!CommandRegistry.Contains(command))
                throw new CommandServiceException($"Unable to execute command {command.GetType().Name} because it is not registered.");

            Type aggregateType = CommandRegistry.GetAggregateRootType(command);
            Func<ICommand, Guid> aggregateRootIdResolver = CommandRegistry.GetAggregateRootIdResolver(command);
            Action<ICommand, IAggregateRoot> commandHandler = CommandRegistry.GetCommandHandler(command);
            IEnumerable<Action<IAggregateRoot, ICommand>> validators = CommandRegistry.GetValidators(command, this.serviceLocator);
            bool isAggregateEventSourced = CommandRegistry.IsAggregateEventSourced(command);
            bool isAggregatePlain = CommandRegistry.IsAggregatePlain(command);

            Guid aggregateId = aggregateRootIdResolver.Invoke(command);

            if (isAggregateEventSourced)
            {
                this.ExecuteEventSourcedCommand(command, origin, aggregateType, aggregateId, validators, commandHandler, cancellationToken);
            }
            else if (isAggregatePlain)
            {
                this.ExecutePlainCommand(command, aggregateType, aggregateId, validators, commandHandler, cancellationToken);
            }
            else
            {
                throw new CommandServiceException($"Unable to execute command {command.GetType().Name} because it is registered to unknown aggregate root kind.");
            }
        }

        private void ExecuteEventSourcedCommand(ICommand command, string origin,
            Type aggregateType, Guid aggregateId, IEnumerable<Action<IAggregateRoot, ICommand>> validators,
            Action<ICommand, IAggregateRoot> commandHandler, CancellationToken cancellationToken)
        {
            IEventSourcedAggregateRoot aggregate = this.eventSourcedRepository.GetLatest(aggregateType, aggregateId);

            cancellationToken.ThrowIfCancellationRequested();

            if (aggregate == null)
            {
                if (!CommandRegistry.IsInitializer(command))
                    throw new CommandServiceException($"Unable to execute not-constructing command {command.GetType().Name} because aggregate {aggregateId.FormatGuid()} does not exist.");

                aggregate = (IEventSourcedAggregateRoot) this.serviceLocator.GetInstance(aggregateType);
                aggregate.SetId(aggregateId);
            }

            cancellationToken.ThrowIfCancellationRequested();

            foreach (Action<IAggregateRoot, ICommand> validator in validators)
            {
                validator.Invoke(aggregate, command);
            }

            cancellationToken.ThrowIfCancellationRequested();

            commandHandler.Invoke(command, aggregate);

            if (!aggregate.HasUncommittedChanges())
                return;

            CommittedEventStream commitedEvents = this.eventBus.CommitUncommittedEvents(aggregate, origin);
            aggregate.MarkChangesAsCommitted();

            try
            {
                this.eventBus.PublishCommittedEvents(commitedEvents);
            }
            finally
            {
                this.snapshooter.CreateSnapshotIfNeededAndPossible(aggregate);
            }
        }

        private void ExecutePlainCommand(ICommand command,
            Type aggregateType, Guid aggregateId, IEnumerable<Action<IAggregateRoot, ICommand>> validators,
            Action<ICommand, IAggregateRoot> commandHandler, CancellationToken cancellationToken)
        {
            IPlainAggregateRoot aggregate = null;
            if (CommandRegistry.IsInitializer(command))
            {
                aggregate = (IPlainAggregateRoot) this.serviceLocator.GetInstance(aggregateType);
                aggregate.SetId(aggregateId);
            }
            else
            {
                aggregate = this.plainAggregateRootRepository.Get(aggregateType, aggregateId);
                if (aggregate == null)
                    throw new CommandServiceException(
                        $"Unable to execute not-constructing command {command.GetType().Name} because command service does not support plain repositories.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            foreach (Action<IAggregateRoot, ICommand> validator in validators)
            {
                validator.Invoke(aggregate, command);
            }

            cancellationToken.ThrowIfCancellationRequested();

            commandHandler.Invoke(command, aggregate);

            this.plainAggregateRootRepository.Save(aggregate);
        }
    }
}