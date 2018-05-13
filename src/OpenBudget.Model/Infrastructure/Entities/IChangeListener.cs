using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Entities
{
    public interface IChangeListener
    {
        event EventHandler HasChanged;
        void attachTo(object obj);
        void detachFrom(object jb);
    }
}
