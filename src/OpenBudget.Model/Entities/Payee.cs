using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Entities
{
    public class PayeeSnapshot : EntitySnapshot
    {
        public string Name { get; set; }
    }

    public class Payee : EntityBase<PayeeSnapshot>
    {
        public Payee()
            : base(Guid.NewGuid().ToString())
        {
        }

        internal Payee(PayeeSnapshot snapshot)
            : base(snapshot)
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
