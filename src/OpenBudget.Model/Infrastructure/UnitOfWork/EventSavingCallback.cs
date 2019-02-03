using OpenBudget.Model.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Infrastructure.UnitOfWork
{
    public delegate void NotifyEventSaved(ModelEvent evt);
    public delegate void AttachToModel(BudgetModel model);

    public class EventSavingCallback
    {
        public ModelEvent Event { get; set; }
        public NotifyEventSaved EventSavedCallback { get; set; }
    }
}
