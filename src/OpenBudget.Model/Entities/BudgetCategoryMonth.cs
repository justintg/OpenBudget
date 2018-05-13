using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenBudget.Model.Entities
{
    public class BudgetCategoryMonth : NoCreateEntity
    {
        internal BudgetCategoryMonth(string entityId) : base(entityId)
        {
            string regexPattern = @"^(.*)\/([0-9]{4})(0[1-9]|1[0-2])$";
            var match = Regex.Match(entityId, regexPattern);
            string id = match.Groups[1].Value;
            int year = int.Parse(match.Groups[2].Value);
            int month = int.Parse(match.Groups[3].Value);

            _entityData["Parent"] = new EntityReference(nameof(BudgetSubCategory), id);
            _entityData["Month"] = new DateTime(year, month, 1);
        }

        public DateTime Month
        {
            get => GetProperty<DateTime>();
        }

        public double AmountBudgeted
        {
            get => GetProperty<double>();
            set => SetProperty(value);
        }
    }
}
