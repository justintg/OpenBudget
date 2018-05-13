using OpenBudget.Model.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Infrastructure
{
    public class BudgetMessageBus : Messenger<ModelEvent>
    {
        public BudgetMessageBus()
        {
        }
    }
}
