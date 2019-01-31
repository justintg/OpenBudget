using OpenBudget.Model.Entities;
using OpenBudget.Model.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.BudgetView.Model
{
    public class CategoryMonthKey : IEquatable<CategoryMonthKey>
    {
        public string EntityType { get; private set; }
        public string EntityID { get; private set; }
        public DateTime FirstDayOfMonth { get; private set; }

        public CategoryMonthKey(string entityType, string entityId, DateTime date)
        {
            EntityType = entityType;
            EntityID = entityId;
            FirstDayOfMonth = date.FirstDayOfMonth();
        }

        public CategoryMonthKey(Category category, DateTime date)
        {
            EntityType = nameof(Category);
            EntityID = category.EntityID;
            FirstDayOfMonth = date.FirstDayOfMonth();
        }

        public CategoryMonthKey(IncomeCategory category)
        {
            EntityType = nameof(IncomeCategory);
            EntityID = category.EntityID;

            FirstDayOfMonth = category.Month.FirstDayOfMonth();
        }

        public bool Equals(CategoryMonthKey other)
        {
            if (other == null) return false;
            if (ReferenceEquals(other, this)) return true;
            return this.EntityType == other.EntityType && this.EntityID == other.EntityID && this.FirstDayOfMonth == other.FirstDayOfMonth;

            throw new InvalidOperationException("Either Category or IncomeCategory must be non-null");
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CategoryMonthKey);
        }

        public override int GetHashCode()
        {
            int hashCode = EntityType.GetHashCode();
            hashCode = (hashCode * 397) ^ (EntityID.GetHashCode());
            hashCode = (hashCode * 397) ^ (FirstDayOfMonth.GetHashCode());
            return hashCode;
        }
    }

}
