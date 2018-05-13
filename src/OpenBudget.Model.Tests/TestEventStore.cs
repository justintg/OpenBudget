using OpenBudget.Model.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenBudget.Model.Event;

namespace OpenBudget.Model.Tests
{
    public class TestEventStore : MemoryEventStore
    {
        public List<ModelEvent> TestEvents = new List<ModelEvent>();

        public override void StoreEvents(IEnumerable<ModelEvent> events)
        {
            base.StoreEvents(events);
            TestEvents.AddRange(events);
        }
    }
}
