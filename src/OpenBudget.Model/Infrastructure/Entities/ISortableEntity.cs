using OpenBudget.Model.BudgetStore;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Entities
{
    internal interface ISortableEntity
    {
        int SortOrder { get; }

        void ForceSetSortOrder(int position);

        IEntityCollection GetParentCollection();

        int GetMaxSnapshotSortOrder(ISnapshotStore snapshotStore);
    }
}
