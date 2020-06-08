using OpenBudget.Model.BudgetStore;
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

    public class Category : EntityBase<CategorySnapshot>, ISortableEntity
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
            SetSortOrderImpl(this, position);
        }

        public CategoryMonthFinder CategoryMonths { get; private set; }

        protected override void OnAttached(BudgetModel model)
        {
            base.OnAttached(model);
            CategoryMonths.AttachToModel(model);
        }

        void ISortableEntity.ForceSetSortOrder(int position)
        {
            SortOrder = position;
        }

        IEntityCollection ISortableEntity.GetParentCollection()
        {
            var parentReference = GetProperty<EntityReference>(nameof(EntityBase.Parent));
            var masterCategory = parentReference?.ReferencedEntity as MasterCategory;
            return masterCategory?.Categories;
        }

        int ISortableEntity.GetMaxSnapshotSortOrder(ISnapshotStore snapshotStore)
        {
            var parentReference = GetProperty<EntityReference>(nameof(EntityBase.Parent));
            return snapshotStore.GetCategoryMaxSortOrder(parentReference.EntityID);
        }
    }
}
