using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage;
using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using OpenBudget.Model.SQLite.Tables;
using OpenBudget.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace OpenBudget.Model.SQLite
{
    internal class SQLiteSnapshotStore : ISnapshotStore
    {
        private readonly string _connectionString;
        private readonly SqliteConnection _connection;
        private bool _isBatching = false;
        private IDbContextTransaction _transaction;
        private SqliteContext _batchContext;
        private VectorClock _lastVectorClockCache;

        public SQLiteSnapshotStore(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();
        }

        private SqliteContext GetContext()
        {
            if (_isBatching)
            {
                var context = new SqliteContext(_connection);
                context.Database.UseTransaction(_transaction.GetDbTransaction());
                return context;
            }
            else
            {
                return new SqliteContext(_connection);
            }
        }

        public decimal GetAccountBalance(string accountId)
        {
            using (var context = GetContext())
            {
                var amounts = context.Transactions.Where(t => t.Parent.EntityType == nameof(Account) && t.Parent.EntityID == accountId && !t.IsDeleted).Select(t => t.Amount).ToList();
                return amounts.Sum();
            }
        }

        public IEnumerable<TSnapshot> GetAllSnapshots<TSnapshot>() where TSnapshot : EntitySnapshot
        {
            using (var context = GetContext())
            {
                var snapshotSet = context.GetSnapshotSet<TSnapshot>();
                return snapshotSet.ToList();
            }
        }

        public IEnumerable<TChildSnapshot> GetChildSnapshots<TChildSnapshot>(string parentType, string parentId) where TChildSnapshot : EntitySnapshot
        {
            using (var context = GetContext())
            {
                var snapshotSet = context.GetSnapshotSet<TChildSnapshot>();
                return snapshotSet.Where(e => e.Parent.EntityType == parentType && e.Parent.EntityID == parentId).ToList();
            }
        }

        public TSnapshot GetSnapshot<TSnapshot>(string entityId) where TSnapshot : EntitySnapshot
        {
            using (var context = GetContext())
            {
                var snapshotSet = context.GetSnapshotSet<TSnapshot>();
                return snapshotSet.SingleOrDefault(s => s.EntityID == entityId);
            }
        }

        public void StoreSnapshot<TSnapshot>(TSnapshot snapshot) where TSnapshot : EntitySnapshot
        {
            using (var context = GetContext())
            {
                var snapshotSet = context.GetSnapshotSet<TSnapshot>();
                if (snapshotSet.Any(e => e.EntityID == snapshot.EntityID))
                {
                    context.Update(snapshot);
                }
                else
                {
                    context.Add(snapshot);
                }
                context.SaveChanges();
            }
            SetLastVectorClock(snapshot.LastEventVector);
        }

        public void StoreSnapshots(IEnumerable<EntitySnapshot> snapshots)
        {
            using (var context = GetContext())
            {
                foreach (var snapshot in snapshots)
                {
                    var dbSnapshot = context.Find(snapshot.GetType(), snapshot.EntityID);
                    if (dbSnapshot != null)
                    {
                        context.Update(snapshot);
                    }
                    else
                    {
                        context.Add(snapshot);
                    }
                }
                context.SaveChanges();
            }
        }

        private void SetLastVectorClock(VectorClock vectorClock)
        {
            if (_isBatching)
            {
                _lastVectorClockCache = vectorClock;
            }
            else
            {
                SetLastVectorClockImpl(vectorClock);
            }
        }

        private void SetLastVectorClockImpl(VectorClock vectorClock)
        {
            using (var context = GetContext())
            {
                var currentClock = context.Info.SingleOrDefault(i => i.Key == "LastSnapshotVector");
                if (currentClock == null)
                {
                    currentClock = new Info()
                    {
                        Key = "LastSnapshotVector",
                        Data = vectorClock.ToByteArray()
                    };
                    context.Add(currentClock);
                }
                else
                {
                    currentClock.Data = vectorClock.ToByteArray();
                    context.Update(currentClock);
                }
                context.SaveChanges();
            }
        }

        public VectorClock GetLastVectorClock()
        {
            using (var context = GetContext())
            {
                var currentClock = context.Info.SingleOrDefault(i => i.Key == "LastSnapshotVector");
                if (currentClock == null)
                {
                    return null;
                }
                else
                {
                    return new VectorClock(currentClock.Data);
                }
            }
        }

        public IDictionary<EntityReference, List<TChildSnapshot>> GetChildSnapshots<TChildSnapshot>(IReadOnlyList<EntityReference> parents) where TChildSnapshot : EntitySnapshot
        {
            var parentTypes = parents.GroupBy(p => p.EntityType).ToDictionary(g => g.Key, g => g.Select(r => r.EntityID).ToList());
            Expression<Func<TChildSnapshot, bool>> whereExpr = null;
            foreach (var parentType in parentTypes)
            {
                var entityIds = parentType.Value;
                Expression<Func<TChildSnapshot, bool>> expr = s => s.Parent.EntityType == parentType.Key && entityIds.Contains(s.Parent.EntityID);
                if (whereExpr == null)
                {
                    whereExpr = expr;
                }
                else
                {
                    var snapshotParam = whereExpr.Parameters.Single();
                    var body = Expression.OrElse(whereExpr.Body, expr.Body);
                    whereExpr = Expression.Lambda<Func<TChildSnapshot, bool>>(body, snapshotParam);
                }
            }

            using (var context = GetContext())
            {
                var snapshotSet = context.Set<TChildSnapshot>();
                return snapshotSet.Where(whereExpr).AsEnumerable().GroupBy(s => s.Parent).ToDictionary(g => g.Key, g => g.ToList());
            }
        }

        public IEnumerable<TSnapshot> GetSnapshots<TSnapshot>(IReadOnlyList<string> entityIds) where TSnapshot : EntitySnapshot
        {
            List<string> entityIdsCopy = entityIds.ToList();
            using (var context = GetContext())
            {
                var snapshotSet = context.Set<TSnapshot>();
                return snapshotSet.Where(e => entityIdsCopy.Contains(e.EntityID)).ToList();
            }
        }

        public IEnumerable<EntityReference> GetChildSnapshotReferences<TChildSnapshot>(string parentType, string parentId) where TChildSnapshot : EntitySnapshot
        {
            using (var context = GetContext())
            {
                var snapshotSet = context.GetSnapshotSet<TChildSnapshot>();
                string childType = EntityTypeLookups.GetEntityType(typeof(TChildSnapshot)).Name;
                return snapshotSet.Where(e => e.Parent.EntityType == parentType && e.Parent.EntityID == parentId)
                    .Select(e => e.EntityID)
                    .ToList()
                    .Select(id => new EntityReference(childType, id));
            }
        }

        public IDisposable StartSnapshotStoreBatch()
        {
            if (_isBatching) throw new InvalidOperationException("Cannot start batching while batching is already started");
            _batchContext = GetContext();
            _transaction = _batchContext.Database.BeginTransaction();
            _isBatching = true;
            return Disposable.Create(StopBatching);
        }

        private void StopBatching()
        {
            if (!_isBatching) throw new InvalidOperationException("Cannot stop batching while batching is not started");

            if (_lastVectorClockCache != null)
                SetLastVectorClockImpl(_lastVectorClockCache);

            _transaction.Commit();
            _transaction.Dispose();
            _batchContext.Dispose();
            _transaction = null;
            _batchContext = null;
            _lastVectorClockCache = null;
            _isBatching = false;

            var cmd = _connection.CreateCommand();
            cmd.CommandText = "PRAGMA wal_checkpoint;";
            cmd.ExecuteNonQuery();
        }
    }
}
