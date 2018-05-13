using OpenBudget.Application.PlatformServices;
using OpenBudget.Application.ViewModels.TransactionGrid;
using OpenBudget.Model;
using OpenBudget.Model.Entities;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.ViewModels
{
    public class MainBudgetViewModel : ClosableViewModel
    {
        private MainViewModel _mainViewModel;
        private INavigationService _navigationService;
        private IServiceLocator _serviceLocator;

        public MainBudgetViewModel(MainViewModel mainViewModel, INavigationService navigationService, IServiceLocator serviceLocator)
        {
            _mainViewModel = mainViewModel;
            _navigationService = navigationService;
            _serviceLocator = serviceLocator;

            this.BudgetModel = mainViewModel.BudgetModel;
            _mainViewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "BudgetModel")
                {
                    this.BudgetModel = _mainViewModel.BudgetModel;
                }
            };
            InitialiseRelayCommands();
        }

        /// <summary>
        /// Initialise all relay command properties
        /// </summary>
        private void InitialiseRelayCommands()
        {
            this.AddAccountCommand = new RelayCommand(this.AddAccount);
            this.RenameAccountCommand = new RelayCommand(this.RenameAccount);
        }

        private ViewModelBase _currentScreen;

        public ViewModelBase CurrentScreen
        {
            get { return _currentScreen; }
            set
            {
                if (_currentScreen is IDisposable)
                {
                    (_currentScreen as IDisposable).Dispose();
                }
                _currentScreen = value; RaisePropertyChanged();
            }
        }

        private int _renameCount = 0;

        private Account _selectedAccount;

        public Account SelectedAccount
        {
            get { return _selectedAccount; }
            set
            {
                _selectedAccount = value;

                if (_selectedAccount != null)
                {
                    var transactionGrid = new TransactionGridViewModel(_selectedAccount);
                    CurrentScreen = transactionGrid;
                }

                RaisePropertyChanged();
            }
        }

        private BudgetModel _budgetModel;

        public BudgetModel BudgetModel
        {
            get { return _budgetModel; }
            private set
            {
                _budgetModel = value;
                SelectedAccount = null;
                CurrentScreen = null;
                RaisePropertyChanged();
            }
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

        /// <summary>
        /// Gets the command to rename the selected Account.
        /// </summary>
        public RelayCommand RenameAccountCommand { get; private set; }

        private void RenameAccount()
        {
            if (this.SelectedAccount == null)
            {
                return;
            }

            // Read models wrap changes directly for us
            this.SelectedAccount.Name = $"Renamed Account ({_renameCount++})";
            this.BudgetModel.SaveChanges();
        }
    }
}
