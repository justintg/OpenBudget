using OpenBudget.Model.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.BudgetView.Calculator
{
    public class CategoryKey : IEquatable<CategoryKey>
    {
        public string EntityType { get; private set; }
        public string EntityID { get; private set; }

        public CategoryKey(Category category)
        {
            EntityType = nameof(Category);
            EntityID = category.EntityID;
        }

        public CategoryKey(CategoryMonthKey categoryMonth)
        {
            EntityType = categoryMonth.EntityType;
            EntityID = categoryMonth.EntityID;
        }

        public CategoryKey(string entityType, string entityId)
        {
            EntityType = entityType;
            EntityID = entityId;
        }

        public bool Equals(CategoryKey other)
        {
            if (other == null) return false;
            if (ReferenceEquals(other, this)) return true;
            return this.EntityType == other.EntityType && this.EntityID == other.EntityID;

            throw new InvalidOperationException("Either Category or IncomeCategory must be non-null");
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CategoryKey);
        }

        public override int GetHashCode()
        {
            int hashCode = EntityType.GetHashCode();
            hashCode = (hashCode * 397) ^ (EntityID.GetHashCode());
            return hashCode;
        }
    }
}
