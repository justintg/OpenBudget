using OpenBudget.Model.Event;
using OpenBudget.Model.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Infrastructure.Entities
{
    internal interface IEntityCollection : IHasChanges
    {
        void AttachToModel(BudgetModel model);
        void ForceRemoveChild(EntityBase child);
    }
}
