using System;
using System.Collections.Generic;
using System.Text;
using OpenBudget.Model.Event;
using OpenBudget.Model.EventStream;

namespace OpenBudget.Model.Infrastructure
{
    public class InMemoryEventStream : IEventStream
    {
        private Func<IEnumerable<ModelEvent>> _intializeEventIterator;
        private EventStreamHeader _header;

        public InMemoryEventStream(Func<IEnumerable<ModelEvent>> intializeEventIterator, EventStreamHeader header)
        {
            _intializeEventIterator = intializeEventIterator;
            _header = header;
        }

        public EventStreamHeader Header => _header;

        public IEnumerable<ModelEvent> CreateEventIterator() => _intializeEventIterator();
    }
}
