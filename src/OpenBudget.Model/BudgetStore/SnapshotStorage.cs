using Newtonsoft.Json;
using OpenBudget.Model.Infrastructure.Entities;
using OpenBudget.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.BudgetStore
{
    internal interface ISnapshotStorage
    {
        void StoreSnapshot(EntitySnapshot snapshot);
    }

    internal interface ISnapshotStorage<TSnapshot> : ISnapshotStorage where TSnapshot : EntitySnapshot
    {
        void StoreSnapshot(TSnapshot snapshot);
        TSnapshot GetSnapshot(string entityId);
        IEnumerable<TSnapshot> GetSnapshots();
        IEnumerable<TSnapshot> GetSnapshotsByParent(string parentType, string parentId);
    }

    internal class ParentKey : IEquatable<ParentKey>
    {
        public string ParentType { get; private set; }
        public string ParentId { get; private set; }

        public ParentKey(string parentType, string parentId)
        {
            ParentType = parentType;
            ParentId = parentId;
        }

        public bool Equals(ParentKey other)
        {
            if (other == null) return false;
            if (ReferenceEquals(other, this)) return true;
            return this.ParentType == other.ParentType && this.ParentId == other.ParentId;
        }

        public override int GetHashCode()
        {
            int hashCode = ParentType.GetHashCode();
            hashCode = (hashCode * 397) ^ (ParentId.GetHashCode());
            return hashCode;
        }
    }

    internal class SnapshotStorage<TSnapshot> : ISnapshotStorage<TSnapshot> where TSnapshot : EntitySnapshot
    {
        private Dictionary<string, TSnapshot> _identityMap = new Dictionary<string, TSnapshot>();
        private Dictionary<ParentKey, List<TSnapshot>> _parentMap = new Dictionary<ParentKey, List<TSnapshot>>();
        private SnapshotSerializer _serializer = new SnapshotSerializer();

        public SnapshotStorage()
        {

        }

        public TSnapshot GetSnapshot(string entityId)
        {
            TSnapshot snapshot = null;
            if (!_identityMap.TryGetValue(entityId, out snapshot)) return null;
            return SerializeRoundTripSnapshot(snapshot);
        }

        public IEnumerable<TSnapshot> GetSnapshots()
        {
            foreach (var snapshot in _identityMap.Values)
            {
                yield return SerializeRoundTripSnapshot(snapshot);
            };
        }

        public IEnumerable<TSnapshot> GetSnapshotsByParent(string parentType, string parentId)
        {
            ParentKey parentKey = new ParentKey(parentType, parentId);

            List<TSnapshot> childSnapshot = null;
            if (_parentMap.TryGetValue(parentKey, out childSnapshot))
            {
                foreach (var snapshot in childSnapshot)
                {
                    yield return SerializeRoundTripSnapshot(snapshot);
                }
            }
        }

        public void StoreSnapshot(TSnapshot snapshot)
        {
            snapshot = SerializeRoundTripSnapshot(snapshot);
            RemoveCurrentSnapshot(snapshot);

            _identityMap.Add(snapshot.EntityID, snapshot);

            if (snapshot.Parent?.EntityID != null)
            {
                ParentKey parentKey = new ParentKey(snapshot.Parent.EntityType, snapshot.Parent.EntityID);
                List<TSnapshot> childList = null;
                if (_parentMap.TryGetValue(parentKey, out childList))
                {
                    childList.Add(snapshot);
                }
                else
                {
                    childList = new List<TSnapshot>();
                    childList.Add(snapshot);
                    _parentMap.Add(parentKey, childList);
                }
            }
        }

        private TSnapshot SerializeRoundTripSnapshot(TSnapshot snapshot)
        {
            string data = _serializer.SerializeToString(snapshot);
            return _serializer.DeserializeFromString<TSnapshot>(data);
        }

        public void StoreSnapshot(EntitySnapshot snapshot)
        {
            TSnapshot typedSnapshot = (TSnapshot)snapshot;
            StoreSnapshot(typedSnapshot);
        }

        private TSnapshot FindCurrentSnapshot(TSnapshot snapshot)
        {
            TSnapshot currentSnapshot = null;
            if (!_identityMap.TryGetValue(snapshot.EntityID, out currentSnapshot)) return null;
            return currentSnapshot;
        }

        private void RemoveCurrentSnapshot(TSnapshot snapshot)
        {
            var currentSnapshot = FindCurrentSnapshot(snapshot);
            if (currentSnapshot == null) return;

            if (currentSnapshot.Parent?.EntityID != null)
            {
                ParentKey parentKey = new ParentKey(currentSnapshot.Parent.EntityType, currentSnapshot.Parent.EntityID);
                List<TSnapshot> childList = null;
                if (_parentMap.TryGetValue(parentKey, out childList))
                {
                    childList.Remove(currentSnapshot);
                }
            }

            _identityMap.Remove(snapshot.EntityID);
        }
    }
}
