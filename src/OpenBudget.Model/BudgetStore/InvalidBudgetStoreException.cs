using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.SQLite
{
    public class InvalidBudgetStoreException : Exception
    {
        public InvalidBudgetStoreException()
        {
        }

        public InvalidBudgetStoreException(string message) : base(message)
        {
        }

        public InvalidBudgetStoreException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
