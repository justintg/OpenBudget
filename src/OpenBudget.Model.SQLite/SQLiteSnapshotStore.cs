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
    internal class SQLiteSnapshotStore : ISnapshotStore, IDisposable
    {
        private readonly string _connectionString;
        private SqliteConnection _connection;
        private bool _isBatching = false;
        private IDbContextTransaction _transaction;
        private SqliteContext _parentBatchContext;
        private SqliteContext _batchContext;
        private int _batchCount = 0;
        private VectorClock _lastVectorClockCache;
        private const int FLUSH_THRESHOLD = 1000;

        public SQLiteSnapshotStore(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();
        }

        private IDisposable GetContext(out SqliteContext context)
        {
            if (_isBatching)
            {
                FlushBatch();
                context = _batchContext;
                return Disposable.CreateEmpty();
            }
            else
            {
                context = new SqliteContext(_connection);
                return context;
            }
        }

        public long GetAcountBalanceLongValue(string accountId)
        {
            using (GetContext(out SqliteContext context))
            {
                var amounts = context.Transactions
                    .AsNoTracking()
                    .Where(t => t.Parent.EntityType == nameof(Account)
                    && t.Parent.EntityID == accountId
                    && !t.IsDeleted)
                    .Sum(t => t.Amount);
                return amounts;
            }
        }

        public IEnumerable<TSnapshot> GetAllSnapshots<TSnapshot>() where TSnapshot : EntitySnapshot
        {
            using (GetContext(out SqliteContext context))
            {
                var snapshotSet = context.GetSnapshotSet<TSnapshot>();
                return snapshotSet.AsNoTracking().ToList();
            }
        }

        public IEnumerable<TChildSnapshot> GetChildSnapshots<TChildSnapshot>(string parentType, string parentId) where TChildSnapshot : EntitySnapshot
        {
            using (GetContext(out SqliteContext context))
            {
                var snapshotSet = context.GetSnapshotSet<TChildSnapshot>();
                return snapshotSet.Where(e => e.Parent.EntityType == parentType && e.Parent.EntityID == parentId).ToList();
            }
        }

        public TSnapshot GetSnapshot<TSnapshot>(string entityId) where TSnapshot : EntitySnapshot
        {
            using (GetContext(out SqliteContext context))
            {
                var snapshotSet = context.GetSnapshotSet<TSnapshot>();
                return snapshotSet.SingleOrDefault(s => s.EntityID == entityId);
            }
        }

        public void StoreSnapshot<TSnapshot>(TSnapshot snapshot) where TSnapshot : EntitySnapshot
        {
            if (_isBatching)
            {
                StoreSnapshotImpl(_batchContext, snapshot);
                IncrementAndFlushBatchIfOverthreshold();
            }
            else
            {
                using (GetContext(out SqliteContext context))
                {
                    StoreSnapshotImpl(context, snapshot);
                    context.SaveChanges();
                }
            }
            SetLastVectorClock(snapshot.LastEventVector);
        }

        private void StoreSnapshotImpl<TSnapshot>(SqliteContext context, TSnapshot snapshot) where TSnapshot : EntitySnapshot
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
        }

        public void StoreSnapshots(IEnumerable<EntitySnapshot> snapshots)
        {
            if (_isBatching)
            {
                foreach (var snapshot in snapshots)
                {
                    EntityTypeLookups.StoreSnapshot(this, snapshot);
                }
            }
            else
            {
                using (GetContext(out SqliteContext context))
                {
                    StoreSnapshotsImpl(context, snapshots);
                    context.SaveChanges();
                }
            }
        }

        private void StoreSnapshotsImpl(SqliteContext context, IEnumerable<EntitySnapshot> snapshots)
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
            using (GetContext(out SqliteContext context))
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
            using (GetContext(out SqliteContext context))
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

            using (GetContext(out SqliteContext context))
            {
                var snapshotSet = context.Set<TChildSnapshot>();
                return snapshotSet.Where(whereExpr).AsEnumerable().GroupBy(s => s.Parent).ToDictionary(g => g.Key, g => g.ToList());
            }
        }

        public IEnumerable<TSnapshot> GetSnapshots<TSnapshot>(IReadOnlyList<string> entityIds) where TSnapshot : EntitySnapshot
        {
            List<string> entityIdsCopy = entityIds.ToList();
            using (GetContext(out SqliteContext context))
            {
                var snapshotSet = context.Set<TSnapshot>();
                return snapshotSet.Where(e => entityIdsCopy.Contains(e.EntityID)).ToList();
            }
        }

        public IEnumerable<EntityReference> GetChildSnapshotReferences<TChildSnapshot>(string parentType, string parentId) where TChildSnapshot : EntitySnapshot
        {
            using (GetContext(out SqliteContext context))
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
            _parentBatchContext = new SqliteContext(_connection);
            _transaction = _parentBatchContext.Database.BeginTransaction();

            _batchContext = new SqliteContext(_connection);
            _batchContext.Database.UseTransaction(_transaction.GetDbTransaction());

            _isBatching = true;
            return Disposable.Create(StopBatching);
        }

        private void IncrementAndFlushBatchIfOverthreshold()
        {
            _batchCount++;
            if (_batchCount >= FLUSH_THRESHOLD)
            {
                FlushBatch();
            }
        }

        private void FlushBatch()
        {
            _batchContext.SaveChanges();
            _batchContext.Dispose();
            _batchContext = new SqliteContext(_connection);
            _batchContext.Database.UseTransaction(_transaction.GetDbTransaction());
        }

        private void StopBatching()
        {
            if (!_isBatching) throw new InvalidOperationException("Cannot stop batching while batching is not started");

            FlushBatch();

            if (_lastVectorClockCache != null)
                SetLastVectorClockImpl(_lastVectorClockCache);

            _batchContext.SaveChanges();
            _batchContext.Dispose();

            _transaction.Commit();
            _transaction.Dispose();

            _parentBatchContext.SaveChanges();
            _parentBatchContext.Dispose();

            _transaction = null;
            _batchContext = null;
            _parentBatchContext = null;
            _lastVectorClockCache = null;
            _isBatching = false;
            _batchCount = 0;

            var cmd = _connection.CreateCommand();
            cmd.CommandText = "PRAGMA wal_checkpoint;";
            cmd.ExecuteNonQuery();
        }

        public void Dispose()
        {
            _connection.Dispose();
            _connection = null;
        }

        public int GetCategoryMaxSortOrder(string masterCategoryId)
        {
            using (GetContext(out SqliteContext context))
            {
                return context.Categories.Where(c => c.Parent.EntityID == nameof(MasterCategory) && c.Parent.EntityID == masterCategoryId).Max(c => c.SortOrder);
            }
        }
    }
}
