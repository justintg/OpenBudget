using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenBudget.Model.Entities
{
    public class IncomeCategorySnapshot : EntitySnapshot
    {
        public DateTime Month { get; set; }
    }

    public class IncomeCategory : NoCreateEntity<IncomeCategorySnapshot>
    {
        internal IncomeCategory(IncomeCategorySnapshot snapshot) : base(snapshot)
        {
        }

        internal IncomeCategory(string entityId) : base(entityId)
        {
            string regexPattern = @"^(.*)\/([0-9]{4})(0[1-9]|1[0-2])$";
            var match = Regex.Match(entityId, regexPattern);
            string id = match.Groups[1].Value;
            int year = int.Parse(match.Groups[2].Value);
            int month = int.Parse(match.Groups[3].Value);

            SetEntityData(new DateTime(year, month, 1), "Month");
        }

        public DateTime Month
        {
            get => GetProperty<DateTime>();
        }
    }
}
