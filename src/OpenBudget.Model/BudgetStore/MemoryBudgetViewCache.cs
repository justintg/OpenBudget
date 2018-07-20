using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenBudget.Model.BudgetStore.Model;
using OpenBudget.Model.BudgetView.Calculator;
using OpenBudget.Model.Entities;
using OpenBudget.Util.Collections;

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

        public BudgetViewMonth GetLastBudgetViewMonth(DateTime month, out bool exactMatch)
        {
            if (_lastResult == null) RecalculateCache();

            BudgetViewMonth budgetViewMonth = null;
            if (_lastResult.Months.TryGetValue(month, out budgetViewMonth))
            {
                exactMatch = true;
                return budgetViewMonth;
            }

            int index = _lastResult.MonthsByDate.BinarySearch(m => m.Month, month);
            index = ~index;
            exactMatch = false;
            if (index == 0) return null;
            return _lastResult.MonthsByDate[index - 1];
        }

        public BudgetViewCategoryMonth GetLastCategoryMonth(string categoryId, DateTime month, out bool exactMatch)
        {
            if (_lastResult == null) RecalculateCache();

            CategoryMonthKey key = new CategoryMonthKey(nameof(Category), categoryId, month);
            BudgetViewCategoryMonth categoryMonth = null;
            if (_lastResult.CategoryMonths.TryGetValue(key, out categoryMonth))
            {
                exactMatch = true;
                return categoryMonth;
            }

            List<BudgetViewCategoryMonth> monthList = null;
            if (_lastResult.CategoryMonthsOrdered.TryGetValue(new CategoryKey(key), out monthList))
            {
                int index = monthList.BinarySearch(m => m.Month, month);
                index = ~index;
                exactMatch = false;
                if (index == 0) return null;
                return monthList[index - 1];
            }
            else
            {
                exactMatch = false;
                return null;
            }
        }

        public void RecalculateCache()
        {
            BudgetViewCalculator calculator = new BudgetViewCalculator(_model);
            _lastResult = calculator.Calculate();

            RaiseCacheUpdated();
        }
    }
}
