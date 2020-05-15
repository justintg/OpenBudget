using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure.Messaging;
using OpenBudget.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Entities
{
    internal class NoCreateEntitySnapshotDenormalizer<TEntity, TSnapshot>
        : IHandler<EntityUpdatedEvent>
        where TEntity : NoCreateEntity<TSnapshot>
        where TSnapshot : EntitySnapshot, new()
    {
        protected readonly BudgetModel _budgetModel;
        private readonly NoCreateEntityRepository<TEntity, TSnapshot> _repository;
        protected readonly ISnapshotStore _snapshotStore;

        protected readonly Func<TSnapshot, TEntity> _createEntityFromSnapshot;

        internal NoCreateEntitySnapshotDenormalizer(BudgetModel budgetModel,
            NoCreateEntityRepository<TEntity, TSnapshot> repository)
        {
            _budgetModel = budgetModel ?? throw new ArgumentNullException(nameof(budgetModel));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _snapshotStore = budgetModel?.BudgetStore?.SnapshotStore ?? throw new ArgumentException("Could not find snapshot store of budgetModel", nameof(budgetModel));

            _createEntityFromSnapshot = CreateFromSnapshotConstructor();

            RegisterForMessages();
        }

        protected virtual void RegisterForMessages()
        {
            _budgetModel.InternalMessageBus.RegisterForMessages<EntityUpdatedEvent>(typeof(TEntity).Name, this);
        }

        private Func<TSnapshot, TEntity> CreateFromSnapshotConstructor()
        {
            var entityType = typeof(TEntity);
            var constructor = entityType
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(c => c.GetParameters().Count() == 1 && c.GetParameters().First().ParameterType == typeof(TSnapshot))
                .First();

            var snapshotParam = Expression.Parameter(typeof(TSnapshot), "s");
            var constructExp = Expression.New(constructor, snapshotParam);
            return Expression.Lambda<Func<TSnapshot, TEntity>>(constructExp, snapshotParam).Compile();
        }

        public void Handle(EntityUpdatedEvent message)
        {
            var snapshot = _snapshotStore.GetSnapshot<TSnapshot>(message.EntityID);
            TEntity entity;
            if (snapshot != null)
            {
                entity = _createEntityFromSnapshot(snapshot);
            }
            else
            {
                entity = _repository.MaterializeNewEntityCopy(message.EntityID);
            }

            entity.ReplayEvents(message.Yield());
            snapshot = entity.GetSnapshot();
            _snapshotStore.StoreSnapshot(snapshot);
        }
    }
}
