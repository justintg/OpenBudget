using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Infrastructure.UnitOfWork
{
    internal class UnitOfWork
    {
        private Dictionary<string, EntityBase> _entityIdentityMap = new Dictionary<string, EntityBase>();
        private List<EntityBase> _changedEntities = new List<EntityBase>();

        public void RegisterChangedEntity(EntityBase entity)
        {
            if (_entityIdentityMap.ContainsKey(entity.EntityID))
            {
                throw new InvalidOperationException("This entity has already been registered for changes. You cannot perform operations on different copies of the same entity.");
            }
            _entityIdentityMap.Add(entity.EntityID, entity);
            _changedEntities.Add(entity);

            foreach (var childCollection in entity.EnumerateChildEntityCollections())
            {
                foreach (var childEntity in childCollection.EnumerateUnattachedEntities())
                {
                    this.RegisterChangedEntity(childEntity);
                }
            }

            UpdateEntityState(entity);
        }

        private void UpdateEntityState(EntityBase entity)
        {
            if (entity.SaveState == EntitySaveState.Unattached)
            {
                entity.SaveState = EntitySaveState.UnattachedRegistered;
            }
            else if (entity.SaveState == EntitySaveState.AttachedNoChanges)
            {
                entity.SaveState = EntitySaveState.AttachedHasChanges;
            }
        }

        public List<EventSavingCallback> GetChangeEvents()
        {
            List<EventSavingCallback> events = new List<EventSavingCallback>();
            foreach (var entity in _changedEntities)
            {
                entity.BeforeSaveChanges();
                var entityEvents = entity.GetAndSaveChanges();
                events.AddRange(entityEvents);
            }

            return events;
        }
    }
}
