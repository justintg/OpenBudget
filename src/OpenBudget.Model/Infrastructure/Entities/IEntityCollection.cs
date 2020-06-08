using OpenBudget.Model.Events;
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
    internal interface IEntityCollection
    {
        void AttachToModel(BudgetModel model);
        void RequestDeletion(EntityBase child);
        void CancelDeletion(EntityBase child);
        void ForceRemoveChild(EntityBase child);
        void ForceAddChild(EntityBase child);
        void EnsureContainsMaterializedChild(EntityBase child);
        IEnumerable<EntityBase> EnumerateUnattachedEntities();
        IEnumerable<EntityBase> EnumerateChildren();
        IList<EntityBase> GetPendingAdds();
        bool IsLoaded { get; }
        int IndexOf(EntityBase child);
    }
}
