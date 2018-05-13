using OpenBudget.Model.Event;
using OpenBudget.Model.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Entities
{
    internal abstract class NoCreateEntityGenerator<T> : EntityGenerator<T>, IHasChanges where T : NoCreateEntity
    {
        public NoCreateEntityGenerator(BudgetModel model) : base(model)
        {

        }

        /// <summary>
        /// We explicitly override this method to ensure that this generator doesn't 
        /// register itself for EntityCreatedEvent
        /// </summary>
        protected override void RegisterForMessages()
        {
            _messenger.RegisterForMessages<EntityUpdatedEvent>(typeof(T).Name, this);
        }

        public override void Handle(EntityCreatedEvent message)
        {
            throw new NotSupportedException("This Generator cannot handle the EntityCreatedEvent");
        }

        protected abstract T MaterializeEntity(string entityID);

        protected abstract bool IsValidID(string entityID);

        public override T GetEntity(string entityID)
        {
            T entity = base.GetEntity(entityID);
            if (entity != null)
            {
                return entity;
            }
            else if (entity == null && IsValidID(entityID))
            {
                entity = MaterializeEntity(entityID);
                entity.AttachToModel(_model); //This will call EnsureIdentityTracked and add the entity to the IdentityMap
                return entity;
            }
            else
                return null;
        }

        internal virtual IEnumerable<ModelEvent> GetAndSaveChanges()
        {
            foreach (var entity in _identityMap.Values)
            {
                foreach (var change in entity.GetAndSaveChanges())
                {
                    yield return change;
                }
            }
        }

        internal virtual void BeforeSaveChanges()
        {
            foreach (var entity in _identityMap.Values)
            {
                entity.BeforeSaveChanges();
            }
        }

        IEnumerable<ModelEvent> IHasChanges.GetAndSaveChanges()
        {
            return this.GetAndSaveChanges();
        }

        void IHasChanges.BeforeSaveChanges()
        {
            this.BeforeSaveChanges();
        }
    }
}
