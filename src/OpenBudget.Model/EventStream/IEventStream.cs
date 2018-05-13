using OpenBudget.Model.Event;
using OpenBudget.Model.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.EventStream
{
    public interface IEventStream
    {
        IEnumerable<ModelEvent> CreateEventIterator();

        EventStreamHeader Header { get; }
    }
}
