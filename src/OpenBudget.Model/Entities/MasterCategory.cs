using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Entities
{
    public class MasterCategory : EntityBase
    {
        public MasterCategory()
            : base(Guid.NewGuid().ToString())
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

        public EntityCollection<Category> Categories { get; private set; }
    }
}
