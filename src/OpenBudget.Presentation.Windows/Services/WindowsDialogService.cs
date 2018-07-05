namespace OpenBudget.Presentation.Windows.Services
{
    using MahApps.Metro.Controls;
    using MahApps.Metro.Controls.Dialogs;
    using OpenBudget.Application.PlatformServices;
    using OpenBudget.Application.ViewModels;
    using OpenBudget.Presentation.Windows.Views;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// The WPF implementation of the <see cref="System.Windows.IWindowService"/>.
    /// </summary>
    public class WindowsDialogService : IDialogService
    {
        static WindowsDialogService()
        {
            RegisterWindow<MainViewModel, MainWindow>();
            RegisterDialog<AddAccountViewModel, AddAccountView>();
        }

        /// <summary>
        /// A dictionary to hold window registrations for each ViewModel.
        /// </summary>
        private static Dictionary<Type, Type> registeredWindows = new Dictionary<Type, Type>();

        private static Dictionary<Type, Type> registedDialogs = new Dictionary<Type, Type>();

        /// <summary>
        /// A Set of ViewModel types that have been registered as windows.
        /// </summary>
        public static HashSet<Type> GetRegisteredTypes()
        {
            return new HashSet<Type>(registeredWindows.Keys.Concat(registedDialogs.Keys));
        }

        /// <summary>
        /// Registers a type of ViewModel to be shown in a Type of Window.
        /// </summary>
        /// <typeparam name="TViewModel">The type of the ViewModel</typeparam>
        /// <typeparam name="TWindow">The type of the Window.</typeparam>
        public static void RegisterWindow<TViewModel, TWindow>() where TViewModel : ClosableViewModel where TWindow : Window
        {
            if (registeredWindows.ContainsKey(typeof(TViewModel)))
            {
                throw new Exception("This ViewModel has already been registered");
            }

            registeredWindows.Add(typeof(TViewModel), typeof(TWindow));
        }

        public static void RegisterDialog<TViewModel, TView>() where TViewModel : ClosableViewModel where TView : Control
        {
            if (registedDialogs.ContainsKey(typeof(TViewModel)))
            {
                throw new Exception("This ViewModel has already been registered");
            }

            registedDialogs.Add(typeof(TViewModel), typeof(TView));
        }

        public void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Shows a message box with a message.
        /// </summary>
        /// <param name="message">The message</param>
        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }

        /// <summary>
        /// Shows a Window based on a ViewModel.
        /// </summary>
        /// <typeparam name="T">The type of the ViewModel, must subclass <see cref="ClosableViewModel"/></typeparam>
        /// <param name="viewModel">The viewModel to show a window for.</param>
        /// <returns>The <see cref="Window"/> that is created to show the ViewModel.</returns>
        public object ShowDialog<T>(T viewModel) where T : ClosableViewModel
        {
            if (registeredWindows.ContainsKey(typeof(T)))
            {
                return ShowDialogWindowImpl(viewModel);
            }
            else if (registedDialogs.ContainsKey(typeof(T)))
            {
                return ShowDialogMetroDialogImpl(viewModel);
            }

            throw new InvalidOperationException("The ViewModel passed to ShowDialog is not recognized, you must register the ViewModel type");
        }

        private object ShowDialogMetroDialogImpl<T>(T viewModel) where T : ClosableViewModel
        {
            Type windowType = registedDialogs[typeof(T)];
            Control dialogContent = (Control)Activator.CreateInstance(windowType);
            dialogContent.DataContext = viewModel;

            MetroWindow mainWindow = App.Current.MainWindow as MetroWindow;
            if (mainWindow == null) throw new InvalidOperationException("The MainWindow must be a MetroWindow");

            CustomDialog dialog = new CustomDialog();
            dialog.Content = dialogContent;
            dialog.Title = viewModel.Header;

            mainWindow.ShowMetroDialogAsync(dialog);

            EventHandler closeHandler = null;

            closeHandler = (sender, e) =>
            {
                mainWindow.HideMetroDialogAsync(dialog);
                viewModel.RequestClose -= closeHandler;
            };

            viewModel.RequestClose += closeHandler;

            return dialog;
        }

        private object ShowDialogWindowImpl<T>(T viewModel) where T : ClosableViewModel
        {
            if (!registeredWindows.ContainsKey(typeof(T)))
            {
                throw new Exception("This ViewModel has NOT been registered");
            }

            Type windowType = registeredWindows[typeof(T)];
            Window window = (Window)Activator.CreateInstance(windowType);

            var mainWindow = App.Current.MainWindow;
            if (mainWindow != null && mainWindow != window)
            {
                window.Owner = mainWindow;
            }

            window.DataContext = viewModel;

            // If the _windowClosing flag is not set then call the window close method
            bool viewModelClose = false;
            bool windowClose = false;

            viewModel.RequestClose += (sender, e) =>
            {
                viewModelClose = true;

                if (!windowClose)
                {
                    window.Close();
                }
            };

            window.Closing += (sender, e) =>
            {
                windowClose = true;

                // Call RequestClose on the ViewModel and block the thread until it runs it closing routines
                if (!viewModelClose)
                {
                    viewModel.CloseCommand.Execute(null);
                }
            };

            window.Show();

            return window;
        }
    }
}
