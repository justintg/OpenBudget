using OpenBudget.Model.Entities.Generators;
using OpenBudget.Model.Event;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Entities
{
    public class BudgetSubCategory : EntityBase
    {
        public BudgetSubCategory()
            : base(Guid.NewGuid().ToString())
        {
            CategoryMonths = new BudgetCategoryMonthFinder(this);
        }

        internal BudgetSubCategory(EntityCreatedEvent evt)
            : base(evt)
        {
            CategoryMonths = new BudgetCategoryMonthFinder(this);
        }

        public string Name
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public BudgetCategoryMonthFinder CategoryMonths { get; private set; }

        internal override void AttachToModel(BudgetModel model)
        {
            base.AttachToModel(model);
            CategoryMonths.AttachToModel(model);
        }
    }
}
