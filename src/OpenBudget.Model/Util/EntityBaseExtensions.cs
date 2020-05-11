using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace OpenBudget.Model.Util
{
    public static class EntityBaseExtensions
    {
        public static EntityCollection<TChild> SelectAndLoad<T, TChild>(this T entity, Expression<Func<T, EntityCollection<TChild>>> childExpression)
            where T : EntityBase
            where TChild : EntityBase
        {
            var childFunc = childExpression.Compile();
            var child = childFunc(entity);
            child.LoadCollection();
            return child;
        }

        public static IEnumerable<TChild> SelectManyAndLoad<T, TChild>(this IEnumerable<T> entityCollection, Expression<Func<T, EntityCollection<TChild>>> childExpression)
            where T : EntityBase
            where TChild : EntityBase
        {
            var childFunc = childExpression.Compile();
            foreach (var parent in entityCollection)
            {
                var childCollection = childFunc(parent);
                childCollection.LoadCollection();
                foreach (var child in childCollection)
                {
                    yield return child;
                }
            }
        }
    }
}
