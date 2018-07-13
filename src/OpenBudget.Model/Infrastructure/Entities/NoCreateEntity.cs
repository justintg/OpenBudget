using OpenBudget.Model.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Entities
{
    public class NoCreateEntity : EntityBase
    {
        internal NoCreateEntity(string entityId) : base(entityId)
        {
            this.CurrentEvent = new EntityUpdatedEvent(this.GetType().Name, entityId);
        }

        public override void Delete()
        {
            throw new NotSupportedException();
        }
    }
}
