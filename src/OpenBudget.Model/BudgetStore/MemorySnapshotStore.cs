using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;

namespace OpenBudget.Model.BudgetStore
{
    public class MemorySnapshotStore : ISnapshotStore
    {
        private Dictionary<Type, object> _snapshotStorage = new Dictionary<Type, object>();
        private VectorClock _lastVectorClock = null;

        public IEnumerable<TSnapshot> GetAllSnapshots<TSnapshot>() where TSnapshot : EntitySnapshot
        {
            var storage = GetOrCreateSnapshotStorage<TSnapshot>();
            return storage.GetSnapshots();
        }

        public IEnumerable<TChildSnapshot> GetChildSnapshots<TChildSnapshot>(string parentType, string parentId)
            where TChildSnapshot : EntitySnapshot
        {
            var storage = GetOrCreateSnapshotStorage<TChildSnapshot>();
            return storage.GetSnapshotsByParent(parentType, parentId);
        }

        public TSnapshot GetSnapshot<TSnapshot>(string entityId) where TSnapshot : EntitySnapshot
        {
            var storage = GetOrCreateSnapshotStorage<TSnapshot>();
            return storage.GetSnapshot(entityId);
        }

        public void StoreSnapshot<TSnapshot>(TSnapshot snapshot) where TSnapshot : EntitySnapshot
        {
            var storage = GetOrCreateSnapshotStorage<TSnapshot>();
            storage.StoreSnapshot(snapshot);

            if (snapshot.LastEventVector == null)
                throw new ArgumentException("Null LastEventVector on snapshot.", nameof(snapshot));
            _lastVectorClock = snapshot.LastEventVector;
        }

        public void StoreSnapshots(IEnumerable<EntitySnapshot> snapshots)
        {
            foreach (var snapshot in snapshots)
            {
                var storage = GetOrCreateSnapshotStorage(snapshot.GetType());
                storage.StoreSnapshot(snapshot);
            }
        }

        private ISnapshotStorage GetOrCreateSnapshotStorage(Type snapshotType)
        {
            object snapshotStorage = null;
            if (_snapshotStorage.TryGetValue(snapshotType, out snapshotStorage))
            {
                return (ISnapshotStorage)snapshotStorage;
            }
            else
            {
                snapshotStorage = CreateSnapshotStorage(snapshotType);
                _snapshotStorage.Add(snapshotType, snapshotStorage);
                return (ISnapshotStorage)snapshotStorage;
            }
        }

        private object CreateSnapshotStorage(Type snapshotType)
        {
            return Activator.CreateInstance(typeof(SnapshotStorage<>).MakeGenericType(snapshotType));
        }

        private ISnapshotStorage<TSnapshot> GetOrCreateSnapshotStorage<TSnapshot>() where TSnapshot : EntitySnapshot
        {
            return (ISnapshotStorage<TSnapshot>)GetOrCreateSnapshotStorage(typeof(TSnapshot));
        }

        public decimal GetAccountBalance(string accountId)
        {
            var storage = GetOrCreateSnapshotStorage<TransactionSnapshot>();
            var transactionSnapshots = storage.GetSnapshotsByParent(nameof(Account), accountId);
            return transactionSnapshots.Where(t => !t.IsDeleted).Sum(ts => ts.Amount);
        }

        public VectorClock GetLastVectorClock()
        {
            return _lastVectorClock;
        }

        public IDictionary<EntityReference, List<TChildSnapshot>> GetChildSnapshots<TChildSnapshot>(IReadOnlyList<EntityReference> parents) where TChildSnapshot : EntitySnapshot
        {
            var storage = GetOrCreateSnapshotStorage<TChildSnapshot>();
            return storage.GetSnapshotsByParents(parents);
        }
    }
}
