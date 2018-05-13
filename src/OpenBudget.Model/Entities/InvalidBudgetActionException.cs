using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Entities
{
    public class InvalidBudgetActionException : Exception
    {
        public InvalidBudgetActionException()
        {
        }

        public InvalidBudgetActionException(string message) : base(message)
        {
        }

        public InvalidBudgetActionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
