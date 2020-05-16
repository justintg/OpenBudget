using OpenBudget.Application.Model;
using OpenBudget.Application.PlatformServices;
using OpenBudget.Application.Settings;
using OpenBudget.Model;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using OpenBudget.Model.Entities;

namespace OpenBudget.Application.ViewModels
{
    public class WelcomeViewModel : ClosableViewModel
    {
        private MainViewModel _mainViewModel;
        private INavigationService _navigationService;
        private ISettingsProvider _settingsProvider;
        private IBudgetLoader _budgetLoader;
        private IDialogService _dialogService;

        public WelcomeViewModel(
            MainViewModel mainViewModel,
            INavigationService navigationService,
            ISettingsProvider settingsProvider,
            IBudgetLoader budgetLoader,
            IDialogService dialogService)
        {
            _mainViewModel = mainViewModel;
            _navigationService = navigationService;
            _settingsProvider = settingsProvider;
            _budgetLoader = budgetLoader;
            _dialogService = dialogService;

            InitializeRelayCommands();

            var deviceSettings = _settingsProvider.Get<Device>();
            LastBudget = deviceSettings.LastBudget;
            RecentBudgets = new ObservableCollection<BudgetStub>(deviceSettings.RecentBudgets);
        }

        private BudgetStub _lastBudget;

        public BudgetStub LastBudget
        {
            get { return _lastBudget; }
            private set { _lastBudget = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<BudgetStub> _recentBudgets;

        public ObservableCollection<BudgetStub> RecentBudgets
        {
            get { return _recentBudgets; }
            private set { _recentBudgets = value; RaisePropertyChanged(); }
        }

        private void InitializeRelayCommands()
        {
            NewBudgetCommand = new RelayCommand(NewBudget);
            OpenBudgetCommand = new RelayCommand(OpenBudget);
            BackCommand = new RelayCommand(Back, CanBack);
            OpenRecentBudgetCommand = new RelayCommand<BudgetStub>(OpenRecentBudget);
        }

        public RelayCommand NewBudgetCommand { get; private set; }

        private void NewBudget()
        {
            _navigationService.NavigateTo<CreateBudgetViewModel>(true);
        }

        public RelayCommand OpenBudgetCommand { get; private set; }

        private void OpenBudget()
        {
            var budgetPath = _budgetLoader.GetBudgetOpenPath();
            if (budgetPath == null) return;

            ValidBudgetCheck validBudgetCheck = _budgetLoader.IsBudgetValid(budgetPath);
            if (!validBudgetCheck.IsValid)
            {
                _dialogService.ShowError(validBudgetCheck.Error);
                return;
            }

            try
            {
                BudgetModel budgetModel = _budgetLoader.LoadBudget(budgetPath);
                Budget budget = budgetModel.GetBudget();
                BudgetStub stub = new BudgetStub() { BudgetName = budget.Name, BudgetPath = budgetPath, LastEdited = DateTime.Now };

                var deviceSettings = _settingsProvider.Get<Device>();
                deviceSettings.AddRecentBudgetToTop(stub);
                deviceSettings.Save();

                _mainViewModel.MountBudget(budgetModel);
                _navigationService.NavigateTo<MainBudgetViewModel>();
            }
            catch (Exception)
            {
                _dialogService.ShowError($"There was an error opening the budget {budgetPath}. It may be corrupt.");
            }
        }

        public RelayCommand<BudgetStub> OpenRecentBudgetCommand { get; private set; }

        private void OpenRecentBudget(BudgetStub budgetStub)
        {
            if (budgetStub == null) return;

            ValidBudgetCheck validBudgetCheck = _budgetLoader.IsBudgetValid(budgetStub.BudgetPath);
            if (!validBudgetCheck.IsValid)
            {
                _dialogService.ShowError(validBudgetCheck.Error);
                return;
            }

            try
            {
                BudgetModel budget = _budgetLoader.LoadBudget(budgetStub.BudgetPath);
                var deviceSettings = _settingsProvider.Get<Device>();
                budgetStub.LastEdited = DateTime.Now;
                deviceSettings.AddRecentBudgetToTop(budgetStub);
                deviceSettings.Save();

                _mainViewModel.MountBudget(budget);
                _navigationService.NavigateTo<MainBudgetViewModel>();
            }
            catch (Exception)
            {
                _dialogService.ShowError($"There was an error opening the budget {budgetStub.BudgetPath}. It may be corrupt.");
            }
        }

        public RelayCommand BackCommand { get; private set; }

        public bool CanBack()
        {
            return _navigationService.CanGoBack();
        }

        public void Back()
        {
            _navigationService.NavigateBack();
        }
    }
}
