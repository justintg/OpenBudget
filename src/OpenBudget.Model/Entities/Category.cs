using OpenBudget.Model.Entities.Generators;
using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Entities
{
    public class CategorySnapshot : EntitySnapshot
    {
        public string Name { get; set; }
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

        public CategoryMonthFinder CategoryMonths { get; private set; }

        /*protected override void OnAttachAttachToModel(BudgetModel model)
        {
            base.AttachToModel(model);
            CategoryMonths.AttachToModel(model);
        }*/
    }
}
