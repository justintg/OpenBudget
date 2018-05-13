using OpenBudget.Model.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Event
{
    public class EventVector
    {
        public EventVector(Guid eventId, VectorClock vector)
        {
            this.EventID = eventId;
            this.Vector = vector;
        }

        public Guid EventID { get; private set; }

        public VectorClock Vector { get; private set; }
    }
}
