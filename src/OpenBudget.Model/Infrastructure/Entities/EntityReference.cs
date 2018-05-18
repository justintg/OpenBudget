using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Entities
{
    [DataContract]
    public class EntityReference
    {
        [DataMember]
        public string EntityType { get; private set; }

        [DataMember]
        public string EntityID { get; private set; }

        public bool IsReferenceResolved(BudgetModel model)
        {
            return this.ReferencedEntity != null && (this.ReferencedEntity.Model == model || !this.ReferencedEntity.IsAttached);
        }

        [IgnoreDataMember]
        public EntityBase ReferencedEntity { get; private set; }

        internal EntityReference(string entityType, string entityId)
        {
            this.EntityType = entityType;
            this.EntityID = entityId;
        }

        internal EntityReference(EntityBase entity)
        {
            this.EntityType = entity.GetType().Name;
            this.EntityID = entity.EntityID;
            this.ReferencedEntity = entity;
        }

        [JsonConstructor]
        private EntityReference()
        {
            ReferencedEntity = null;
        }

        public T Resolve<T>(BudgetModel model) where T : EntityBase
        {
            var genericEntity = ResolveGeneric(model);
            if (genericEntity is T)
                return (T)genericEntity;
            else
                return null;
        }

        public EntityBase ResolveGeneric(BudgetModel model)
        {
            var entity = model.FindEntity(this.EntityType, this.EntityID);
            ReferencedEntity = entity;
            return entity;
        }
    }
}
