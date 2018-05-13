using OpenBudget.Model.Infrastructure;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace OpenBudget.Model.EventStream
{
    [DataContract]
    public class EventStreamHeader
    {
        [DataMember]
        public VectorClock EndVectorClock { get; private set; }

        [DataMember]
        public VectorClock StartVectorClock { get; private set; }

        [DataMember]
        public Guid DeviceID { get; private set; }

        [DataMember]
        public Guid EventStreamID { get; private set; }

        public EventStreamHeader(VectorClock start, VectorClock end, Guid deviceId, Guid streamId)
        {
            this.StartVectorClock = start;
            this.EndVectorClock = end;
            this.DeviceID = deviceId;
            this.EventStreamID = streamId;
        }
    }
}
