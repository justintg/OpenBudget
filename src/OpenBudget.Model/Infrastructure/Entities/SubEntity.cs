using System;
using System.Collections.Generic;
using System.Text;
using OpenBudget.Model.Events;

namespace OpenBudget.Model.Infrastructure.Entities
{
    public abstract class SubEntity : EntityBase
    {
        protected SubEntity(string entityId) : base(entityId)
        {
        }

        protected SubEntity(EntityCreatedEvent evt) : base(evt)
        {
        }

        //Override the Parent property so we can store a reference to the Parent
        //the reference won't be persisted to the EventStore since it is unecessary
        //A SubEntity can't exist outside of it's parent
        EntityBase _parent;

        public override EntityBase Parent
        {
            get => _parent;
            internal set => _parent = value;
        }
    }
}
