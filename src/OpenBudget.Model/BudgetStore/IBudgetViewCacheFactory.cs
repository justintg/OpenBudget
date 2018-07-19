using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.BudgetStore
{
    public interface IBudgetViewCacheFactory
    {
        IBudgetViewCache CreateCache(BudgetModel model);
    }
}
