using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace OpenBudget.Model.Util
{
    public static class ExpressionHelper
    {
        public static string propertyName<TProp>(Expression<Func<TProp>> property)
        {
            MemberExpression memberExpr = property.Body as MemberExpression;
            return memberExpr.Member.Name;
        }

        public static string propertyName<TTarget, TProp>(Expression<Func<TTarget, TProp>> property)
        {
            MemberExpression memberExpr = property.Body as MemberExpression;
            return memberExpr.Member.Name;
        }
    }
}
