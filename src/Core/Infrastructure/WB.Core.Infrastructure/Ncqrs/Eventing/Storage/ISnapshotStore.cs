﻿using System;
using Ncqrs.Eventing.Sourcing;
using Ncqrs.Eventing.Sourcing.Snapshotting;

namespace Ncqrs.Eventing.Storage
{

    /// <summary>
    /// A <see cref="Snapshot"/> store. Can store and retrieve a <see cref="Snapshot"/>.
    /// </summary>
    public interface ISnapshotStore
    {
        /// <summary>
        /// Persists a <see cref="Snapshot"/> of an <see cref="EventSource"/>.
        /// </summary>
        /// <param name="snapshot">The <see cref="Snapshot"/> that is being saved.</param>
        void SaveShapshot(Snapshot snapshot);

        /// <summary>
        /// Gets a snapshot of a particular event source, if one exists. Otherwise, returns <c>null</c>.
        /// </summary>
        /// <param name="eventSourceId">Indicates the event source to retrieve the snapshot for.</param>
        /// <param name="maxVersion">Indicates the maximum allowed version to be returned.</param>
        /// <returns>
        /// Returns the most recent <see cref="Snapshot"/> that exists in the store. If the store has a 
        /// snapshot that is more recent than the <paramref name="maxVersion"/>, then <c>null</c> will be returned.
        /// </returns>
        Snapshot GetSnapshot(Guid eventSourceId, int maxVersion);
    }
}
