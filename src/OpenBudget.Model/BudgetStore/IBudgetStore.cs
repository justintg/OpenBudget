using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.BudgetStore
{
    public interface IBudgetStore
    {
        IEventStore EventStore { get; }
        ISnapshotStore SnapshotStore { get; }

        TExtension TryGetExtension<TExtension>() where TExtension : class;
    }
}
