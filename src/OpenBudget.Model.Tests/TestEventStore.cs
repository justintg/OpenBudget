using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.Events;
using System.Collections.Generic;

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
