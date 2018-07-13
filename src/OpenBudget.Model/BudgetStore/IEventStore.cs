using OpenBudget.Model.Events;
using OpenBudget.Model.EventStream;
using OpenBudget.Model.Infrastructure;
using System;
using System.Collections.Generic;

namespace OpenBudget.Model.BudgetStore
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
