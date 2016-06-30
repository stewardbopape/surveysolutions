using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ncqrs.Eventing;
using Ncqrs.Eventing.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Nito.AsyncEx;
using SQLite.Net;
using SQLite.Net.Interop;
using WB.Core.BoundedContexts.Interviewer.Views;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.SharedKernels.Enumerator;
using SQLite.Net.Async;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.Enumerator.Implementation.Services;

namespace WB.Core.BoundedContexts.Interviewer.Implementation.Storage
{
    public class SqliteEventStorage : IInterviewerEventStorage, IDisposable
    {
        private IEnumeratorSettings enumeratorSettings;
        internal readonly SQLiteConnectionWithLock connection;
        private ILogger logger;

        static readonly Encoding TextEncoding = Encoding.UTF8;

        public SqliteEventStorage(ISQLitePlatform sqLitePlatform,
            ILogger logger,
            ITraceListener traceListener,
            SqliteSettings settings,
            IEnumeratorSettings enumeratorSettings)
        {
            string pathToDatabase = settings.PathToDatabaseDirectory;
            if (pathToDatabase != ":memory:")
            {
                pathToDatabase = Path.Combine(settings.PathToDatabaseDirectory, "events-data.sqlite3");
            }

            this.connection = new SQLiteConnectionWithLock(sqLitePlatform,
                new SQLiteConnectionString(pathToDatabase, true,
                    new BlobSerializerDelegate(
                        (obj) => TextEncoding.GetBytes(JsonConvert.SerializeObject(obj, Formatting.None)),
                        (data, type) => JsonConvert.DeserializeObject(TextEncoding.GetString(data, 0, data.Length), type),
                        (type) => true), 
                    openFlags: SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex))
            {
                //TraceListener = traceListener
            };

            this.logger = logger;
            this.enumeratorSettings = enumeratorSettings;
            this.connection.CreateTable<EventView>();
            this.connection.CreateIndex<EventView>(entity => entity.EventId);
        }

        public IEnumerable<CommittedEvent> Read(Guid id, int minVersion)
            => this.Read(id, minVersion, null, CancellationToken.None);

        public IEnumerable<CommittedEvent> Read(Guid id, int minVersion, IProgress<EventReadingProgress> progress, CancellationToken cancellationToken)
        {
            var startEventSequence = Math.Max(minVersion, 0);
            int totalEventCount;
            int readEventCount = 0;

            using (connection.Lock())
            {
                totalEventCount = this
                    .connection
                    .Table<EventView>()
                    .Count(eventView
                        => eventView.EventSourceId == id
                        && eventView.EventSequence >= startEventSequence);
            }

            if (totalEventCount == 0)
                yield break;

            int nextStartEventSequence = startEventSequence;
            var bulkSize = this.enumeratorSettings.EventChunkSize;
            List<CommittedEvent> bulk;

            progress?.Report(new EventReadingProgress(nextStartEventSequence, totalEventCount));

            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                var startSequenceInTheBulk = nextStartEventSequence;
                var exclusiveEndSequenceInTheBulk = startSequenceInTheBulk + bulkSize;

                using (connection.Lock())
                {
                    bulk = this
                        .connection
                        .Table<EventView>()
                        .Where(eventView
                            => eventView.EventSourceId == id
                            && eventView.EventSequence >= startSequenceInTheBulk
                            && eventView.EventSequence < exclusiveEndSequenceInTheBulk)
                        .OrderBy(x => x.EventSequence)
                        .Select(ToCommitedEvent)
                        .ToList();
                }

                foreach (var committedEvent in bulk)
                {
                    yield return committedEvent;
                    readEventCount++;
                    progress?.Report(new EventReadingProgress(nextStartEventSequence, totalEventCount));
                }

                nextStartEventSequence = exclusiveEndSequenceInTheBulk;

            } while (readEventCount < totalEventCount);
        }

        public CommittedEventStream Store(UncommittedEventStream eventStream)
        {
            using (connection.Lock())
            {
                try
                {
                    this.connection.BeginTransaction();

                    this.ValidateStreamVersion(eventStream);

                    List<EventView> storedEvents = eventStream.Select(ToStoredEvent).ToList();
                    foreach (var @event in storedEvents)
                    {
                        connection.Insert(@event);
                    }

                    this.connection.Commit();
                    return new CommittedEventStream(eventStream.SourceId, eventStream.Select(ToCommitedEvent));
                }
                catch
                {
                    this.connection.Rollback();
                    throw;
                }
            }
        }

