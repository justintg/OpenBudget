using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
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

        public object Payload { get; private set; }

        public BudgetMenuItemViewModel(MenuItemTypes itemType, string label, object payload)
        {
            MenuItemType = itemType;
            _label = label;
            Payload = payload;
            _isSelected = false;
        }
    }
}
