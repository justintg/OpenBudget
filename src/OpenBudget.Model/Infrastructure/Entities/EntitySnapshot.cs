using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Entities
{
    public abstract class EntitySnapshot
    {
        public string EntityID { get; set; }
        public EntityReference Parent { get; set; }
        public bool IsDeleted { get; set; }

        public string LastEventID { get; set; }
        public VectorClock LastEventVector { get; set; }
    }
}
