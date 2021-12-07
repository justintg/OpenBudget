using Microsoft.Extensions.DependencyInjection;
using OpenBudget.Application.PlatformServices;
using OpenBudget.Application.ViewModels;
using OpenBudget.Avalonia.ViewModels;
using System;

namespace OpenBudget.Avalonia.Services
{
    public sealed class AvaloniaServiceLocator : IServiceLocator
    {
        //Lazy Singleton
        //Todo: Make this singleton thread-safe
        private static AvaloniaServiceLocator defaultInstance;

        public static AvaloniaServiceLocator Default => defaultInstance ?? (defaultInstance = new AvaloniaServiceLocator());

        private IServiceCollection _serviceCollection;
        private IServiceProvider _serviceProvider;

        private AvaloniaServiceLocator()
        {
            _serviceCollection = new ServiceCollection();
            _serviceCollection.AddSingleton<IServiceLocator>(this);
            _serviceCollection.AddSingleton<IDialogService, AvaloniaDialogService>();
            _serviceCollection.AddSingleton<IBudgetLoader, AvaloniaBudgetLoader>();
            _serviceCollection.AddSingleton<ISettingsProvider, AvaloniaSettingsProvider>();
            _serviceCollection.AddSingleton<MainViewModel, DesktopMainViewModel>();
            _serviceCollection.AddSingleton<MainBudgetViewModel>();

            _serviceProvider = _serviceCollection.BuildServiceProvider();
        }

        public TInterface GetInstance<TInterface>()
        {
            return _serviceProvider.GetService<TInterface>();
        }
    }
}
