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
    internal class EntityGenerator<T> : IIdentityMap, IHandler<EntityCreatedEvent>, IHandler<EntityUpdatedEvent>, IHandler<GroupedFieldChangeEvent> where T : EntityBase
    {
        protected Dictionary<string, T> _identityMap;
        protected IMessenger<ModelEvent> _messenger;
        protected BudgetModel _model;

        public EntityGenerator(BudgetModel model)
        {
            _model = model;
            IMessenger<ModelEvent> messenger = model.InternalMessageBus;
            _identityMap = new Dictionary<string, T>();
            _messenger = messenger;
            RegisterForMessages();
        }

        protected virtual void RegisterForMessages()
        {
            _messenger.RegisterForMessages<EntityCreatedEvent>(typeof(T).Name, this);
            _messenger.RegisterForMessages<EntityUpdatedEvent>(typeof(T).Name, this);
            _messenger.RegisterForMessages<GroupedFieldChangeEvent>(typeof(T).Name, this);
        }

        public virtual void EnsureIdentityTracked(EntityBase entity)
        {
            if (entity.GetType() != typeof(T))
                throw new InvalidOperationException("The Entity to be Tracked is not the appropriate type for this Generator!");

            T typedEntity = (T)entity;
            T identityEntity;
            if (!_identityMap.TryGetValue(typedEntity.EntityID, out identityEntity))
            {
                _identityMap.Add(entity.EntityID, typedEntity);
            }
            else
            {
                if (identityEntity != typedEntity)
                {
                    throw new InvalidOperationException("An entity is already tracked with this ID!");
                }
            }
        }

        public virtual T GetEntity(string entityID)
        {
            T entity;
            if (_identityMap.TryGetValue(entityID, out entity))
            {
                if (entity.IsDeleted)
                    return null;

                return entity;
            }

            return null;
        }

        public virtual void Handle(EntityUpdatedEvent message)
        {
            T entity = this.GetEntity(message.EntityID);
            entity.ReplayEvents(message.Yield());
        }

        public virtual void Handle(EntityCreatedEvent message)
        {
            var constructor =
                typeof(T)
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(c =>
                {
                    var parameters = c.GetParameters().ToList();
                    if (parameters.Count == 1 && parameters.Single().ParameterType == typeof(EntityCreatedEvent))
                    {
                        return true;
                    }

                    return false;
                });

            T entity = (T)constructor.Invoke(new object[] { message });
            entity.AttachToModel(_model);
            _identityMap[entity.EntityID] = entity;
        }

        public IEnumerable<EntityBase> GetAll()
        {
            return _identityMap.Values;
        }

        EntityBase IIdentityMap.GetEntity(string EntityID) => GetEntity(EntityID);

        public void Handle(GroupedFieldChangeEvent message)
        {
            T entity = this.GetEntity(message.EntityID);
            List<FieldChangeEvent> eventsToBroadcast = message.GroupedEvents.ToList();
            if (entity == null)
            {
                var firstEvent = message.GroupedEvents[0];
                if (firstEvent.EntityType != typeof(T).Name || !(firstEvent is EntityCreatedEvent))
                    throw new InvalidBudgetException("The order of events is incorrect, sub entity events are present for an entity not yet created.");

                _messenger.PublishEvent(typeof(T).Name, (EntityCreatedEvent)firstEvent);
                entity = this.GetEntity(message.EntityID);
                if (entity == null)
                    throw new InvalidBudgetException("The order of events is incorrect, sub entity events are present for an entity not yet created.");

                eventsToBroadcast.Remove(firstEvent);
            }

            foreach (var evt in eventsToBroadcast)
            {
                if (evt.EntityType == typeof(T).Name)
                {
                    _messenger.PublishEvent(typeof(T).Name, evt);
                }
                else
                {
                    entity.HandleSubEntityEvent(evt);
                }
            }
        }
    }
}
