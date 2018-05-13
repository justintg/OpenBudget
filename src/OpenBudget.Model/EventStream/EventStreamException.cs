using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.EventStream
{
    public class EventStreamException : Exception
    {
        public EventStreamException(string message) : base(message)
        {

        }

        public EventStreamException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
