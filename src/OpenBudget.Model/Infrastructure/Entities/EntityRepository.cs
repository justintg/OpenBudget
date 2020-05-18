using OpenBudget.Model.BudgetStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Entities
{
    public class EntityRepository<TEntity, TSnapshot> : IEntityRepository<TEntity>
        where TEntity : EntityBase where TSnapshot : EntitySnapshot, new()
    {
        protected readonly BudgetModel _budgetModel;
        protected readonly ISnapshotStore _snapshotStore;
        protected readonly Func<TSnapshot, TEntity> _entitySnapshotConstructor;

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
            _entitySnapshotConstructor = Expression.Lambda<Func<TSnapshot, TEntity>>(constructExp, snapshotParam).Compile();
        }

        protected virtual TEntity LoadEntityFromSnapshot(TSnapshot snapshot)
        {
            TEntity entity = _entitySnapshotConstructor(snapshot);
            _budgetModel.AttachToModel(entity);
            entity.LoadSubEntities();
            return entity;
        }

        public TEntity GetEntity(string entityId)
        {
            var snapshot = _snapshotStore.GetSnapshot<TSnapshot>(entityId);
            if (snapshot != null)
            {
                return LoadEntityFromSnapshot(snapshot);
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<TEntity> GetEntitiesByParent(string parentType, string parentId)
        {
            var snapshots = _snapshotStore.GetChildSnapshots<TSnapshot>(parentType, parentId);
            foreach (var snapshot in snapshots)
            {
                yield return LoadEntityFromSnapshot(snapshot);
            }
        }

        public IEnumerable<TEntity> GetAllEntities()
        {
            foreach (var snapshot in _snapshotStore.GetAllSnapshots<TSnapshot>())
            {
                yield return LoadEntityFromSnapshot(snapshot);
            }
        }

        public EntityBase GetEntityBase(string entityId)
        {
            return GetEntity(entityId);
        }
    }
}
