using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Synchronization
{
    public class DeviceEventContainer
    {
        public string EntityType { get; set; }
        public string EntityID { get; set; }
        public Guid DeviceID { get; set; }
        public List<ConflictResolutionEvent> ConflictResolutionEvents { get; set; }
        public List<ModelEvent> PreviouslyIgnoredEvents { get; set; }
        public List<ModelEvent> DeviceEvents { get; set; }
        public VectorClock MaxVectorClock { get; set; }
        public VectorClock MaxVectorClockIncludingResolutions { get; set; }
    }
}
