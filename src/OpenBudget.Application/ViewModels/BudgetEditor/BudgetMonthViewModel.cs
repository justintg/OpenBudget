using GalaSoft.MvvmLight;
using OpenBudget.Model.BudgetView;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.ViewModels.BudgetEditor
{
    public class BudgetMonthViewModel : ViewModelBase, IDisposable
    {
        public BudgetMonthView BudgetMonthView { get; private set; }

        public BudgetMonthViewModel(BudgetMonthView budgetMonthView)
        {
            BudgetMonthView = budgetMonthView;
        }

        private bool _isFirstVisibleMonth;

        public bool IsFirstVisibleMonth
        {
            get { return _isFirstVisibleMonth; }
            set { _isFirstVisibleMonth = value; RaisePropertyChanged(); }
        }

        public void Dispose()
        {
            BudgetMonthView?.Dispose();
        }
    }
}
