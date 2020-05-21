using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenBudget.Model.Entities.Repositories
{
    public class TransactionRepository : EntityRepository<Transaction, TransactionSnapshot>
    {
        internal TransactionRepository(BudgetModel budgetModel) : base(budgetModel)
        {
        }

        protected override IDictionary<EntityReference, List<EntitySnapshot>> FindSubEntitySnapshots(List<EntityReference> parents)
        {
            var snapshotStore = _budgetModel.BudgetStore.SnapshotStore;
            var snapshots = snapshotStore.GetChildSnapshots<SubTransactionSnapshot>(parents);
            if (snapshots == null)
            {
                return null;
            }
            return snapshots.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(s => (EntitySnapshot)s).ToList());
        }
    }
}
