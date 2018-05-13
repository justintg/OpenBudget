using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.SQLite
{
    public class InvalidEventStoreException : Exception
    {
        public InvalidEventStoreException()
        {
        }

        public InvalidEventStoreException(string message) : base(message)
        {
        }
    }
}
