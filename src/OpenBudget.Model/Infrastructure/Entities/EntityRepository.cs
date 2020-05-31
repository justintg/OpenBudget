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

        protected virtual TEntity LoadEntityFromSnapshot(TSnapshot snapshot, List<EntitySnapshot> subEntities)
        {
            TEntity entity = _entitySnapshotConstructor(snapshot);

            _budgetModel.AttachToModel(entity);
            if (subEntities == null)
            {
                entity.LoadSubEntities();
            }
            else
            {
                entity.LoadSubEntities(subEntities);
            }
            return entity;
        }

        protected virtual IDictionary<EntityReference, List<EntitySnapshot>> FindSubEntitySnapshots(List<EntityReference> parents)
        {
            return null;
        }

        protected IDictionary<EntityReference, List<EntitySnapshot>> GetSubEntitySnapshotLookup(List<TSnapshot> snapshots)
        {
            var snapshotReference = snapshots.Select(s => new EntityReference(typeof(TEntity).Name, s.EntityID)).ToList();
            var subEntitySnapshots = FindSubEntitySnapshots(snapshotReference);
            if (subEntitySnapshots == null)
            {
                return null;
            }
            else
            {
                return subEntitySnapshots;
            }
        }

        public TEntity GetEntity(string entityId)
        {
            var snapshot = _snapshotStore.GetSnapshot<TSnapshot>(entityId);
            if (snapshot != null)
            {
                return LoadEntityFromSnapshot(snapshot, null);
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<TEntity> GetEntitiesByParent(string parentType, string parentId)
        {
            var snapshots = _snapshotStore.GetChildSnapshots<TSnapshot>(parentType, parentId).ToList();
            var subEntitySnapshots = GetSubEntitySnapshotLookup(snapshots);
            foreach (var snapshot in snapshots)
            {
                if (subEntitySnapshots == null)
                {
                    yield return LoadEntityFromSnapshot(snapshot, null);
                }
                else
                {
                    EntityReference snapshotReference = new EntityReference(typeof(TEntity).Name, snapshot.EntityID);
                    List<EntitySnapshot> subEntities = null;
                    if (!subEntitySnapshots.TryGetValue(snapshotReference, out subEntities))
                    {
                        subEntities = new List<EntitySnapshot>();
                    }

                    yield return LoadEntityFromSnapshot(snapshot, subEntities);
                }
            }
        }

        public IEnumerable<TEntity> GetAllEntities()
        {
            var snapshots = _snapshotStore.GetAllSnapshots<TSnapshot>().ToList();
            var subEntitySnapshots = GetSubEntitySnapshotLookup(snapshots);
            foreach (var snapshot in snapshots)
            {
                if (subEntitySnapshots == null)
                {
                    yield return LoadEntityFromSnapshot(snapshot, null);
                }
                else
                {
                    EntityReference snapshotReference = new EntityReference(typeof(TEntity).Name, snapshot.EntityID);
                    List<EntitySnapshot> subEntities = null;
                    if (!subEntitySnapshots.TryGetValue(snapshotReference, out subEntities))
                    {
                        subEntities = new List<EntitySnapshot>();
                    }

                    yield return LoadEntityFromSnapshot(snapshot, subEntities);
                }
            }
        }

        public EntityBase GetEntityBase(string entityId)
        {
            return GetEntity(entityId);
        }

        public IEnumerable<TEntity> GetEntitiesByParent(string parentType, string parentId, EntityLookupRoot lookupRoot)
        {
            List<TEntity> cachedEntities = new List<TEntity>();
            List<string> missingEntityIds = new List<string>();
            var references = _snapshotStore.GetChildSnapshotReferences<TSnapshot>(parentType, parentId).ToList();
            if (references.Count == 0)
            {
                return Enumerable.Empty<TEntity>();
            }

            foreach (var reference in references)
            {
                if (lookupRoot.TryGetValue(reference, out TEntity entity))
                {
                    cachedEntities.Add(entity);
                }
                else
                {
                    missingEntityIds.Add(reference.EntityID);
                }
            }

            List<TEntity> missingEntiies = new List<TEntity>();
            var snapshots = _snapshotStore.GetSnapshots<TSnapshot>(missingEntityIds).ToList();
            var subEntitySnapshots = GetSubEntitySnapshotLookup(snapshots);
            foreach (var snapshot in snapshots)
            {
                if (subEntitySnapshots == null)
                {
                    TEntity entity = LoadEntityFromSnapshot(snapshot, null);
                    entity.LookupRoot = lookupRoot;
                    lookupRoot.Add(entity);
                    missingEntiies.Add(entity);
                }
                else
                {
                    EntityReference snapshotReference = new EntityReference(typeof(TEntity).Name, snapshot.EntityID);
                    List<EntitySnapshot> subEntities = null;
                    if (!subEntitySnapshots.TryGetValue(snapshotReference, out subEntities))
                    {
                        subEntities = new List<EntitySnapshot>();
                    }

                    TEntity entity = LoadEntityFromSnapshot(snapshot, subEntities);
                    entity.LookupRoot = lookupRoot;
                    lookupRoot.Add(entity);
                    missingEntiies.Add(entity);
                }
            }

            return cachedEntities.Concat(missingEntiies);
        }
    }
}
