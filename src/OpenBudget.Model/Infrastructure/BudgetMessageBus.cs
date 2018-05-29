using OpenBudget.Model.Event;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Infrastructure
{
    public class BudgetMessageBus : Messenger<ModelEvent>
    {
        public BudgetMessageBus()
        {
        }

        public IObservable<ModelEvent> EventPublished
        {
            get => _modelEventSubject.AsObservable();
        }

        public IObservable<EntityCreatedEvent> EntityCreated
        {
            get => EventPublished.Where(e => e is EntityCreatedEvent).Select(e => e as EntityCreatedEvent);
        }

        public IObservable<EntityUpdatedEvent> EntityUpdated
        {
            get => EventPublished.Where(e => e is EntityUpdatedEvent).Select(e => e as EntityUpdatedEvent);
        }

        public IObservable<EntityUpdatedEvent> EntityDeleted
        {
            get => EntityUpdated.Where(e => e.Changes.ContainsKey(nameof(EntityBase.IsDeleted)));
        }

        public IObservable<FieldChangeEvent> EntityCreatedOrUpdated
        {
            get => EventPublished.Where(e => e is FieldChangeEvent).Select(e => e as FieldChangeEvent);
        }

        private Subject<ModelEvent> _modelEventSubject = new Subject<ModelEvent>();

        public override void PublishEvent<TMessage>(string entityName, TMessage message)
        {
            base.PublishEvent(entityName, message);
            _modelEventSubject.OnNext(message);
        }
    }
}
