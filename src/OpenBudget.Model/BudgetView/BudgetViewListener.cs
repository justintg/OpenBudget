using OpenBudget.Model.Entities;
using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure.Entities;
using OpenBudget.Model.Util;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;

namespace OpenBudget.Model.BudgetView
{
    public class BudgetViewListener : IDisposable
    {
        private BudgetModel _model;
        private IDisposable _eventSubscription;

        private bool _isBatching;
        private List<ModelEvent> _batchedEvents = new List<ModelEvent>();

        public BudgetViewListener(BudgetModel model)
        {
            _model = model;
            _eventSubscription = _model.MessageBus.EventPublished.Subscribe(e =>
                {
                    if (_isBatching)
                    {
                        _batchedEvents.Add(e);
                    }
                    else
                    {
                        if (ShouldRecalculate(e))
                        {
                            _model.BudgetViewCache.RecalculateCache();
                        }
                    }
                });
        }

        private bool ShouldRecalculate(ModelEvent evt)
        {
            if (evt.EntityType == nameof(Transaction))
            {
                if (evt is EntityCreatedEvent createEvent)
                {
                    return true;
                }
                else if (evt is EntityUpdatedEvent updateEvent)
                {
                    if (updateEvent.Changes.ContainsKey(nameof(Transaction.Amount)))
                    {
                        return true;
                    }
                    else if (updateEvent.Changes.ContainsKey(nameof(Transaction.Category)))
                    {
                        return true;
                    }
                }
                else if (evt is GroupedFieldChangeEvent groupedEvent)
                {
                    foreach (var subEvent in groupedEvent.GroupedEvents)
                    {
                        if (ShouldRecalculate(subEvent))
                        {
                            return true;
                        }
                    }
                }
            }
            else if (evt.EntityType == nameof(SubTransaction))
            {
                if (evt is EntityCreatedEvent)
                {
                    return true;
                }
                else if (evt is EntityUpdatedEvent updateEvent)
                {
                    if (updateEvent.Changes.ContainsKey(nameof(SubTransaction.Amount)))
                    {
                        return true;
                    }
                    else if (updateEvent.Changes.ContainsKey(nameof(SubTransaction.Category)))
                    {
                        return true;
                    }
                }
            }
            else if (evt.EntityType == nameof(Entities.CategoryMonth))
            {
                if (evt is EntityUpdatedEvent updateEvent)
                {
                    if (updateEvent.Changes.ContainsKey(nameof(CategoryMonth.AmountBudgeted)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void Dispose()
        {
            _eventSubscription.Dispose();
        }

        public IDisposable StartBatch()
        {
            _isBatching = true;
            return Disposable.Create(StopBatch);
        }

        private void StopBatch()
        {
            _isBatching = false;
            foreach (var evt in _batchedEvents)
            {
                if (ShouldRecalculate(evt))
                {
                    _model.BudgetViewCache.RecalculateCache();
                    break;
                }
            }
            _batchedEvents.Clear();
        }
    }
}
