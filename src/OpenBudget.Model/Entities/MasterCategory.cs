using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Entities
{
    public class MasterCategorySnapshot : EntitySnapshot
    {
        public string Name { get; set; }
        public int SortOrder { get; set; }
    }

    public class MasterCategory : EntityBase<MasterCategorySnapshot>, ISortableEntity
    {
        public MasterCategory()
            : base(Guid.NewGuid().ToString())
        {
            Categories = RegisterChildEntityCollection(new EntityCollection<Category>(this, true));
        }

        internal MasterCategory(MasterCategorySnapshot snapshot) : base(snapshot)
        {
            Categories = RegisterChildEntityCollection(new EntityCollection<Category>(this));
        }

        internal MasterCategory(EntityCreatedEvent evt)
            : base(evt)
        {
            Categories = RegisterChildEntityCollection(new EntityCollection<Category>(this));
        }

        public string Name
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

        public EntityCollection<Category> Categories { get; private set; }

        int ISortableEntity.SortOrder => SortOrder;

        void ISortableEntity.ForceSetSortOrder(int position)
        {
            SortOrder = position;
        }

        int ISortableEntity.GetMaxSnapshotSortOrder(ISnapshotStore snapshotStore)
        {
            return snapshotStore.GetMasterCategoryMaxSortOrder();
        }

        IEntityCollection ISortableEntity.GetParentCollection()
        {
            var parentReference = GetProperty<EntityReference>(nameof(EntityBase.Parent));
            var budget = parentReference?.ReferencedEntity as Budget;
            return budget?.MasterCategories;
        }
    }
}
