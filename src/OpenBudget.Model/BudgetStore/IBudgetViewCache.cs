using OpenBudget.Model.BudgetStore.Model;
using OpenBudget.Model.BudgetView.Calculator;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.BudgetStore
{
    public interface IBudgetViewCache
    {
        void RecalculateCache();

        event EventHandler CacheUpdated;

        BudgetViewMonth GetLastBudgetViewMonth(DateTime month, out bool exactMatch);

        BudgetViewCategoryMonth GetLastCategoryMonth(string categoryId, DateTime month, out bool exactMatch);
    }
}
