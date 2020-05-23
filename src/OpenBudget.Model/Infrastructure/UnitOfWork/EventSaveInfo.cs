using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Infrastructure.UnitOfWork
{
    public delegate void NotifyEventSavedHandler(EventSaveInfo eventSaveInfo);

    public class EventSaveInfo
    {
        public bool NeedsAttach { get; set; }
        public EntityBase Entity { get; set; }
        public EntitySnapshot Snapshot { get; set; }
        public ModelEvent Event { get; set; }
        public NotifyEventSavedHandler EventSavedCallback { get; set; }
        public List<EventSaveInfo> SubEntityEvents { get; set; }

        public IEnumerable<EntitySnapshot> GetSnapshots()
        {
            yield return Snapshot;
            if (SubEntityEvents != null)
            {
                foreach (var subEntityEvent in SubEntityEvents)
                {
                    var snapshots = subEntityEvent.GetSnapshots();
                    foreach (var subEntitySnapshot in snapshots)
                    {
                        yield return subEntitySnapshot;
                    }
                }
            }
        }
    }
}
