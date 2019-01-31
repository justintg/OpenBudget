using OpenBudget.Model.BudgetView.Model;
using System;
using System.Collections.Generic;

namespace OpenBudget.Model.BudgetView.Calculator
{
    public class BudgetViewCalculatorResult
    {
        public Dictionary<CategoryMonthKey, BudgetViewCategoryMonth> CategoryMonths { get; set; }
        public Dictionary<CategoryKey, List<BudgetViewCategoryMonth>> CategoryMonthsOrdered { get; set; }
        public Dictionary<DateTime, List<BudgetViewCategoryMonth>> CategoryMonthsByMonth { get; set; }
        public Dictionary<DateTime, BudgetViewMonth> Months { get; set; }
        public List<BudgetViewMonth> MonthsByDate { get; set; }
    }
}
