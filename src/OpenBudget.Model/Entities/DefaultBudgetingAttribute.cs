using System;
using System.Linq;

namespace OpenBudget.Model.Entities
{
    [AttributeUsage(AttributeTargets.Field)]
    public class DefaultBudgetingAttribute : Attribute
    {
        public BudgetingTypes BudgetingType { get; private set; }

        public DefaultBudgetingAttribute(BudgetingTypes budgetingType)
        {
            BudgetingType = budgetingType;
        }

        public static BudgetingTypes GetDefaultBudgetingType(Enum value)
        {
            var type = value.GetType();
            var member = type.GetMember(value.ToString()).Single();
            var attr = member.GetCustomAttributes(typeof(DefaultBudgetingAttribute), false).Cast<DefaultBudgetingAttribute>().Single();

            return attr?.BudgetingType ?? default(BudgetingTypes);
        }
    }
}
