using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.BudgetStore
{
    public class MemoryBudgetStore : IBudgetStore
    {
        public IEventStore EventStore { get; private set; }

        public MemoryBudgetStore()
        {
            EventStore = new MemoryEventStore();
        }

        public MemoryBudgetStore(IEventStore eventStore)
        {
            this.EventStore = eventStore;
        }

        public TExtension TryGetExtension<TExtension>() where TExtension : class
        {
            return null;
        }
    }
}
