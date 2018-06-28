using GalaSoft.MvvmLight;
using OpenBudget.Model.BudgetView;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.ViewModels.BudgetEditor
{
    public class CategoryMonthViewModel : ViewModelBase, IDisposable
    {
        public CategoryMonthView MonthView { get; private set; }

        public CategoryMonthViewModel(CategoryMonthView monthView)
        {
            MonthView = monthView;
            MonthView.PropertyChanged += MonthView_PropertyChanged;
        }

        private void MonthView_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CategoryMonthView.AmountBudgeted))
            {
                RaisePropertyChanged(nameof(AmountBudgeted));
            }
        }

        public void Dispose()
        {
            MonthView.PropertyChanged -= MonthView_PropertyChanged;
        }

        public decimal AmountBudgeted
        {
            get { return MonthView.AmountBudgeted; }
            set { MonthView.CategoryMonth.AmountBudgeted = value; MonthView.CategoryMonth.Model.SaveChanges(); }
        }

    }
}
