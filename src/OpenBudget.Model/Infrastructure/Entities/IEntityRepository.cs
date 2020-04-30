using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Entities
{
    internal interface IEntityRepository<TEntity> where TEntity : EntityBase
    {
        TEntity GetEntity(string entityId);
        IEnumerable<TEntity> GetEntitiesByParent(string parentType, string parentId);
        IEnumerable<TEntity> GetAllEntities();
    }
}
