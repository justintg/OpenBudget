using OpenBudget.Model.Events;
using OpenBudget.Model.EventStream;
using OpenBudget.Model.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenBudget.Model.BudgetStore
{
    public class MemoryEventStore : IEventStore
    {
        private List<ModelEvent> _events = new List<ModelEvent>();
        private List<ModelEvent> _ignoredevents = new List<ModelEvent>();
        private VectorClock _maxVectorClock;

        public IEnumerable<ModelEvent> GetEntityEvents(string entityType, string entityId)
        {
            return _events.Where(e => e.EntityType == entityType && e.EntityID == entityId).OrderBy(e => e.EventVector);
        }

        public IEnumerable<EventVector> GetEntityEventVectors(string entityType, string entityId)
        {
            return GetEntityEvents(entityType, entityId).Select(e => new EventVector(e.EventID, e.EventVector));
        }

        public IEnumerable<ModelEvent> GetEvents()
        {
            return _events.OrderBy(e => e.EventVector);
        }

        public VectorClock GetMaxVectorClock()
        {
            return _maxVectorClock;
        }

        public VectorClock GetMaxVectorForEntity(string entityType, string entityId)
        {
            return GetEntityEventVectors(entityType, entityId).LastOrDefault()?.Vector;
        }

        public HashSet<Guid> GetStoredEventIDSet()
        {
            return new HashSet<Guid>(_events.Concat(_ignoredevents).Select(e => e.EventID));
        }

        public IEnumerable<ModelEvent> GetUnpublishedEvents(HashSet<Guid> publishedEventSet)
        {
            return _events.Where(e => !publishedEventSet.Contains(e.EventID));
        }

        public void IgnoreEvents(IEnumerable<Guid> eventIds)
        {
            foreach (var eventId in eventIds)
            {
                var evt = _events.SingleOrDefault(e => e.EventID == eventId);
                if (evt == null) continue;

                _events.Remove(evt);
                _ignoredevents.Add(evt);
            }
        }

        public void MergeEventStreamClock(IEventStream eventStream)
        {
            var internalVector = _maxVectorClock;
            if (internalVector == null)
            {
                SetMaxVectorClock(eventStream.Header.EndVectorClock);
            }
            else
            {
                SetMaxVectorClock(internalVector.Merge(eventStream.Header.EndVectorClock));
            }
        }

        public void SetMaxVectorClock(VectorClock vectorClock)
        {
            _maxVectorClock = vectorClock;
        }

        public virtual void StoreEvents(IEnumerable<ModelEvent> events)
        {
            _events.AddRange(events);
        }
    }
}
