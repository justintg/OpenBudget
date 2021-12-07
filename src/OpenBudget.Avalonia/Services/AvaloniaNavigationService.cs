using OpenBudget.Application.PlatformServices;
using OpenBudget.Application.ViewModels;
using OpenBudget.Avalonia.ViewModels;
using System.Collections.Generic;

namespace OpenBudget.Avalonia.Services
{
    public class AvaloniaNavigationService : INavigationService
    {
        private IServiceLocator _serviceLocator;
        private IDialogService _dialogService;

        private Stack<ClosableViewModel> _backStack = new Stack<ClosableViewModel>();

        public AvaloniaNavigationService(IServiceLocator serviceLocator, IDialogService dialogService)
        {
            _serviceLocator = serviceLocator;
            _dialogService = dialogService;
        }

        public bool CanGoBack()
        {
            return _backStack.Count > 0;
        }

        public void NavigateBack()
        {
            var backViewModel = _backStack.Pop();
            if (AvaloniaDialogService.GetRegisteredTypes().Contains(backViewModel.GetType()))
            {
                backViewModel.CloseCommand.Execute(null);
            }
            else
            {
                var mainViewModel = _serviceLocator.GetInstance<DesktopMainViewModel>();
                mainViewModel.NavigateTo(backViewModel);
            }
        }

        public void NavigateTo<T>(bool storeBack = false) where T : ClosableViewModel
        {
            var viewModel = _serviceLocator.GetInstance<T>();
            var mainViewModel = _serviceLocator.GetInstance<DesktopMainViewModel>();

            if (!storeBack)
            {
                _backStack.Clear();
            }

            if (AvaloniaDialogService.GetRegisteredTypes().Contains(typeof(T)))
            {
                if (storeBack)
                {
                    _backStack.Push(viewModel);
                }
                _dialogService.ShowDialog(viewModel);
            }
            else
            {
                if (storeBack)
                {
                    _backStack.Push(mainViewModel.CurrentWorkspace);
                }
                mainViewModel.NavigateTo(viewModel);
            }
        }
    }
}
