using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.BudgetView.Calculator;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Util;
using OpenBudget.Util.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenBudget.Model.BudgetView
{
    public class BudgetMonthView : PropertyChangedBase, IDisposable
    {
        private BudgetModel _model;
        private IncomeCategory _incomeCategory;
        public DateTime Date { get; private set; }

        private DateTime _lastDayOfMonth;
        private IBudgetViewCache _cache;

        public BudgetMonthView(BudgetModel model, DateTime date)
        {
            _model = model;
            Date = date.FirstDayOfMonth();
            _lastDayOfMonth = Date.LastDayOfMonth();
            _incomeCategory = _model.Budget.IncomeCategories.GetIncomeCategory(Date);

            _masterCategories = new TransformingObservableCollection<MasterCategory, MasterCategoryMonthView>(
                _model.Budget.MasterCategories,
                mc => { return new MasterCategoryMonthView(mc, Date); },
                mcv => { mcv.Dispose(); });

            _cache = _model.BudgetViewCache;
            _cache.CacheUpdated += Cache_CacheUpdated;
            RefreshValues();
        }

        private void Cache_CacheUpdated(object sender, EventArgs e)
        {
            RefreshValues();
        }

        private TransformingObservableCollection<MasterCategory, MasterCategoryMonthView> _masterCategories;

        public TransformingObservableCollection<MasterCategory, MasterCategoryMonthView> MasterCategories
        {
            get { return _masterCategories; }
            private set { _masterCategories = value; RaisePropertyChanged(); }
        }

        private decimal _notBudgetedPreviousMonth;

        public decimal NotBudgetedPreviousMonth
        {
            get { return _notBudgetedPreviousMonth; }
            private set { _notBudgetedPreviousMonth = value; RaisePropertyChanged(); }
        }

        private decimal _overspentPreviousMonth;

        public decimal OverspentPreviousMonth
        {
            get { return _overspentPreviousMonth; }
            private set { _overspentPreviousMonth = value; RaisePropertyChanged(); }
        }

        private decimal _incomeThisMonth;

        public decimal IncomeThisMonth
        {
            get { return _incomeThisMonth; }
            private set { _incomeThisMonth = value; RaisePropertyChanged(); }
        }

        private decimal _budgetedThisMonth;

        public decimal BudgetedThisMonth
        {
            get { return _budgetedThisMonth; }
            private set { _budgetedThisMonth = value; RaisePropertyChanged(); }
        }

        private decimal _availableToBudget;

        public decimal AvailableToBudget
        {
            get { return _availableToBudget; }
            private set { _availableToBudget = value; RaisePropertyChanged(); }
        }

        private void RefreshValues()
        {
            bool exactMatch;
            var budgetViewMonth = _cache.GetLastBudgetViewMonth(this.Date, out exactMatch);

            if (exactMatch)
            {
                IncomeThisMonth = budgetViewMonth.Income;
                BudgetedThisMonth = budgetViewMonth.Budgeted;
                NotBudgetedPreviousMonth = budgetViewMonth.OverUnderBudgetedPreviousMonth;
                OverspentPreviousMonth = budgetViewMonth.OverspentPreviousMonth;
                AvailableToBudget = budgetViewMonth.AvailableToBudget;
            }
            else
            {
                if (budgetViewMonth != null)
                {
                    IncomeThisMonth = 0M;
                    BudgetedThisMonth = 0M;
                    OverspentPreviousMonth = 0M;
                    NotBudgetedPreviousMonth = budgetViewMonth.AvailableToBudget;
                    AvailableToBudget = budgetViewMonth.AvailableToBudget;
                }
                else
                {
                    IncomeThisMonth = 0M;
                    BudgetedThisMonth = 0M;
                    OverspentPreviousMonth = 0M;
                    NotBudgetedPreviousMonth = 0M;
                    AvailableToBudget = 0M;
                }
            }
        }

        public void Dispose()
        {
            _masterCategories?.Dispose();
            _cache.CacheUpdated -= Cache_CacheUpdated;
        }
    }
}
