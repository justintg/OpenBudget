using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Infrastructure.UnitOfWork
{
    internal class UnitOfWork
    {
        private List<EntityBase> _changedEntities = new List<EntityBase>();

        public void RegisterChangedEntity(EntityBase entity)
        {
            _changedEntities.Add(entity);
        }



        public List<ModelEvent> GetChangedEventsAndNotifyEntities()
        {
            List<ModelEvent> events = new List<ModelEvent>();
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
