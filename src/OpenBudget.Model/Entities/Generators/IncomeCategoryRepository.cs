using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenBudget.Model.Entities.Generators
{
    internal class IncomeCategoryRepository : NoCreateEntityRepository<IncomeCategory, IncomeCategorySnapshot>
    {
        public IncomeCategoryRepository(BudgetModel model) : base(model)
        {
        }

        protected override bool IsValidID(string entityID)
        {
            string regexPattern = @"^IncomeCategory\/[0-9]{4}(0[1-9]|1[0-2])$";
            Regex reg = new Regex(regexPattern);
            return reg.IsMatch(entityID);
        }

        protected override IncomeCategory MaterializeEntity(string entityID)
        {
            return new IncomeCategory(entityID);
        }
    }
}
