using OpenBudget.Model.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Entities
{
    internal interface IHasChanges
    {
        void BeforeSaveChanges();

        IEnumerable<ModelEvent> GetAndSaveChanges();
    }
}
