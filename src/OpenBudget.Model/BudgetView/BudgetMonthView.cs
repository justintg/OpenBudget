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

            CalculateValues();
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

        public decimal AvailableToBudget
        {
            get { return NotBudgetedPreviousMonth + OverspentPreviousMonth + IncomeThisMonth - BudgetedThisMonth; }
        }

        private void CalculateValues()
        {
            DateTime previousMonthStart = Date.AddMonths(-1).FirstDayOfMonth();
            decimal incomeThisMonth = 0M;
            decimal incomePreviousMonths = 0M;
            decimal budgetedThisMonth = 0M;
            decimal budgetedPreviousMonths = 0m;
            decimal notBudgetedPreviousMonth = 0m;
            decimal spentPreviousMonth = 0m;
            decimal spentOtherMonths = 0m;

            foreach (var transaction in _model.Budget.Accounts.SelectMany(a => a.Transactions))
            {
                if (transaction.TransactionType == TransactionTypes.Normal)
                {
                    if (transaction.IncomeCategory != null)
                    {
                        if (transaction.IncomeCategory == _incomeCategory)
                        {
                            incomeThisMonth += transaction.Amount;
                        }
                        else if (transaction.IncomeCategory.Month < Date)
                        {
                            incomePreviousMonths += transaction.Amount;
                        }
                    }
                    else if (transaction.TransactionCategory != null)
                    {
                        if (transaction.TransactionDate < Date && transaction.TransactionDate >= previousMonthStart)
                        {
                            spentPreviousMonth += transaction.Amount;
                        }
                        else if (transaction.TransactionDate < previousMonthStart)
                        {
                            spentOtherMonths += transaction.Amount;
                        }
                    }
                }
            }

            foreach (CategoryMonth categoryMonth in _model.Budget.MasterCategories.SelectMany(c => c.Categories).SelectMany(c => c.CategoryMonths.GetAllMaterialized()))
            {
                if (categoryMonth.Month == Date)
                {
                    budgetedThisMonth += categoryMonth.AmountBudgeted;
                }
                else if (categoryMonth.Month < Date)
                {
                    budgetedPreviousMonths += categoryMonth.AmountBudgeted;
                }
            }

            IncomeThisMonth = incomeThisMonth;
            BudgetedThisMonth = budgetedThisMonth;
            NotBudgetedPreviousMonth = incomePreviousMonths - budgetedPreviousMonths;
            RaisePropertyChanged(nameof(AvailableToBudget));
        }

        public void Dispose()
        {
            _masterCategories?.Dispose();
        }
    }
}
