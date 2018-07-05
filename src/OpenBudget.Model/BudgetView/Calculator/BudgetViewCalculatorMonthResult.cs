using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.BudgetView.Calculator
{
    internal struct BudgetViewCalculatorMonthResult
    {
        public decimal Income;
        public decimal Budgeted;
        public decimal OverUnderBudgetedPreviousMonth;
        public decimal OverspentPreviousMonth;
        public decimal AvailableToBudget;
    }
}
