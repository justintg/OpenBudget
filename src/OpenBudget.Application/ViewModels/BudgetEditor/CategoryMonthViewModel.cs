using GalaSoft.MvvmLight;
using OpenBudget.Model.BudgetView;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.ViewModels.BudgetEditor
{
    public class CategoryMonthViewModel : ViewModelBase, IDisposable
    {
        public CategoryMonthView CategoryMonthView { get; private set; }
        public BudgetMonthViewModel BudgetMonthViewModel { get; private set; }

        public CategoryMonthViewModel(BudgetMonthViewModel budgetMonthViewModel, CategoryMonthView monthView)
        {
            BudgetMonthViewModel = budgetMonthViewModel;
            CategoryMonthView = monthView;
            CategoryMonthView.PropertyChanged += MonthView_PropertyChanged;
        }

        private void MonthView_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OpenBudget.Model.BudgetView.CategoryMonthView.AmountBudgeted))
            {
                base.RaisePropertyChanged(nameof(AmountBudgeted));
            }
        }

        public void Dispose()
        {
            CategoryMonthView.PropertyChanged -= MonthView_PropertyChanged;
        }

        public decimal AmountBudgeted
        {
            get { return CategoryMonthView.AmountBudgeted; }
            set { CategoryMonthView.CategoryMonth.AmountBudgeted = value; CategoryMonthView.CategoryMonth.Model.SaveChanges(); }
        }

    }
}
