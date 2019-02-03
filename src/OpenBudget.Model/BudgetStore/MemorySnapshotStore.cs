using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenBudget.Model.Infrastructure.Entities;

namespace OpenBudget.Model.BudgetStore
{
    public class MemorySnapshotStore : ISnapshotStore
    {
        private Dictionary<string, EntitySnapshot> _snapshots = new Dictionary<string, EntitySnapshot>();
        private Dictionary<string, List<EntitySnapshot>> _snapshotsByParent = new Dictionary<string, List<EntitySnapshot>>();

        public IEnumerable<TSnapshot> GetChildSnapshots<TSnapshot>(string parentId) where TSnapshot : EntitySnapshot
        {
            throw new NotImplementedException();
        }

        public TSnapshot GetSnapshot<TSnapshot>(string entityId) where TSnapshot : EntitySnapshot
        {
            throw new NotImplementedException();
        }

        public void StoreSnapshot<TSnapshot>(TSnapshot snapshot) where TSnapshot : EntitySnapshot
        {
            EntitySnapshot removeSnapshot = null;
            if (_snapshots.TryGetValue(snapshot.EntityID, out removeSnapshot))
            {
                _snapshots[snapshot.EntityID] = snapshot;
            }
            else
            {
                _snapshots.Add(snapshot.EntityID, snapshot);
            }

            AddSnapshotToParent(snapshot, snapshot.Parent.EntityID, removeSnapshot);
        }

        private void AddSnapshotToParent(EntitySnapshot snapshot, string parentId, EntitySnapshot removeSnapshot)
        {
            List<EntitySnapshot> childrenList = null;
            if (_snapshotsByParent.TryGetValue(parentId, out childrenList))
            {
                if (removeSnapshot != null)
                {
                    childrenList.Remove(removeSnapshot);
                }

                childrenList.Add(snapshot);
            }
            else
            {
                childrenList = new List<EntitySnapshot>();
                childrenList.Add(snapshot);
                _snapshotsByParent.Add(parentId, childrenList);
            }
        }

        public void StoreSnapshots(IEnumerable<EntitySnapshot> snapshots)
        {
            foreach (var snapshot in snapshots)
            {
                StoreSnapshot(snapshot);
            }
        }
    }
}
