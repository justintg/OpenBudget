﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Events
{
    [DataContract]
    public class EntityCreatedEvent : FieldChangeEvent
    {
        public EntityCreatedEvent(string entityName, string entityId)
            : base(entityName, entityId)
        {
        }
    }
}
