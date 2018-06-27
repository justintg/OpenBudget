using OpenBudget.Model.Entities;
using OpenBudget.Model.Util;
using OpenBudget.Util.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenBudget.Model.BudgetView
{
    public class MasterCategoryMonthView : PropertyChangedBase, IDisposable
    {
        private MasterCategory _category;
        private DateTime _date;

        public MasterCategoryMonthView(MasterCategory category, DateTime date)
        {
            _category = category;
            _date = date.FirstDayOfMonth();

            _categories = new TransformingObservableCollection<Category, CategoryMonthView>(_category.Categories,
                sc =>
                {
                    return new CategoryMonthView(sc, _date);
                },
                scv =>
                {
                    scv.Dispose();
                }, null, null, true);

            _categories.ItemPropertyChanged += Categories_ItemPropertyChanged;

            CalculateValues();
        }

        private void CalculateValues()
        {
            BeginningBalance = _categories.Sum(c => c.BeginningBalance);
            EndBalance = _categories.Sum(c => c.EndBalance);
            TransactionsInMonth = _categories.Sum(c => c.TransactionsInMonth);
            AmountBudgeted = _categories.Sum(c => c.AmountBudgeted);
        }

        private void Categories_ItemPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            CalculateValues();
        }

        private decimal _beginningBalance;

        public decimal BeginningBalance
        {
            get { return _beginningBalance; }
            private set { _beginningBalance = value; RaisePropertyChanged(); }
        }

        private decimal _endBalance;

        public decimal EndBalance
        {
            get { return _endBalance; }
            private set { _endBalance = value; RaisePropertyChanged(); }
        }

        private decimal _amountBudgeted;

        public decimal AmountBudgeted
        {
            get { return _amountBudgeted; }
            private set { _amountBudgeted = value; RaisePropertyChanged(); }
        }

        private decimal _transactionsInMonth;

        public decimal TransactionsInMonth
        {
            get { return _transactionsInMonth; }
            private set { _transactionsInMonth = value; RaisePropertyChanged(); }
        }

        private TransformingObservableCollection<Category, CategoryMonthView> _categories;

        public TransformingObservableCollection<Category, CategoryMonthView> Categories
        {
            get { return _categories; }
            private set { _categories = value; RaisePropertyChanged(); }
        }


        public void Dispose()
        {
            _categories.ItemPropertyChanged -= Categories_ItemPropertyChanged;
            _categories.Dispose();
        }
    }
}
