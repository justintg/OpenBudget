using OpenBudget.Model.Event;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Entities
{
    public class BudgetCategory : EntityBase
    {
        public BudgetCategory()
            : base(Guid.NewGuid().ToString())
        {
            SubCategories = RegisterChildEntityCollection(new EntityCollection<BudgetSubCategory>(this));
        }

        internal BudgetCategory(EntityCreatedEvent evt)
            : base(evt)
        {
            SubCategories = RegisterChildEntityCollection(new EntityCollection<BudgetSubCategory>(this));
        }

        public string Name
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public EntityCollection<BudgetSubCategory> SubCategories { get; private set; }
    }
}
