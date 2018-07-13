using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Entities
{
    [AttributeUsage(AttributeTargets.Field)]
    public class LabelAttribute : Attribute
    {
        public string Label { get; private set; }

        public LabelAttribute(string label)
        {
            this.Label = label;
        }

        public static string GetLabel(Enum value)
        {
            var type = value.GetType();
            var member = type.GetMember(value.ToString()).Single();
            var attr = member.GetCustomAttributes(typeof(LabelAttribute), false).Cast<LabelAttribute>().Single();

            return attr?.Label ?? value.ToString();
        }
    }
}
