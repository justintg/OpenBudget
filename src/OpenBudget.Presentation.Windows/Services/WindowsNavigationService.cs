using OpenBudget.Application.PlatformServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenBudget.Application.ViewModels;
using OpenBudget.Presentation.Windows.ViewModels;

namespace OpenBudget.Presentation.Windows.Services
{
    public class WindowsNavigationService : INavigationService
    {
        private IServiceLocator _serviceLocator;
        private IDialogService _dialogService;

        private Stack<ClosableViewModel> _backStack = new Stack<ClosableViewModel>();

        public WindowsNavigationService(IServiceLocator serviceLocator, IDialogService dialogService)
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
            if (WindowsDialogService.GetRegisteredTypes().Contains(backViewModel.GetType()))
            {
                backViewModel.CloseCommand.Execute(null);
            }
            else
            {
                var mainViewModel = _serviceLocator.GetInstance<WindowsMainViewModel>();
                mainViewModel.NavigateTo(backViewModel);
            }
        }

        public void NavigateTo<T>(bool storeBack = false) where T : ClosableViewModel
        {
            var viewModel = _serviceLocator.GetInstance<T>();
            var mainViewModel = _serviceLocator.GetInstance<WindowsMainViewModel>();

            if (!storeBack)
            {
                _backStack.Clear();
            }

            if (WindowsDialogService.GetRegisteredTypes().Contains(typeof(T)))
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
