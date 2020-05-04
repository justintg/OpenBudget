using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Entities
{
    internal interface IEntityRepository
    {
        EntityBase GetEntityBase(string entityId);
    }

    internal interface IEntityRepository<TEntity> : IEntityRepository where TEntity : EntityBase
    {
        TEntity GetEntity(string entityId);
        IEnumerable<TEntity> GetEntitiesByParent(string parentType, string parentId);
        IEnumerable<TEntity> GetAllEntities();
    }
}
