using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.BudgetStore
{
    public class MemoryBudgetStore : IBudgetStore
    {
        public IEventStore EventStore { get; private set; }
        public ISnapshotStore SnapshotStore { get; private set; }

        public MemoryBudgetStore()
        {
            EventStore = new MemoryEventStore();
            this.SnapshotStore = new MemorySnapshotStore();
        }

        public MemoryBudgetStore(IEventStore eventStore)
        {
            this.EventStore = eventStore;
            this.SnapshotStore = new MemorySnapshotStore();
        }

        public TExtension TryGetExtension<TExtension>() where TExtension : class
        {
            return null;
        }

        public void Dispose()
        {
            //Nothing to dispose
        }
    }
}
