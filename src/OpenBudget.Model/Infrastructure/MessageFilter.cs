using OpenBudget.Model.Entities;
using OpenBudget.Model.Event;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenBudget.Model.Infrastructure
{
    public class PropertyChangedMessageFilter<T> : IHandler<EntityUpdatedEvent>, IHandler<EntityCreatedEvent> where T : EntityBase
    {
        Action<FieldChangeEvent> _triggerAction;
        Predicate<string> _whereEntityId;
        string _propertyName;

        public PropertyChangedMessageFilter(BudgetModel model, Predicate<string> whereEntityId, string propertyName, Action<FieldChangeEvent> triggerAction)
        {
            _triggerAction = triggerAction;
            _whereEntityId = whereEntityId;
            _propertyName = propertyName;

            model.MessageBus.RegisterForMessages<EntityUpdatedEvent>(typeof(T).Name, this);
            model.MessageBus.RegisterForMessages<EntityCreatedEvent>(typeof(T).Name, this);
        }

        public void Handle(EntityUpdatedEvent message)
        {
            if (_whereEntityId(message.EntityID) && message.Changes.ContainsKey(_propertyName))
            {
                _triggerAction(message);
            }
        }

        public void Handle(EntityCreatedEvent message)
        {
            if (_whereEntityId(message.EntityID) && message.Changes.ContainsKey(_propertyName))
            {
                _triggerAction(message);
            }
        }
    }
}
