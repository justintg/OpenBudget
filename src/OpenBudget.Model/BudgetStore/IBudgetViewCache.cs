using OpenBudget.Model.BudgetView.Model;
using System;

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
