using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.BudgetView.Calculator
{
    internal class BudgetViewCalculatorCategoryMonthResult
    {
        public string CategoryID;
        public DateTime Month;
        public decimal BeginningBalance = 0M;
        public decimal TransactionsInMonth = 0M;
        public decimal AmountBudgeted = 0M;
        public decimal EndBalance = 0M;

        public BudgetViewCalculatorCategoryMonthResult(string categoryID, DateTime month)
        {
            this.CategoryID = categoryID;
            this.Month = month;
        }
    }
}
