using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace OpenBudget.Model.Infrastructure.Messaging
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
