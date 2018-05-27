using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using OpenBudget.Application.Collections;
using OpenBudget.Application.PlatformServices;
using OpenBudget.Application.ViewModels.TransactionGrid;
using OpenBudget.Model;
using OpenBudget.Model.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.ViewModels
{
    public class BudgetMenuViewModel : ViewModelBase
    {
        private MainViewModel _mainViewModel;
        private Action<ViewModelBase> _showScreenCallback;
        private INavigationService _navigationService;

        public BudgetMenuViewModel(MainViewModel mainViewModel, INavigationService navigationService, Action<ViewModelBase> showScreenCallback)
        {
            _mainViewModel = mainViewModel;
            _showScreenCallback = showScreenCallback;
            _navigationService = navigationService;

            _budgetModel = _mainViewModel.BudgetModel;
            _mainViewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.BudgetModel))
                {
                    this.BudgetModel = _mainViewModel.BudgetModel;
                }
            };

            InitializeAccountItems();
            InitializeRelayCommands();

            _budgetViewItem = new BudgetMenuItemViewModel(MenuItemTypes.BudgetView, "Budget", null);
        }

        private void InitializeRelayCommands()
        {
            AddAccountCommand = new RelayCommand(AddAccount);
            SelectMenuItemCommand = new RelayCommand<BudgetMenuItemViewModel>(SelectMenuItem);
        }

        private BudgetModel _budgetModel;

        public BudgetModel BudgetModel
        {
            get { return _budgetModel; }
            private set
            {
                _budgetModel = value;
                InitializeAccountItems();
                RaisePropertyChanged();
            }
        }

        private void InitializeAccountItems()
        {
            if (_budgetModel == null) return;

            if (OnBudgetAccounts != null)
            {
                OnBudgetAccounts.Dispose();
            }

            OnBudgetAccounts = new TransformingObservableCollection<Account, BudgetMenuItemViewModel>(_budgetModel.Budget.Accounts,
                (account) =>
                {
                    return new BudgetMenuItemViewModel(MenuItemTypes.Account, account.Name, account);
                },
                a => { });
        }

        /// <summary>
        /// Gets the command to add an Account.
        /// </summary>
        public RelayCommand AddAccountCommand { get; private set; }

        /// <summary>
        /// Adds a new account.
        /// </summary>
        private void AddAccount()
        {
            _navigationService.NavigateTo<AddAccountViewModel>(true);
        }

        private BudgetMenuItemViewModel _selectedItemModel;

        public RelayCommand<BudgetMenuItemViewModel> SelectMenuItemCommand { get; private set; }

        private void SelectMenuItem(BudgetMenuItemViewModel menuItem)
        {
            if (_selectedItemModel != null)
            {
                _selectedItemModel.IsSelected = false;
            }

            _selectedItemModel = menuItem;
            _selectedItemModel.IsSelected = true;

            switch (menuItem.MenuItemType)
            {
                case MenuItemTypes.BudgetView:
                    break;
                case MenuItemTypes.Account:
                    Account account = menuItem.Payload as Account;
                    var transactionGrid = new TransactionGridViewModel(account);
                    _showScreenCallback(transactionGrid);
                    break;
            }
        }

        private TransformingObservableCollection<Account, BudgetMenuItemViewModel> _onBudgetAccounts;

        public TransformingObservableCollection<Account, BudgetMenuItemViewModel> OnBudgetAccounts
        {
            get { return _onBudgetAccounts; }
            private set { _onBudgetAccounts = value; RaisePropertyChanged(); }
        }


        private BudgetMenuItemViewModel _budgetViewItem;

        public BudgetMenuItemViewModel BudgetViewItem
        {
            get { return _budgetViewItem; }
            private set { _budgetViewItem = value; }
        }

    }
}
