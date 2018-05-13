using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.PlatformServices
{
    public interface IServiceLocator
    {
        TInterface GetInstance<TInterface>();

        void RegisterInstance<TInterface>(TInterface instance);

    }
}
