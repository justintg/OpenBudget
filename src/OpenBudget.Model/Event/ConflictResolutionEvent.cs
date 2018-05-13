using OpenBudget.Model.Infrastructure.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace OpenBudget.Model.Event
{
    [DataContract]
    public class ConflictResolutionEvent : ModelEvent
    {
        internal ConflictResolutionEvent(
            string entityType,
            string entityId,
            IEnumerable<Guid> eventsToIgnore)
            : base(entityType, entityId)
        {
            _eventsToIgnore = eventsToIgnore.ToList();
        }

        [JsonConstructor]
        private ConflictResolutionEvent() : base(null, null)
        {
        }

        [DataMember]
        private List<Guid> _eventsToIgnore;

        [IgnoreDataMember]
        public IReadOnlyList<Guid> EventsToIgnore => _eventsToIgnore.AsReadOnly();
    }
}
