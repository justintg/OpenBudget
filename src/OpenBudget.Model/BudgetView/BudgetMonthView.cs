using OpenBudget.Model.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.BudgetView
{
    public class BudgetMonthView : IDisposable
    {
        private BudgetModel _model;
        private DateTime _date;

        public BudgetMonthView(BudgetModel model, DateTime date)
        {
            _model = model;
            _date = date.FirstDayOfMonth();
        }

        public void Dispose()
        {
        }
    }
}
