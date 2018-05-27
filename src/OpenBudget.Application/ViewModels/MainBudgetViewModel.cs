using OpenBudget.Application.PlatformServices;
using OpenBudget.Application.ViewModels.TransactionGrid;
using OpenBudget.Model;
using OpenBudget.Model.Entities;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Text;
using GalaSoft.MvvmLight.Messaging;

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

            _menu = new BudgetMenuViewModel(_mainViewModel, _navigationService, (screen) => { CurrentScreen = screen; });
        }

        private BudgetMenuViewModel _menu;

        public BudgetMenuViewModel Menu
        {
            get { return _menu; }
            private set
            {
                _menu = value;
                RaisePropertyChanged();
            }
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
    }
}
