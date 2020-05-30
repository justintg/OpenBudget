using OpenBudget.Model.Entities.Collections;
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
    }

    public class MasterCategory : EntityBase<MasterCategorySnapshot>
    {
        public MasterCategory()
            : base(Guid.NewGuid().ToString())
        {
            Categories = RegisterChildEntityCollection(new CategoryCollection(this, true));
        }

        internal MasterCategory(MasterCategorySnapshot snapshot) : base(snapshot)
        {
            Categories = RegisterChildEntityCollection(new CategoryCollection(this));
        }

        internal MasterCategory(EntityCreatedEvent evt)
            : base(evt)
        {
            Categories = RegisterChildEntityCollection(new CategoryCollection(this));
        }

        public string Name
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public EntityCollection<Category> Categories { get; private set; }
    }
}
