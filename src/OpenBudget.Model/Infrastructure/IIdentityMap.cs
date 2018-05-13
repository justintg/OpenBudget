using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Infrastructure
{
    public interface IIdentityMap
    {
        void EnsureIdentityTracked(EntityBase entity);

        EntityBase GetEntity(string EntityID);

        IEnumerable<EntityBase> GetAll();
    }
}
