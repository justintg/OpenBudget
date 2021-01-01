using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure.Entities;
using OpenBudget.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace OpenBudget.Model.BudgetView
{
    public class CategoryMonthView : PropertyChangedBase, IDisposable
    {
        public Category Category { get; private set; }
        public CategoryMonth CategoryMonth { get; private set; }

        private readonly BudgetModel _model;
        private readonly DateTime _firstDayOfMonth;
        private readonly DateTime _lastDayOfMonth;

        private readonly IBudgetViewCache _cache;

        public CategoryMonthView(Category subCategory, DateTime date)
        {
            if (subCategory == null) throw new ArgumentNullException(nameof(subCategory));

            Category = subCategory;
            _model = Category.Model;
            _firstDayOfMonth = date.FirstDayOfMonth().Date;
            _lastDayOfMonth = _firstDayOfMonth.LastDayOfMonth().Date;
            _cache = _model.BudgetViewCache;
            CategoryMonth = Category.CategoryMonths.GetCategoryMonth(_firstDayOfMonth);
            CategoryMonth.PropertyChanged += CategoryMonth_PropertyChanged;

            RefreshValues();
            _cache.CacheUpdated += Cache_CacheUpdated;
        }

        private void Cache_CacheUpdated(object sender, EventArgs e)
        {
            RefreshValues();
        }

        private void CategoryMonth_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Entities.CategoryMonth.AmountBudgeted))
            {
                RaisePropertyChanged(nameof(AmountBudgeted));
                RefreshValues();
            }
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

        public decimal AmountBudgeted
        {
            get { return CategoryMonth.AmountBudgeted; }
        }

        private decimal _transactionsInMonth;

        public decimal TransactionsInMonth
        {
            get { return _transactionsInMonth; }
            private set { _transactionsInMonth = value; RaisePropertyChanged(); }
        }


        private void RefreshValues()
        {
            bool exactMatch;
            var categoryMonth = _model.BudgetViewCache.GetLastCategoryMonth(Category.EntityID, _firstDayOfMonth, out exactMatch);

            if (exactMatch)
            {
                BeginningBalance = categoryMonth.BeginningBalance;
                TransactionsInMonth = categoryMonth.TransactionsInMonth;
                EndBalance = categoryMonth.EndBalance;
            }
            else
            {
                if (categoryMonth == null)
                {
                    BeginningBalance = 0M;
                    TransactionsInMonth = 0M;
                    EndBalance = 0M;
                }
                else
                {
                    if (categoryMonth.EndBalance < 0M && categoryMonth.NegativeBalanceHandling == NegativeBalanceHandlingTypes.AvailableToBudget)
                    {
                        BeginningBalance = 0M;
                        TransactionsInMonth = 0M;
                        EndBalance = 0M;
                    }
                    else
                    {
                        BeginningBalance = categoryMonth.EndBalance;
                        TransactionsInMonth = 0M;
                        EndBalance = categoryMonth.EndBalance;
                    }
                }
            }
        }

        public void Dispose()
        {
            CategoryMonth.PropertyChanged -= CategoryMonth_PropertyChanged;
            _cache.CacheUpdated -= Cache_CacheUpdated;
        }
    }
}
