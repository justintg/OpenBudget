using OpenBudget.Model.BudgetStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Entities
{
    public class EntityRepository<TEntity, TSnapshot> where TEntity : EntityBase where TSnapshot : EntitySnapshot, new()
    {
        private readonly BudgetModel _budgetModel;
        private readonly ISnapshotStore _snapshotStore;
        private readonly Func<TSnapshot, TEntity> _loadEntityFromSnapshot;

        internal EntityRepository(BudgetModel budgetModel)
        {
            _budgetModel = budgetModel ?? throw new ArgumentNullException(nameof(budgetModel));
            _snapshotStore = budgetModel?.BudgetStore?.SnapshotStore ?? throw new ArgumentException("Could not find snapshot store of budgetModel", nameof(budgetModel));

            var entityType = typeof(TEntity);
            var constructor = entityType
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(c => c.GetParameters().Count() == 1 && c.GetParameters().First().ParameterType == typeof(TSnapshot))
                .First();

            var snapshotParam = Expression.Parameter(typeof(TSnapshot), "s");
            var constructExp = Expression.New(constructor, snapshotParam);
            _loadEntityFromSnapshot = Expression.Lambda<Func<TSnapshot, TEntity>>(constructExp, snapshotParam).Compile();
        }

        public TEntity GetEntity(string entityId)
        {
            var snapshot = _snapshotStore.GetSnapshot<TSnapshot>(entityId);
            if (snapshot != null)
            {
                return _loadEntityFromSnapshot(snapshot);
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<TEntity> GetEntitiesByParent<TParent>(string parentId) where TParent : EntityBase
        {
            return null;
        }

        public IEnumerable<TEntity> GetAllEntities()
        {
            foreach (var snapshot in _snapshotStore.GetAllSnapshots<TSnapshot>())
            {
                yield return _loadEntityFromSnapshot(snapshot);
            }
        }


    }
}
