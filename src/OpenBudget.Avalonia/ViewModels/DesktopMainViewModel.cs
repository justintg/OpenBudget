using OpenBudget.Application.PlatformServices;
using OpenBudget.Application.ViewModels;

namespace OpenBudget.Avalonia.ViewModels
{
    public class DesktopMainViewModel : MainViewModel
    {
        public DesktopMainViewModel(
            IServiceLocator serviceLocator,
            IBudgetLoader budgetLoader,
            ISettingsProvider settingsProvider,
            INavigationService navigationService,
            IDialogService dialogService)
            : base(serviceLocator, budgetLoader, settingsProvider, navigationService, dialogService)
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
