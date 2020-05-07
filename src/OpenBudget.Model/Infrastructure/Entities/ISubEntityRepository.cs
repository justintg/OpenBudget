using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Entities
{
    public interface ISubEntityRepository<TEntity> where TEntity : SubEntity
    {
        IEnumerable<TEntity> GetEntitiesByParent(string parentType, string parentId);
    }
}
