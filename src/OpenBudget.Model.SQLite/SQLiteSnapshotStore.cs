using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using OpenBudget.Model.SQLite.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Text;

namespace OpenBudget.Model.SQLite
{
    internal class SQLiteSnapshotStore : ISnapshotStore
    {
        private readonly string _connectionString;

        public SQLiteSnapshotStore(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        private SqliteContext GetContext()
        {
            return new SqliteContext(_connectionString);
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
    }
}
