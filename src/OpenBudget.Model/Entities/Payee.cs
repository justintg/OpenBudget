using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Entities
{
    public class Payee : EntityBase
    {
        public Payee()
            : base(Guid.NewGuid().ToString())
        {
        }

        internal Payee(EntityCreatedEvent evt)
            : base(evt)
        {
        }

        public string Name
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }
    }
}
