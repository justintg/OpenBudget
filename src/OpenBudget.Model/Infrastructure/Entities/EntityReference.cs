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

        [IgnoreDataMember]
        public bool IsReferenceResolved { get; private set; }

        [IgnoreDataMember]
        public EntityBase ReferencedEntity { get; private set; }

        internal EntityReference(string entityType, string entityId)
        {
            this.EntityType = entityType;
            this.EntityID = entityId;
            IsReferenceResolved = false;
        }

        internal EntityReference(EntityBase entity)
        {
            this.EntityType = entity.GetType().Name;
            this.EntityID = entity.EntityID;
            this.ReferencedEntity = entity;
            IsReferenceResolved = true;
        }

        [JsonConstructor]
        private EntityReference()
        {
            IsReferenceResolved = false;
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
            IsReferenceResolved = true;
            ReferencedEntity = entity;
            return entity;
        }
    }
}
