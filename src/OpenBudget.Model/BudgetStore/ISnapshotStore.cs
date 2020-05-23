using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.BudgetStore
{
    public interface ISnapshotStore
    {
        TSnapshot GetSnapshot<TSnapshot>(string entityId) where TSnapshot : EntitySnapshot;

        IEnumerable<TSnapshot> GetSnapshots<TSnapshot>(IReadOnlyList<string> entityIds)
            where TSnapshot : EntitySnapshot;

        IEnumerable<TChildSnapshot> GetChildSnapshots<TChildSnapshot>(string parentType, string parentId)
            where TChildSnapshot : EntitySnapshot;

        IEnumerable<EntityReference> GetChildSnapshotReferences<TChildSnapshot>(string parentType, string parentId)
            where TChildSnapshot : EntitySnapshot;

        IDictionary<EntityReference, List<TChildSnapshot>> GetChildSnapshots<TChildSnapshot>(IReadOnlyList<EntityReference> parents)
            where TChildSnapshot : EntitySnapshot;

        IEnumerable<TSnapshot> GetAllSnapshots<TSnapshot>() where TSnapshot : EntitySnapshot;
        void StoreSnapshot<TSnapshot>(TSnapshot snapshot) where TSnapshot : EntitySnapshot;
        //void StoreSnapshots(IEnumerable<EntitySnapshot> snapshots);

        decimal GetAccountBalance(string accountId);

        VectorClock GetLastVectorClock();
        IDisposable StartSnapshotStoreBatch();
    }
}
