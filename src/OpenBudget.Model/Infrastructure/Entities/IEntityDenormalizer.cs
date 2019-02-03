using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Entities
{
    internal interface IEntityDenormalizer
    {
        void RegisterForChanges(EntityBase entity);
    }
}
