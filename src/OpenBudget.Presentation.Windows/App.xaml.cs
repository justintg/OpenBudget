namespace OpenBudget.Presentation.Windows
{
    using System;
    using System.Windows;

    using OpenBudget.Application.ViewModels;
    using OpenBudget.Presentation.Windows.Services;

    using MainWindow = OpenBudget.Presentation.Windows.Views.MainWindow;
    using OpenBudget.Application.PlatformServices;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// The main ViewModel for the application.
        /// </summary>
        private MainViewModel mainViewModel;

        /// <summary>
        /// The main window of the application.
        /// </summary>
        private Window mainWindow;

        /// <summary>
        /// A method that is called when the application starts.
        /// </summary>
        /// <param name="e">The <see cref="StartupEventArgs"/>.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            WindowsServiceLocator serviceLocator = WindowsServiceLocator.Default;

            this.mainViewModel = serviceLocator.GetInstance<MainViewModel>();
            IDialogService windowService = serviceLocator.GetInstance<IDialogService>();

            this.mainWindow = (Window)windowService.ShowWindow(this.mainViewModel);
            this.mainWindow.Closed += this.MainWindow_Closed;

            this.MainWindow = mainWindow;

            this.mainViewModel.Initialize();
        }

        /// <summary>
        /// An event handler for the close event of the Main Window.
        /// </summary>
        /// <param name="sender">The originator of the event.</param>
        /// <param name="e">The event's arguments</param>
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
