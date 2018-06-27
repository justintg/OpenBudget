using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenBudget.Model.Entities.Generators
{
    internal class CategoryMonthGenerator : NoCreateEntityGenerator<CategoryMonth>
    {
        public CategoryMonthGenerator(BudgetModel model) : base(model)
        {
        }

        protected override bool IsValidID(string entityID)
        {
            string regexPattern = @"^(.*)\/[0-9]{4}(0[1-9]|1[0-2])$";
            var match = Regex.Match(entityID, regexPattern);
            if (!match.Success) return false;

            var budgetCategoryID = match.Groups[1].Value;
            var budgetCategory = _model.BudgetSubCategoryGenerator.GetEntity(budgetCategoryID);

            if (budgetCategory != null) return true;

            return false;
        }

        protected override CategoryMonth MaterializeEntity(string entityID)
        {
            return new CategoryMonth(entityID);
        }
    }
}
