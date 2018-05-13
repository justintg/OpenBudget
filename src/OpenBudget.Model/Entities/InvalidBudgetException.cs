using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Entities
{
    public class InvalidBudgetException : Exception
    {
        public InvalidBudgetException()
        {
        }

        public InvalidBudgetException(string message) : base(message)
        {
        }

        public InvalidBudgetException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
