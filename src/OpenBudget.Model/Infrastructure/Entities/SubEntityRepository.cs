﻿using OpenBudget.Model.BudgetStore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Entities
{
    internal class SubEntityRepository<TEntity, TSnapshot>
        : ISubEntityRepository<TEntity>
        where TEntity : SubEntity where TSnapshot : EntitySnapshot, new()
    {
        private readonly BudgetModel _budgetModel;
        private readonly ISnapshotStore _snapshotStore;
        private readonly Func<TSnapshot, TEntity> _entitySnapshotConstructor;

        public SubEntityRepository(BudgetModel budgetModel)
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
            return entity;
        }

        public IEnumerable<TEntity> GetEntitiesByParent(string parentType, string parentId)
        {
            var snapshots = _snapshotStore.GetChildSnapshots<TSnapshot>(parentType, parentId);
            foreach (var snapshot in snapshots)
            {
                yield return LoadEntityFromSnapshot(snapshot);
            }
        }

        public IEnumerable<TEntity> CreateEntitiesFromSnapshot(List<EntitySnapshot> snapshots)
        {
            var typedSnapshots = snapshots.Where(s => s is TSnapshot).Select(s => (TSnapshot)s).ToList();
            foreach (var snapshot in typedSnapshots)
            {
                yield return LoadEntityFromSnapshot(snapshot);
            }
        }
    }
}
