using System;
using System.Collections.Generic;
using System.Text;
using OpenBudget.Model.BudgetStore.Model;
using OpenBudget.Model.BudgetView.Calculator;

namespace OpenBudget.Model.BudgetStore
{
    public class MemoryBudgetViewCache : IBudgetViewCache
    {
        private BudgetModel _model;
        private BudgetViewCalculatorResult _lastResult;

        public MemoryBudgetViewCache(BudgetModel model)
        {
            _model = model;
        }

        private void RaiseCacheUpdated() => CacheUpdated?.Invoke(this, EventArgs.Empty);

        public event EventHandler CacheUpdated;

        public BudgetViewMonth GetLastBudgetViewMonth(DateTime month)
        {
            throw new NotImplementedException();
        }

        public BudgetViewCategoryMonth GetLastCategoryMonth(string categoryId, DateTime month)
        {
            throw new NotImplementedException();
        }

        public void RecalculateCache()
        {
            BudgetViewCalculator calculator = new BudgetViewCalculator(_model);
            _lastResult = calculator.Calculate();

            RaiseCacheUpdated();
        }
    }
}