        private void ValidateStreamVersion(UncommittedEventStream eventStream)
        {
            var expectedVersion = eventStream.InitialVersion;
            if (expectedVersion == 0)
            {
                bool viewExists;

                using (connection.Lock())
                    viewExists = this.connection.Table<EventView>().Any(x => x.EventSourceId == eventStream.SourceId);

                if (viewExists)
                {
                    var errorMessage = $"Wrong version number. Expected to store new event stream, but it already exists. EventStream Id: {eventStream.SourceId}";
                    this.logger.Error(errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }
            }
            else
            {
                int currentStreamVersion;
                var commandText = $"SELECT MAX({nameof(EventView.EventSequence)}) FROM {nameof(EventView)} WHERE {nameof(EventView.EventSourceId)} = ?";

                using (connection.Lock())
                {
                    var sqLiteCommand = this.connection.CreateCommand(commandText, eventStream.SourceId);
                    currentStreamVersion = sqLiteCommand.ExecuteScalar<int>();
                }

                var expectedExistingSequence = eventStream.Min(x => x.EventSequence) - 1;
                if (expectedExistingSequence != currentStreamVersion)
                {
                    var errorMessage = $"Wrong version number. Expected event stream with version {expectedExistingSequence}, but actual {currentStreamVersion}. SourceId: {eventStream.SourceId}";
                    this.logger.Error(errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }
            }
        }

        public void RemoveEventSourceById(Guid interviewId)
        {
            using (connection.Lock())
            {
                try
                {
                    this.connection.BeginTransaction();
                    var commandText = $"DELETE FROM {nameof(EventView)} WHERE {nameof(EventView.EventSourceId)} = ?";
                    var sqLiteCommand = this.connection.CreateCommand(commandText, interviewId);
                    sqLiteCommand.ExecuteNonQuery();
                    this.connection.Commit();
                }
                catch
                {
                    this.connection.Rollback();
                    throw;
                }
            }
        }

        private static CommittedEvent ToCommitedEvent(EventView storedEvent)
            => new CommittedEvent(
                commitId: storedEvent.CommitId ?? storedEvent.EventSourceId,
                origin: string.Empty,
                eventIdentifier: storedEvent.EventId,
                eventSourceId: storedEvent.EventSourceId,
                eventSequence: storedEvent.EventSequence,
                eventTimeStamp: storedEvent.DateTimeUtc,
                globalSequence: -1,
                payload: JsonConvert.DeserializeObject<Infrastructure.EventBus.IEvent>(storedEvent.JsonEvent, JsonSerializerSettings()));

        private static CommittedEvent ToCommitedEvent(UncommittedEvent storedEvent)
            => new CommittedEvent(
                commitId: storedEvent.EventSourceId,
                origin: string.Empty,
                eventIdentifier: storedEvent.EventIdentifier,
                eventSourceId: storedEvent.EventSourceId,
                eventSequence: storedEvent.EventSequence,
                eventTimeStamp: storedEvent.EventTimeStamp,
                globalSequence: -1,
                payload: storedEvent.Payload);

        private static EventView ToStoredEvent(UncommittedEvent evt)
            => new EventView
            {
                EventId = evt.EventIdentifier,
                EventSourceId = evt.EventSourceId,
                CommitId = evt.CommitId,
                EventSequence = evt.EventSequence,
                DateTimeUtc = evt.EventTimeStamp,
                JsonEvent = JsonConvert.SerializeObject(evt.Payload, JsonSerializerSettings()),
                EventType = evt.Payload.GetType().Name
            };

        public void Dispose()
        {
            this.connection.Dispose();
        }

        internal static Func<JsonSerializerSettings> JsonSerializerSettings = () => new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            NullValueHandling = NullValueHandling.Ignore,
            FloatParseHandling = FloatParseHandling.Decimal,
            Binder = new CapiAndMainCoreToInterviewerAndSharedKernelsBinder()
        };

        [Obsolete("Resolves old namespaces. Cuold be dropped after incompatibility shift with the next version.")]
        internal class CapiAndMainCoreToInterviewerAndSharedKernelsBinder : DefaultSerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                var oldCapiAssemblyName = "WB.UI.Capi";
                var newCapiAssemblyName = "WB.Core.BoundedContexts.Interviewer";
                var newQuestionsAssemblyName = "WB.Core.SharedKernels.Questionnaire";
                var oldMainCoreAssemblyName = "Main.Core";

                if (String.Equals(assemblyName, oldCapiAssemblyName, StringComparison.Ordinal))
                {
                    assemblyName = newCapiAssemblyName;
                }
                else if (String.Equals(assemblyName, oldMainCoreAssemblyName, StringComparison.Ordinal))
                {
                    if (oldMainCoreTypeMap.ContainsKey(typeName))
                        assemblyName = oldMainCoreTypeMap[typeName];
                    else
                        assemblyName = newQuestionsAssemblyName;
                }

                return base.BindToType(assemblyName, typeName);
            }

            private readonly Dictionary<string, string> oldMainCoreTypeMap = new Dictionary<string, string>()
            {
                {"Main.Core.Events.AggregateRootEvent", "WB.Core.Infrastructure"},
                {"Main.Core.Events.User.NewUserCreated", "WB.Core.SharedKernels.DataCollection"},
                {"Main.Core.Events.User.UserChanged", "WB.Core.SharedKernels.DataCollection"},
                {"Main.Core.Events.User.UserLocked", "WB.Core.SharedKernels.DataCollection"},
                {"Main.Core.Events.User.UserLockedBySupervisor", "WB.Core.SharedKernels.DataCollection"},
                {"Main.Core.Events.User.UserUnlocked", "WB.Core.SharedKernels.DataCollection"},
                {"Main.Core.Events.User.UserUnlockedBySupervisor", "WB.Core.SharedKernels.DataCollection"},
            };
        }
    }
}
