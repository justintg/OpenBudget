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
    public class AccountTypeItem : ViewModelBase
    {
        private AccountBudgetTypes _value;

        public AccountBudgetTypes Value
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

        public AccountTypeItem(AccountBudgetTypes value, string label)
        {
            _value = value;
            _label = label;
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
            IEnumerable<AccountTypeItem> accountTypes = Enum.GetValues(typeof(AccountBudgetTypes))
                .Cast<AccountBudgetTypes>()
                .Where(v => v != AccountBudgetTypes.None)
                .Select(v => new AccountTypeItem(v, LabelAttribute.GetLabel(v)));

            _accountTypes = new ObservableCollection<AccountTypeItem>(accountTypes);
            _selectedAccountType = _accountTypes[0];
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

        private ObservableCollection<AccountTypeItem> _accountTypes;

        public ObservableCollection<AccountTypeItem> AccountTypes
        {
            get { return _accountTypes; }
            set { _accountTypes = value; RaisePropertyChanged(); }
        }

        private AccountTypeItem _selectedAccountType;

        public AccountTypeItem SelectedAccountType
        {
            get { return _selectedAccountType; }
            set { _selectedAccountType = value; RaisePropertyChanged(); }
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
