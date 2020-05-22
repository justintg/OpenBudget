using OpenBudget.Model.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Entities
{
    public class EntityLookupRoot
    {
        private Dictionary<EntityReference, EntityBase> _loadedEntities = new Dictionary<EntityReference, EntityBase>();

        public EntityLookupRoot()
        {

        }

        public bool TryGetValue<TEntity>(EntityReference reference, out TEntity entity)
            where TEntity : EntityBase
        {
            if (_loadedEntities.TryGetValue(reference, out EntityBase baseEntity))
            {
                if (baseEntity is TEntity typedEntity)
                {
                    entity = typedEntity;
                    return true;
                }
            }

            entity = null;
            return false;
        }

        public void Add(EntityBase entity)
        {
            _loadedEntities.Add(new EntityReference(entity), entity);
        }

        public T ResolveEntity<T>(BudgetModel model, EntityReference entityReference) where T : EntityBase
        {
            var genericEntity = ResolveEntityGeneric(model, entityReference);
            if (genericEntity is T)
                return (T)genericEntity;
            else
                return null;
        }

        public EntityBase ResolveEntityGeneric(BudgetModel budgetModel, EntityReference entityReference)
        {
            if (_loadedEntities.TryGetValue(entityReference, out EntityBase entity))
            {
                return entity;
            }
            else
            {
                if (entityReference.IsReferenceResolved(budgetModel))
                {
                    EntityBase referencedEntity = entityReference.ReferencedEntity;
                    _loadedEntities.Add(entityReference, referencedEntity);
                    return referencedEntity;
                }
                else
                {
                    EntityBase referencedEntity = entityReference.ResolveGeneric(budgetModel);
                    _loadedEntities.Add(entityReference, referencedEntity);
                    referencedEntity.LookupRoot = this;
                    return referencedEntity;
                }
            }
        }
    }
}
