using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.Model
{
    public class ValidBudgetCheck
    {
        public bool IsValid { get; private set; }
        public string Error { get; private set; }


        public ValidBudgetCheck(bool isValid, string errors)
        {
            this.IsValid = isValid;
            this.Error = errors;
        }
    }
}
