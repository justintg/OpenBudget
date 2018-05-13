using OpenBudget.Model.Event;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Synchronization
{
    public class EntityConflictResolution
    {
        public string EntityType { get; set; }
        public string EntityID { get; set; }
        public List<ConflictResolutionEvent> ConflictResolutionEvents { get; set; }
    }
}
