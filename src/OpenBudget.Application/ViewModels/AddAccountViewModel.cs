using OpenBudget.Application.PlatformServices;
using OpenBudget.Model.Entities;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.ViewModels
{
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

        public RelayCommand AddAccountCommand { get; private set; }

        public void AddAccount()
        {
            var budgetModel = _mainViewModel.BudgetModel;
            /*for (int i = 0; i < 10000; i++)
            {
                _account.Transactions.Add(new Transaction() { Amount = i, TransactionDate = DateTime.Today });
            }*/

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
