using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenBudget.Model.Entities.Collections
{
    public class CategoryCollection : EntityCollection<Category>
    {
        internal CategoryCollection(EntityBase parent, bool isLoaded = false) : base(parent, isLoaded)
        {
        }

        protected override void OnBeforeAddItem(Category item)
        {
            if (item.IsAttached)
            {
                var parentReference = item.GetProperty<EntityReference>(nameof(EntityBase.Parent));
                var masterCategory = parentReference.ReferencedEntity as MasterCategory;
                if (masterCategory == null || !masterCategory.Categories.IsLoaded)
                {
                    throw new InvalidBudgetActionException("You cannot move a category to a new MasterCategory when the previous MasterCategory's Category collection is not loaded.");
                }

                var categories = masterCategory.Categories.Where(c => c != item).ToList();
                for (int i = 0; i < categories.Count; i++)
                {
                    categories[i].SortOrder = i;
                }
            }
        }

        protected override void OnAfterAddItem(Category item)
        {
        }
    }
}
