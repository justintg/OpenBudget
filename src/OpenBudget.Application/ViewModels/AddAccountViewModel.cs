using OpenBudget.Application.PlatformServices;
using OpenBudget.Model.Entities;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Text;
using OpenBudget.Model.Infrastructure;
using System.Linq;
using GalaSoft.MvvmLight;
using System.Collections.ObjectModel;

namespace OpenBudget.Application.ViewModels
{
    public class BudgetingTypeItem : ViewModelBase
    {
        private BudgetingTypes _value;

        public BudgetingTypes Value
        {
            get { return _value; }
            private set { _value = value; RaisePropertyChanged(); }
        }

        private string _label;

        public string Label
        {
            get { return _label; }
            private set { _label = value; RaisePropertyChanged(); }
        }

        public BudgetingTypeItem(BudgetingTypes value)
        {
            _value = value;
            _label = LabelAttribute.GetLabel(value);
        }
    }

    public class AccountTypeItem : ViewModelBase
    {
        private AccountTypes _value;

        public AccountTypes Value
        {
            get { return _value; }
            set { _value = value; RaisePropertyChanged(); }
        }

        private string _label;

        public string Label
        {
            get { return _label; }
            set { _label = value; RaisePropertyChanged(); }
        }

        private BudgetingTypes _defaultBudgetingType;

        public BudgetingTypes DefaultBudgetingType
        {
            get { return _defaultBudgetingType; }
            set { _defaultBudgetingType = value; RaisePropertyChanged(); }
        }


        public AccountTypeItem(AccountTypes value)
        {
            _value = value;
            _label = LabelAttribute.GetLabel(value);
            _defaultBudgetingType = DefaultBudgetingAttribute.GetDefaultBudgetingType(value);
        }
    }

    public class AddAccountViewModel : ClosableViewModel
    {
        private MainViewModel _mainViewModel;
        private INavigationService _navigationService;

        public AddAccountViewModel(MainViewModel mainViewModel, INavigationService navigationService)
        {
            _mainViewModel = mainViewModel;
            _navigationService = navigationService;

            InitializeRelayCommands();

            Account = new Account();

            InitializeAccountTypes();
        }

        private void InitializeAccountTypes()
        {
            IEnumerable<AccountTypeItem> accountTypes = Enum.GetValues(typeof(AccountTypes))
                .Cast<AccountTypes>()
                .Select(v => new AccountTypeItem(v));

            IEnumerable<BudgetingTypeItem> budgetingTypes = Enum.GetValues(typeof(BudgetingTypes))
                .Cast<BudgetingTypes>()
                .Where(v => v != BudgetingTypes.None)
                .Select(v => new BudgetingTypeItem(v));

            AccountTypesList = new ObservableCollection<AccountTypeItem>(accountTypes);
            BudgetingTypesList = new ObservableCollection<BudgetingTypeItem>(budgetingTypes);
            SelectedAccountType = AccountTypesList[0];
        }

        private void InitializeRelayCommands()
        {
            GoBackCommand = new RelayCommand(GoBack, CanGoBack);
            AddAccountCommand = new RelayCommand(AddAccount);
        }

        private Account _account;

        public Account Account
        {
            get { return _account; }
            set { _account = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<AccountTypeItem> _accountTypesList;

        public ObservableCollection<AccountTypeItem> AccountTypesList
        {
            get { return _accountTypesList; }
            set { _accountTypesList = value; RaisePropertyChanged(); }
        }

        private AccountTypeItem _selectedAccountType;

        public AccountTypeItem SelectedAccountType
        {
            get { return _selectedAccountType; }
            set
            {
                _selectedAccountType = value;
                var defaultBudgetingType = BudgetingTypesList.Where(b => b.Value == _selectedAccountType.DefaultBudgetingType).Single();
                SelectedBudgetingType = defaultBudgetingType;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<BudgetingTypeItem> _budgetingTypesList;

        public ObservableCollection<BudgetingTypeItem> BudgetingTypesList
        {
            get { return _budgetingTypesList; }
            set { _budgetingTypesList = value; RaisePropertyChanged(); }
        }

        private BudgetingTypeItem _selectedBudgetingType;

        public BudgetingTypeItem SelectedBudgetingType
        {
            get { return _selectedBudgetingType; }
            set { _selectedBudgetingType = value; RaisePropertyChanged(); }
        }

        private decimal _initialBalance;

        public decimal InitialBalance
        {
            get { return _initialBalance; }
            set { _initialBalance = value; RaisePropertyChanged(); }
        }


        public RelayCommand AddAccountCommand { get; private set; }

        public void AddAccount()
        {
            var budgetModel = _mainViewModel.BudgetModel;

            if (_initialBalance != 0)
            {
                Transaction initialTransaction = new Transaction();
                initialTransaction.Amount = _initialBalance;
                initialTransaction.TransactionDate = DateTime.Today;
                initialTransaction.IncomeCategory = budgetModel.Budget.IncomeCategories.GetIncomeCategory(initialTransaction.TransactionDate);
                initialTransaction.Payee = new Payee() { Name = "Initial Balance" };

                Account.Transactions.Add(initialTransaction);
            }

            Account.AccountType = SelectedAccountType.Value;
            Account.BudgetingType = SelectedBudgetingType.Value;

            budgetModel.Budget.Accounts.Add(_account);
            budgetModel.SaveChanges();

            _navigationService.NavigateBack();
        }

        public RelayCommand GoBackCommand { get; private set; }

        private bool CanGoBack()
        {
            return _navigationService.CanGoBack();
        }

        private void GoBack()
        {
            _navigationService.NavigateBack();
        }


    }
}
