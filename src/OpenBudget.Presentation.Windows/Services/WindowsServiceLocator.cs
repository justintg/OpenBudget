using OpenBudget.Application.PlatformServices;
using OpenBudget.Application.ViewModels;
using OpenBudget.Presentation.Windows.ViewModels;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Presentation.Windows.Services
{
    public sealed class WindowsServiceLocator : IServiceLocator, IDisposable
    {
        //Lazy Singleton
        //Todo: Make this singleton thread-safe
        private static WindowsServiceLocator defaultInstance;

        public static WindowsServiceLocator Default => defaultInstance ?? (defaultInstance = new WindowsServiceLocator());

        private readonly IKernel _container;

        private WindowsServiceLocator()
        {
            _container = new StandardKernel();

            _container.Bind<IServiceLocator>().ToConstant(this).InSingletonScope();
            _container.Bind<IDialogService>().To<WindowsDialogService>().InSingletonScope();
            _container.Bind<IBudgetLoader>().To<WindowsBudgetLoader>().InSingletonScope();
            _container.Bind<ISettingsProvider>().To<WindowsSettingsProvider>().InSingletonScope();
            _container.Bind<INavigationService>().To<WindowsNavigationService>().InSingletonScope();
            _container.Bind<WindowsMainViewModel, MainViewModel>().To<WindowsMainViewModel>().InSingletonScope();
        }

        public TInterface GetInstance<TInterface>()
        {
            return _container.Get<TInterface>();
        }

        public void RegisterInstance<TInterface>(TInterface instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            _container.Bind<TInterface>().ToConstant(instance).InSingletonScope();
        }

        public void Dispose()
        {
            _container.Dispose();
        }
    }
}
