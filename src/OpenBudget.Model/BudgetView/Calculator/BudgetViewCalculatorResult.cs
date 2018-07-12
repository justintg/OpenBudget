using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.BudgetView.Calculator
{
    internal class BudgetViewCalculatorResult
    {
        public Dictionary<BudgetViewCalculatorCategoryMonth, BudgetViewCalculatorCategoryMonthResult> CategoryResults { get; private set; }
        public Dictionary<DateTime, BudgetViewCalculatorMonthResult> MonthResults { get; private set; }

    }
}
