using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using OpenBudget.Application.PlatformServices;
using OpenBudget.Application.ViewModels.BudgetEditor;
using OpenBudget.Application.ViewModels.TransactionGrid;
using OpenBudget.Model;
using OpenBudget.Model.Entities;
using OpenBudget.Util.Collections;
using System;
using System.ComponentModel;
using System.Linq;

namespace OpenBudget.Application.ViewModels
{
    public class BudgetMenuViewModel : ViewModelBase
    {
        private MainBudgetViewModel _mainViewModel;
        private Action<ViewModelBase> _showScreenCallback;
        private INavigationService _navigationService;
        private Budget _budget;

        public BudgetMenuViewModel(MainBudgetViewModel mainViewModel, INavigationService navigationService, Action<ViewModelBase> showScreenCallback)
        {
            _mainViewModel = mainViewModel;
            _showScreenCallback = showScreenCallback;
            _navigationService = navigationService;

            _budgetModel = _mainViewModel.BudgetModel;
            _budget = _budgetModel?.GetBudget();
            _budget?.Accounts?.EnsureCollectionLoaded();


            _mainViewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(MainBudgetViewModel.BudgetModel))
                {
                    this.BudgetModel = _mainViewModel.BudgetModel;
                }
            };

            InitializeAccountItems();
            InitializeRelayCommands();

            _budgetViewItem = new BudgetMenuItemViewModel(MenuItemTypes.BudgetView, "Budget", null);
        }

        public void SelectDefaultItem()
        {
            _selectedItemModel = null;
            SelectMenuItemCommand.Execute(_budgetViewItem);
        }

        private void InitializeRelayCommands()
        {
            AddAccountCommand = new RelayCommand(AddAccount);
            SelectMenuItemCommand = new RelayCommand<BudgetMenuItemViewModel>(SelectMenuItem);
            ToggleOnBudgetExpandedCommand = new RelayCommand(ToggleOnBudgetExpanded);
            ToggleOffBudgetExpandedCommand = new RelayCommand(ToggleOffBudgetExpanded);
        }

        private BudgetModel _budgetModel;

        public BudgetModel BudgetModel
        {
            get { return _budgetModel; }
            private set
            {
                _budgetModel = value;
                if (_budgetModel != null)
                {
                    _budget = _budgetModel.GetBudget();
                    _budget.Accounts.EnsureCollectionLoaded();
                    InitializeAccountItems();
                }
                RaisePropertyChanged();
            }
        }

        private void InitializeAccountItems()
        {
            if (_budgetModel == null) return;

            if (OnBudgetAccounts != null)
            {
                OnBudgetAccounts.CollectionChanged -= OnBudgetAccounts_CollectionChanged;
                OnBudgetAccounts.ItemPropertyChanged -= OnBudgetAccounts_ItemPropertyChanged;
                OnBudgetAccounts.Dispose();
            }

            if (OffBudgetAccounts != null)
            {
                OffBudgetAccounts.CollectionChanged -= OffBudgetAccounts_CollectionChanged;
                OffBudgetAccounts.ItemPropertyChanged -= OffBudgetAccounts_ItemPropertyChanged;
                OffBudgetAccounts.Dispose();
            }

            OnBudgetAccounts = new TransformingObservableCollection<Account, AccountMenuItemViewModel>(_budget.Accounts,
                (account) =>
                {
                    return new AccountMenuItemViewModel(account, account.Name);
                },
                a =>
                {
                    a.Dispose();
                }, a => a.BudgetingType == BudgetingTypes.OnBudget, null, true);

            OnBudgetAccounts.CollectionChanged += OnBudgetAccounts_CollectionChanged;
            OnBudgetAccounts.ItemPropertyChanged += OnBudgetAccounts_ItemPropertyChanged;

            OffBudgetAccounts = new TransformingObservableCollection<Account, AccountMenuItemViewModel>(_budget.Accounts,
                (account) =>
                {
                    return new AccountMenuItemViewModel(account, account.Name);
                },
                a =>
                {
                    a.Dispose();
                },
                a => a.BudgetingType == BudgetingTypes.OffBudget, null, true);

            OffBudgetAccounts.CollectionChanged += OffBudgetAccounts_CollectionChanged;
            OffBudgetAccounts.ItemPropertyChanged += OffBudgetAccounts_ItemPropertyChanged;
        }

        private void OffBudgetAccounts_ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AccountMenuItemViewModel.Balance))
            {
                RaisePropertyChanged(nameof(OffBudgetBalance));
            }
        }

        private void OffBudgetAccounts_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(OffBudgetBalance));
        }

        private void OnBudgetAccounts_ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AccountMenuItemViewModel.Balance))
            {
                RaisePropertyChanged(nameof(OnBudgetBalance));
            }
        }

        private void OnBudgetAccounts_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(OnBudgetBalance));
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
            if (_selectedItemModel == menuItem) return;
            if (_selectedItemModel != null)
            {
                _selectedItemModel.IsSelected = false;
            }

            _selectedItemModel = menuItem;
            _selectedItemModel.IsSelected = true;

            switch (menuItem.MenuItemType)
            {
                case MenuItemTypes.BudgetView:
                    var budgetEditor = new BudgetEditorViewModel(BudgetModel);
                    _showScreenCallback(budgetEditor);
                    break;
                case MenuItemTypes.Account:
                    Account account = menuItem.Payload as Account;
                    var transactionGrid = new TransactionGridViewModel(BudgetModel, account.EntityID);
                    _showScreenCallback(transactionGrid);
                    break;
            }
        }

        public decimal OnBudgetBalance
        {
            get
            {
                return OnBudgetAccounts.Sum(a => a.Balance);
            }
        }

        private bool _onBudgetExpanded = true;

        public bool OnBudgetExpanded
        {
            get { return _onBudgetExpanded; }
            private set { _onBudgetExpanded = value; RaisePropertyChanged(); }
        }

        public RelayCommand ToggleOnBudgetExpandedCommand { get; private set; }

        private void ToggleOnBudgetExpanded()
        {
            OnBudgetExpanded = !OnBudgetExpanded;
        }

        private TransformingObservableCollection<Account, AccountMenuItemViewModel> _onBudgetAccounts;

        public TransformingObservableCollection<Account, AccountMenuItemViewModel> OnBudgetAccounts
        {
            get { return _onBudgetAccounts; }
            private set { _onBudgetAccounts = value; RaisePropertyChanged(); }
        }

        public decimal OffBudgetBalance
        {
            get
            {
                return OffBudgetAccounts.Sum(a => a.Balance);
            }
        }

        private bool _offBudgetExpanded = true;

        public bool OffBudgetExpanded
        {
            get { return _offBudgetExpanded; }
            private set { _offBudgetExpanded = value; RaisePropertyChanged(); }
        }

        public RelayCommand ToggleOffBudgetExpandedCommand { get; private set; }

        private void ToggleOffBudgetExpanded()
        {
            OffBudgetExpanded = !OffBudgetExpanded;
        }

        private TransformingObservableCollection<Account, AccountMenuItemViewModel> _offBudgetAccounts;

        public TransformingObservableCollection<Account, AccountMenuItemViewModel> OffBudgetAccounts
        {
            get { return _offBudgetAccounts; }
            private set { _offBudgetAccounts = value; RaisePropertyChanged(); }
        }


        private BudgetMenuItemViewModel _budgetViewItem;

        public BudgetMenuItemViewModel BudgetViewItem
        {
            get { return _budgetViewItem; }
            private set { _budgetViewItem = value; }
        }

    }
}
