using OpenBudget.Model.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Infrastructure.UnitOfWork
{
    public delegate void NotifyEventStamped();
    public delegate void AttachToModel(BudgetModel model);

    public class EventSavingCallback
    {
        public ModelEvent Event { get; set; }
        public NotifyEventStamped EventStampedCallback { get; set; }
        public AttachToModel AttachToModelCallback { get; set; }
    }
}
