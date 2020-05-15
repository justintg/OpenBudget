using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Entities.Generators
{
    public class IncomeCategoryFinder : EntityFinder
    {
        internal IncomeCategoryFinder()
        {

        }

        public IncomeCategory GetIncomeCategory(DateTime date)
        {
            string entityId = string.Format("IncomeCategory/{0:yyyyMM}", date);
            return _model.FindEntity<IncomeCategory>(entityId);
        }
    }
}
