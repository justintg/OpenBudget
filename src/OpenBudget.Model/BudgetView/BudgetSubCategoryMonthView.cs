using OpenBudget.Model.Entities;
using OpenBudget.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenBudget.Model.BudgetView
{
    public class BudgetSubCategoryMonthView : PropertyChangedBase, IDisposable
    {
        private BudgetModel _model;
        private BudgetSubCategory _subCategory;
        private DateTime _firstDayOfMonth;
        private DateTime _lastDayOfMonth;
        private BudgetCategoryMonth _categoryMonth;

        public BudgetSubCategoryMonthView(BudgetSubCategory subCategory, DateTime date)
        {
            if (subCategory == null) throw new ArgumentNullException(nameof(subCategory));

            _subCategory = subCategory;
            _model = _subCategory.Model;
            _firstDayOfMonth = date.FirstDayOfMonth().Date;
            _lastDayOfMonth = _firstDayOfMonth.LastDayOfMonth().Date;
            _categoryMonth = _subCategory.CategoryMonths.GetCategoryMonth(_firstDayOfMonth);
            _categoryMonth.PropertyChanged += CategoryMonth_PropertyChanged;
            CalculateValues();
        }

        private void CategoryMonth_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BudgetCategoryMonth.AmountBudgeted))
            {
                RaisePropertyChanged(nameof(AmountBudgeted));
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
            get { return _categoryMonth.AmountBudgeted; }
        }

        private decimal _transactionsInMonth;

        public decimal TransactionsInMonth
        {
            get { return _transactionsInMonth; }
            set { _transactionsInMonth = value; RaisePropertyChanged(); }
        }


        private void CalculateValues()
        {
            decimal beginningBalance = 0m;
            decimal endBalance = 0m;
            decimal previousMonthsBudgeted = 0m;
            decimal amountBudgeted = AmountBudgeted;
            decimal transactionsInMonth = 0m;

            foreach (var categoryMonth in _subCategory.CategoryMonths.GetAllMaterialized().Where(cm => cm.Month < _firstDayOfMonth.Date))
            {
                previousMonthsBudgeted += categoryMonth.AmountBudgeted;
            }

            foreach (var transaction in _model.Budget.Accounts.SelectMany(a => a.Transactions))
            {
                if (transaction.TransactionDate.Date > _lastDayOfMonth) continue;
                if (transaction.TransactionType == TransactionTypes.Normal)
                {
                    if (transaction.TransactionCategory != null && transaction.TransactionCategory.EntityID == _subCategory.EntityID)
                    {
                        if (transaction.TransactionDate.Date < _firstDayOfMonth)
                        {
                            beginningBalance += transaction.Amount;
                        }
                        else
                        {
                            transactionsInMonth += transaction.Amount;
                        }
                    }
                }
                else if (transaction.TransactionType == TransactionTypes.SplitTransaction)
                {
                    foreach (var subTransaction in transaction.SubTransactions)
                    {
                        if (subTransaction.TransactionCategory != null && transaction.TransactionCategory.EntityID == _subCategory.EntityID)
                        {
                            if (transaction.TransactionDate.Date < _firstDayOfMonth)
                            {
                                beginningBalance += subTransaction.Amount;
                            }
                            else
                            {
                                transactionsInMonth += subTransaction.Amount;
                            }
                        }
                    }
                }
            }

            beginningBalance += previousMonthsBudgeted;
            endBalance = beginningBalance + amountBudgeted + transactionsInMonth;

            BeginningBalance = beginningBalance;
            EndBalance = endBalance;
            TransactionsInMonth = transactionsInMonth;
        }

        public void Dispose()
        {
            _categoryMonth.PropertyChanged -= CategoryMonth_PropertyChanged;
        }
    }
}
