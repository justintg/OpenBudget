using Newtonsoft.Json;
using OpenBudget.Model.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace OpenBudget.Model.Events
{
    /// <summary>
    /// The GroupedFieldChangeEvent holds a group of events that must be kept together in order
    /// for the entity's state and it's child entity's state to be valid.
    /// </summary>
    [DataContract]
    public class GroupedFieldChangeEvent : ModelEvent
    {
        public GroupedFieldChangeEvent(string entityType, string entityId, IEnumerable<FieldChangeEvent> groupedEvents)
            : base(entityType, entityId)
        {
            _groupedEvents = groupedEvents.ToList();
        }

        [JsonConstructor]
        private GroupedFieldChangeEvent() : base(null, null)
        {

        }

        [DataMember]
        private List<FieldChangeEvent> _groupedEvents;

        [IgnoreDataMember]
        public IReadOnlyList<FieldChangeEvent> GroupedEvents => _groupedEvents.AsReadOnly();

        internal override void StampEvent(Guid deviceId, VectorClock vector)
        {
            base.StampEvent(deviceId, vector);

            foreach (var groupedEvent in _groupedEvents)
            {
                groupedEvent.StampEvent(deviceId, vector);
            }
        }
    }
}
