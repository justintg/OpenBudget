using OpenBudget.Model.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Events
{
    [DataContract]
    public abstract class ModelEvent
    {
        [DataMember]
        public Guid EventID { get; protected set; }

        [DataMember]
        public string EntityType { get; protected set; }

        [DataMember]
        public string EntityID { get; protected set; }

        [DataMember]
        public Guid DeviceID { get; protected set; }

        [DataMember]
        public VectorClock EventVector { get; protected set; }

        protected ModelEvent(string entityType, string entityId)
        {
            EventID = Guid.NewGuid();
            EntityType = entityType;
            EntityID = entityId;
        }

        internal void stampEvent(Guid deviceId, VectorClock vector)
        {
            if (deviceId == null)
                throw new ArgumentNullException(nameof(deviceId));

            if (vector == null)
                throw new ArgumentNullException(nameof(vector));

            if (EventVector != null)
                throw new InvalidOperationException("You cannot stamp an event that has already been stamped!");

            DeviceID = deviceId;
            EventVector = vector;
        }

    }
}
