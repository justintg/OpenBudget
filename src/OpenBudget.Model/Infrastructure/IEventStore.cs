using OpenBudget.Model.Event;
using OpenBudget.Model.EventStream;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Infrastructure
{
    public interface IEventStore
    {
        IEnumerable<ModelEvent> GetEvents();

        IEnumerable<ModelEvent> GetEntityEvents(string entityType, string entityId);

        IEnumerable<EventVector> GetEntityEventVectors(string entityType, string entityId);

        void StoreEvents(IEnumerable<ModelEvent> events);

        void MergeEventStreamClock(IEventStream eventStream);

        VectorClock GetMaxVectorClock();

        VectorClock GetMaxVectorForEntity(string entityType, string entityId);

        void SetMaxVectorClock(VectorClock vectorClock);

        void IgnoreEvents(IEnumerable<Guid> eventIds);

        HashSet<Guid> GetStoredEventIDSet();

        IEnumerable<ModelEvent> GetUnpublishedEvents(HashSet<Guid> publishedEventSet);
    }
}
