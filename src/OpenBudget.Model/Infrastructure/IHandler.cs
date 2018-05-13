using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Infrastructure
{
    public interface IHandler<TMessage>
    {
        void Handle(TMessage message);
    }
}
