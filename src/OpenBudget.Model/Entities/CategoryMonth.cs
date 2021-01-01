using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenBudget.Model.Entities
{
    public class CategoryMonthSnapshot : EntitySnapshot
    {
        public DateTime Month { get; set; }
        public long AmountBudgeted { get; set; }
        public int AmountBudgeted_Denominator { get; set; }

        public NegativeBalanceHandlingTypes? NegativeBalanceHandling { get; set; } = null;
    }

    public enum NegativeBalanceHandlingTypes
    {
        AvailableToBudget = 0,
        CarryForwardBalance = 1
    }



    public class CategoryMonth : NoCreateEntity<CategoryMonthSnapshot>
    {
        internal CategoryMonth(CategoryMonthSnapshot snapshot) : base(snapshot)
        {
        }

        internal CategoryMonth(string entityId) : base(entityId)
        {
            string regexPattern = @"^(.*)\/([0-9]{4})(0[1-9]|1[0-2])$";
            var match = Regex.Match(entityId, regexPattern);
            string id = match.Groups[1].Value;
            int year = int.Parse(match.Groups[2].Value);
            int month = int.Parse(match.Groups[3].Value);

            SetEntityData<EntityReference>(new EntityReference(nameof(Category), id), "Parent");
            SetEntityData<DateTime>(new DateTime(year, month, 1), "Month");
        }

        public DateTime Month
        {
            get => GetProperty<DateTime>();
        }

        public decimal AmountBudgeted
        {
            get => GetCurrency();
            set => SetCurrency(value);
        }

        public NegativeBalanceHandlingTypes? NegativeBalanceHandling
        {
            get => GetProperty<NegativeBalanceHandlingTypes?>();
            set => SetProperty(value);
        }
    }
}
