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
    internal class EntitySnapshotDenormalizer<TEntity, TSnapshot>
        : IHandler<EntityCreatedEvent>,
        IHandler<EntityUpdatedEvent>,
        IHandler<GroupedFieldChangeEvent>
        where TEntity : EntityBase<TSnapshot>
        where TSnapshot : EntitySnapshot, new()
    {

        protected readonly BudgetModel _budgetModel;
        protected readonly ISnapshotStore _snapshotStore;
        protected readonly Func<EntityCreatedEvent, TEntity> _createEntityFromEvent;
        protected readonly Func<TSnapshot, TEntity> _createEntityFromSnapshot;

        internal EntitySnapshotDenormalizer(BudgetModel budgetModel)
        {
            _budgetModel = budgetModel ?? throw new ArgumentNullException(nameof(budgetModel));
            _snapshotStore = budgetModel?.BudgetStore?.SnapshotStore ?? throw new ArgumentException("Could not find snapshot store of budgetModel", nameof(budgetModel));

            _createEntityFromEvent = CreateFromEventConstructor();
            _createEntityFromSnapshot = CreateFromSnapshotConstructor();

            RegisterForMessages();
        }

        protected virtual void RegisterForMessages()
        {
            _budgetModel.InternalMessageBus.RegisterForMessages<EntityCreatedEvent>(typeof(TEntity).Name, this);
            _budgetModel.InternalMessageBus.RegisterForMessages<EntityUpdatedEvent>(typeof(TEntity).Name, this);
            _budgetModel.InternalMessageBus.RegisterForMessages<GroupedFieldChangeEvent>(typeof(TEntity).Name, this);
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

        private Func<EntityCreatedEvent, TEntity> CreateFromEventConstructor()
        {
            var entityType = typeof(TEntity);
            var constructor = entityType
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(c => c.GetParameters().Count() == 1 && c.GetParameters().First().ParameterType == typeof(EntityCreatedEvent))
                .First();

            var snapshotParam = Expression.Parameter(typeof(EntityCreatedEvent), "s");
            var constructExp = Expression.New(constructor, snapshotParam);
            return Expression.Lambda<Func<EntityCreatedEvent, TEntity>>(constructExp, snapshotParam).Compile();
        }

        public void Handle(EntityCreatedEvent message)
        {
            if (!_budgetModel.IsSavingLocally)
            {
                var snapshot = _createEntityFromEvent(message).GetSnapshot();
                _snapshotStore.StoreSnapshot(snapshot);
            }
        }

        public void Handle(EntityUpdatedEvent message)
        {
            if (!_budgetModel.IsSavingLocally)
            {
                var snapshot = _snapshotStore.GetSnapshot<TSnapshot>(message.EntityID);
                var entity = _createEntityFromSnapshot(snapshot);
                entity.ReplayEvents(message.Yield());
                snapshot = entity.GetSnapshot();
                _snapshotStore.StoreSnapshot(snapshot);
            }
        }

        public void Handle(GroupedFieldChangeEvent message)
        {
            if (!_budgetModel.IsSavingLocally)
            {
                foreach (var evt in message.GroupedEvents)
                {
                    if (evt.EntityType == typeof(TEntity).Name)
                    {
                        if (evt is EntityCreatedEvent entityCreatedEvent)
                        {
                            Handle(entityCreatedEvent);
                        }
                        else if (evt is EntityUpdatedEvent entityUpdatedEvent)
                        {
                            Handle(entityUpdatedEvent);
                        }
                    }
                }
            }
        }
    }
}

