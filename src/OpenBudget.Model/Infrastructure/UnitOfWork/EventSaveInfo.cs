using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Infrastructure.UnitOfWork
{
    public delegate void NotifyEventSavedHandler(ModelEvent evt);

    public class EventSaveInfo
    {
        public bool NeedsAttach { get; set; }
        public EntityBase Entity { get; set; }
        public ModelEvent Event { get; set; }
        public NotifyEventSavedHandler EventSavedCallback { get; set; }
    }
}
