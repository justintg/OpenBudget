using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.BudgetView.Model
{
    public class BudgetViewMonth
    {
        public DateTime Month;
        public decimal Income;
        public decimal Budgeted;
        public decimal OverUnderBudgetedPreviousMonth;
        public decimal OverspentPreviousMonth;
        public decimal AvailableToBudget;

        public BudgetViewMonth(DateTime month)
        {
            this.Month = month;
        }
    }
}
