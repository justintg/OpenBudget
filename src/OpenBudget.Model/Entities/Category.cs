using OpenBudget.Model.Entities.Generators;
using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
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
            set { SetProperty(value); }
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
