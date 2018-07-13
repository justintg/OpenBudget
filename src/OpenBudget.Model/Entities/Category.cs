using OpenBudget.Model.Entities.Generators;
using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Entities
{
    public class Category : EntityBase
    {
        public Category()
            : base(Guid.NewGuid().ToString())
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

        public CategoryMonthFinder CategoryMonths { get; private set; }

        internal override void AttachToModel(BudgetModel model)
        {
            base.AttachToModel(model);
            CategoryMonths.AttachToModel(model);
        }
    }
}
