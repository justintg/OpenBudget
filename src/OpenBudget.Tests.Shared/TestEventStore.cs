using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.Events;
using OpenBudget.Model.EventStream;
using OpenBudget.Model.Infrastructure;
using System;
using System.Collections.Generic;

namespace OpenBudget.Tests.Shared
{
    public class TestEventStore : EventStoreDecorator
    {
        public List<ModelEvent> TestEvents = new List<ModelEvent>();

        public TestEventStore(IEventStore eventStore) : base(eventStore)
        {

        }

        public override void StoreEvents(IEnumerable<ModelEvent> events)
        {
            base.StoreEvents(events);
            TestEvents.AddRange(events);
        }
    }

    public class EventStoreDecorator : IEventStore
    {
        private readonly IEventStore _eventStore;

        public EventStoreDecorator(IEventStore eventStore)
        {
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        }
        public virtual IEnumerable<ModelEvent> GetEntityEvents(string entityType, string entityId)
        {
            return _eventStore.GetEntityEvents(entityType, entityId);
        }

        public virtual IEnumerable<EventVector> GetEntityEventVectors(string entityType, string entityId)
        {
            return _eventStore.GetEntityEventVectors(entityType, entityId);
        }

        public virtual IEnumerable<ModelEvent> GetEvents()
        {
            return _eventStore.GetEvents();
        }

        public virtual VectorClock GetMaxVectorClock()
        {
            return _eventStore.GetMaxVectorClock();
        }

        public virtual VectorClock GetMaxVectorForEntity(string entityType, string entityId)
        {
            return _eventStore.GetMaxVectorForEntity(entityType, entityId);
        }

        public virtual HashSet<Guid> GetStoredEventIDSet()
        {
            return _eventStore.GetStoredEventIDSet();
        }

        public virtual IEnumerable<ModelEvent> GetUnpublishedEvents(HashSet<Guid> publishedEventSet)
        {
            return _eventStore.GetUnpublishedEvents(publishedEventSet);
        }

        public virtual void IgnoreEvents(IEnumerable<Guid> eventIds)
        {
            _eventStore.IgnoreEvents(eventIds);
        }

        public virtual void MergeEventStreamClock(IEventStream eventStream)
        {
            _eventStore.MergeEventStreamClock(eventStream);
        }

        public virtual void SetMaxVectorClock(VectorClock vectorClock)
        {
            _eventStore.SetMaxVectorClock(vectorClock);
        }

        public virtual void StoreEvents(IEnumerable<ModelEvent> events)
        {
            _eventStore.StoreEvents(events);
        }
    }
}
