using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenBudget.Model.Entities.Generators
{
    public class BudgetCategoryMonthFinder : EntityFinder
    {
        private BudgetSubCategory _parent;

        internal BudgetCategoryMonthFinder(BudgetSubCategory parent)
        {
            _parent = parent;
        }

        public BudgetCategoryMonth GetCategoryMonth(DateTime date)
        {
            string entityId = _parent.EntityID.ToString() + $"/{date:yyyyMM}";
            return _model?.BudgetCategoryMonthGenerator.GetEntity(entityId);
        }

        public IEnumerable<BudgetCategoryMonth> GetAllMaterialized()
        {
            return _model?.BudgetCategoryMonthGenerator.GetAllMaterialized().Where(bc => bc.Parent == _parent);
        }
    }
}
