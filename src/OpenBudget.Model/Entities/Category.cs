using OpenBudget.Model.Entities.Generators;
using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace OpenBudget.Model.Entities
{
    public class CategorySnapshot : EntitySnapshot
    {
        public string Name { get; set; }
        public string Note { get; set; }
        public int SortOrder { get; set; }
    }

    public class Category : EntityBase<CategorySnapshot>
    {
        public Category()
            : base(Guid.NewGuid().ToString())
        {
            CategoryMonths = new CategoryMonthFinder(this);
        }

        internal Category(CategorySnapshot snapshot)
            : base(snapshot)
        {
            CategoryMonths = new CategoryMonthFinder(this);
        }

        internal Category(EntityCreatedEvent evt)
            : base(evt)
        {
            CategoryMonths = new CategoryMonthFinder(this);
        }

        public string Name
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public string Note
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public int SortOrder
        {
            get { return GetProperty<int>(); }
            internal set { SetProperty(value); }
        }

        public void SetSortOrder(int position)
        {
            EntityReference parentReference = GetProperty<EntityReference>(nameof(EntityBase.Parent));
            MasterCategory parent = parentReference.ReferencedEntity as MasterCategory;
            if (parent == null || !parent.Categories.IsLoaded)
                throw new InvalidBudgetActionException("You cannot set the SortOrder of a Category when the MasterCategory's Category collection is not loaded.");

            var categories = parent.Categories.OrderBy(c => c.SortOrder).ToList();
            int index = categories.IndexOf(this);
            if (position == index) return;
            if (index >= 0)
            {
                if (position > index)
                {
                    categories.Insert(position, this);
                    categories.RemoveAt(index);
                }
                else if (position < index)
                {
                    categories.Insert(position, this);
                    categories.RemoveAt(index + 1);
                }
            }

            for (int i = 0; i < categories.Count; i++)
            {
                if (categories[i].SortOrder != i)
                {
                    categories[i].SortOrder = i;
                }
            }
        }

        public CategoryMonthFinder CategoryMonths { get; private set; }

        internal override void BeforeSaveChanges(BudgetModel budgetModel)
        {
            base.BeforeSaveChanges(budgetModel);
            DetermineSortOrder(budgetModel);
        }

        private void DetermineSortOrder(BudgetModel budgetModel)
        {
            if (!this.IsAttached)
            {
                DetermineSortOrderImpl();
            }
            else
            {
                if (this.CurrentEvent.Changes.ContainsKey(nameof(EntityBase.Parent)) &&
                    !this.CurrentEvent.Changes.ContainsKey(nameof(Category.SortOrder)))
                {
                    DetermineSortOrderImpl();
                }
            }
        }

        private void DetermineSortOrderImpl()
        {
            var parent = Parent as MasterCategory;
            if (parent.IsAttached)
            {
                if (parent.Categories.IsLoaded)
                {
                    SortOrder = parent.Categories.IndexOf(this);
                }
                else
                {
                    var pendingAdds = parent.Categories.GetPendingAdds();
                    int pendingAddIndex = pendingAdds.IndexOf(this);
                    if (pendingAddIndex == 0)
                    {
                        SortOrder = parent.Model.BudgetStore.SnapshotStore.GetCategoryMaxSortOrder(parent.EntityID) + 1;
                    }
                    else if (pendingAddIndex > 0)
                    {
                        Category first = pendingAdds[0];
                        SortOrder = first.SortOrder + pendingAddIndex;
                    }
                }
            }
            else
            {
                SortOrder = parent.Categories.IndexOf(this);
            }
        }

        protected override void OnAttached(BudgetModel model)
        {
            base.OnAttached(model);
            CategoryMonths.AttachToModel(model);
        }
    }
}
