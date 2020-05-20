using OpenBudget.Model.Entities;
using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure.Messaging;
using OpenBudget.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OpenBudget.Model.Infrastructure.Entities
{
    internal class EntityDenormalizer<T> : IEntityDenormalizer, IHandler<EntityUpdatedEvent>, IHandler<GroupedFieldChangeEvent> where T : EntityBase
    {
        protected IMessenger<ModelEvent> _messenger;
        protected BudgetModel _model;
        protected Dictionary<string, List<WeakReference<T>>> _registrations = new Dictionary<string, List<WeakReference<T>>>();

        public EntityDenormalizer(BudgetModel model)
        {
            _model = model;
            IMessenger<ModelEvent> messenger = model.InternalMessageBus;
            _messenger = messenger;
            RegisterForMessages();
        }

        internal bool ContainsEntity(T entity)
        {
            foreach (var registeredEntity in EnumerateRegistrations(entity.EntityID))
            {
                if (registeredEntity == entity) return true;
            }

            return false;
        }

        void IEntityDenormalizer.RegisterForChanges(EntityBase entity)
        {
            this.RegisterForChanges(entity);
        }

        internal void RegisterForChanges(EntityBase entity)
        {
            if (entity is T entityTyped)
            {
                RegisterForChanges(entityTyped);
            }
            else
            {
                throw new InvalidOperationException("Entity is not the right type for this Denormalizer!");
            }
        }

        internal void RegisterForChanges(T entity)
        {
            List<WeakReference<T>> entityReferences = null;
            if (!_registrations.TryGetValue(entity.EntityID, out entityReferences))
            {
                entityReferences = new List<WeakReference<T>>();
                _registrations.Add(entity.EntityID, entityReferences);
            }

            entityReferences.Add(new WeakReference<T>(entity));
        }

        protected IEnumerable<T> EnumerateRegistrations(string entityId)
        {
            List<WeakReference<T>> entityRegistrations = null;
            if (_registrations.TryGetValue(entityId, out entityRegistrations))
            {
                foreach (var registration in entityRegistrations.ToList())
                {
                    T entity = null;
                    if (registration.TryGetTarget(out entity))
                    {
                        yield return entity;
                    }
                    else
                    {
                        entityRegistrations.Remove(registration);
                    }
                }
            }
        }

        protected virtual void RegisterForMessages()
        {
            _messenger.RegisterForMessages<EntityUpdatedEvent>(typeof(T).Name, this);
            _messenger.RegisterForMessages<GroupedFieldChangeEvent>(typeof(T).Name, this);
        }

        public virtual void Handle(EntityUpdatedEvent message)
        {
            foreach (var entity in EnumerateRegistrations(message.EntityID))
            {
                entity.ReplayEvents(message.Yield());
            }
        }

        public void Handle(GroupedFieldChangeEvent message)
        {
            List<FieldChangeEvent> eventsToBroadcast = message.GroupedEvents.ToList();

            foreach (var evt in eventsToBroadcast)
            {
                if (evt.EntityType == typeof(T).Name)
                {
                    if (evt is EntityUpdatedEvent entityUpdatedEvent)
                    {
                        Handle(entityUpdatedEvent);
                    }
                }
                else
                {
                    foreach (var entity in EnumerateRegistrations(message.EntityID))
                    {
                        entity.HandleSubEntityEvent(evt);
                    }
                }
            }
        }
    }
}
