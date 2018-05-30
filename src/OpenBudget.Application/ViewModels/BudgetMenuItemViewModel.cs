using GalaSoft.MvvmLight;
using OpenBudget.Model.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace OpenBudget.Application.ViewModels
{
    public enum MenuItemTypes
    {
        BudgetView,
        Report,
        AllAccounts,
        Account
    }

    public class BudgetMenuItemViewModel : ViewModelBase
    {
        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; RaisePropertyChanged(); }
        }

        private string _label;

        public string Label
        {
            get { return _label; }
            set { _label = value; RaisePropertyChanged(); }
        }

        public MenuItemTypes MenuItemType { get; private set; }

        public virtual object Payload { get; protected set; }

        public BudgetMenuItemViewModel(MenuItemTypes itemType, string label, object payload)
        {
            MenuItemType = itemType;
            _label = label;
            Payload = payload;
            _isSelected = false;
        }
    }

    public class AccountMenuItemViewModel : BudgetMenuItemViewModel, IDisposable
    {
        private Account _account;

        public Account Account
        {
            get
            {
                return _account;
            }
            private set
            {
                if (_account != null)
                {
                    _account.PropertyChanged -= Account_PropertyChanged;
                }
                _account = value;
                _account.PropertyChanged += Account_PropertyChanged;

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Balance));
            }
        }

        private void Account_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Account.Balance))
            {
                RaisePropertyChanged(nameof(Balance));
            }
        }

        public void Dispose()
        {
            if (_account != null)
            {
                _account.PropertyChanged -= Account_PropertyChanged;
            }
        }

        public decimal Balance
        {
            get { return Account.Balance; }
        }

        public AccountMenuItemViewModel(Account account, string label)
            : base(MenuItemTypes.Account, label, account)
        {

        }

        public override object Payload
        {
            get => Account;
            protected set
            {
                if (value is Account acct)
                {
                    Account = acct;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}
