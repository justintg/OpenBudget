using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OpenBudget.Application.ViewModels;
using OpenBudget.Avalonia.Services;
using OpenBudget.Avalonia.ViewModels;
using AvaloniaApplication = Avalonia.Application;

namespace OpenBudget.Avalonia
{
    public class App : AvaloniaApplication
    {
        public MainWindow MainWindow { get; private set; }
        public DesktopMainViewModel MainViewModel { get; private set; }
        public AvaloniaServiceLocator ServiceLocator { get; private set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            ServiceLocator = AvaloniaServiceLocator.Default;
            MainViewModel = (DesktopMainViewModel)ServiceLocator.GetInstance<MainViewModel>();
            MainViewModel.Initialize();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                MainWindow = new MainWindow();
                MainWindow.DataContext = MainViewModel;
                desktop.MainWindow = MainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
