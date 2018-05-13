using OpenBudget.Application.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenBudget.Application.PlatformServices;
using OpenBudget.Presentation.Windows.Services;

namespace OpenBudget.Presentation.Windows.ViewModels
{
    public class WindowsMainViewModel : MainViewModel
    {
        public WindowsMainViewModel(
            IServiceLocator serviceLocator,
            IBudgetLoader budgetLoader,
            ISettingsProvider settingsProvider,
            INavigationService navigationService)
            : base(serviceLocator, budgetLoader, settingsProvider, navigationService)
        {
        }

        public void NavigateTo(ClosableViewModel viewModel)
        {
            CurrentWorkspace = viewModel;
        }

        private ClosableViewModel _currentWorkspace;

        public ClosableViewModel CurrentWorkspace
        {
            get { return _currentWorkspace; }
            private set
            {
                _currentWorkspace = value; RaisePropertyChanged();
            }
        }
    }
}
