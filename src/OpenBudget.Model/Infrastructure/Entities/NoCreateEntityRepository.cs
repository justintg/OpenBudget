using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Entities
{
    public abstract class NoCreateEntityRepository<TEntity, TSnapshot> : IEntityRepository<TEntity>
        where TEntity : NoCreateEntity where TSnapshot : EntitySnapshot, new()
    {
        protected readonly BudgetModel _budgetModel;
        private readonly ISnapshotStore _snapshotStore;
        private readonly Func<TSnapshot, TEntity> _entitySnapshotConstructor;

        public NoCreateEntityRepository(BudgetModel budgetModel)
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

        private TEntity LoadEntityFromSnapshot(TSnapshot snapshot)
        {
            TEntity entity = _entitySnapshotConstructor(snapshot);
            _budgetModel.AttachToModel(entity);
            entity.LoadSubEntities();
            return entity;
        }

        protected abstract TEntity MaterializeEntity(string entityID);

        protected abstract bool IsValidID(string entityID);

        internal TEntity MaterializeNewEntityCopy(string entityId)
        {
            if (IsValidID(entityId))
            {
                return MaterializeEntity(entityId);
            }

            throw new InvalidBudgetException("You cannot materialize the entity because the entityId is not valid.");
        }

        public IEnumerable<TEntity> GetAllEntities()
        {
            foreach (var snapshot in _snapshotStore.GetAllSnapshots<TSnapshot>())
            {
                yield return LoadEntityFromSnapshot(snapshot);
            }
        }

        public IEnumerable<TEntity> GetEntitiesByParent(string parentType, string parentId)
        {
            throw new NotImplementedException();
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
                if (IsValidID(entityId))
                {
                    TEntity entity = MaterializeEntity(entityId);
                    _budgetModel.AttachToModel(entity);
                    entity.LoadSubEntities();
                    return entity;
                }
                else
                {
                    return null;
                }
            }
        }

        public EntityBase GetEntityBase(string entityId)
        {
            return GetEntity(entityId);
        }

        public IEnumerable<TEntity> GetEntitiesByParent(string parentType, string parentId, EntityLookupRoot lookupRoot)
        {
            throw new NotImplementedException();
        }
    }
}
