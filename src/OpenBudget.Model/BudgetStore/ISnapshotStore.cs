using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.BudgetStore
{
    public interface ISnapshotStore
    {
        TSnapshot GetSnapshot<TSnapshot>(string entityId) where TSnapshot : EntitySnapshot;
        IEnumerable<TSnapshot> GetChildSnapshots<TSnapshot>(string parentId) where TSnapshot : EntitySnapshot;
        void StoreSnapshot<TSnapshot>(TSnapshot snapshot) where TSnapshot : EntitySnapshot;
        void StoreSnapshots(IEnumerable<EntitySnapshot> snapshots);
    }
}
