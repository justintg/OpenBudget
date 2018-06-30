using OpenBudget.Model.Entities;
using OpenBudget.Model.Event;
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

        private HashSet<string> _categoryTransactions = new HashSet<string>();
        private IDisposable _eventSubscription;

        public CategoryMonthView(Category subCategory, DateTime date)
        {
            if (subCategory == null) throw new ArgumentNullException(nameof(subCategory));

            Category = subCategory;
            _model = Category.Model;
            _firstDayOfMonth = date.FirstDayOfMonth().Date;
            _lastDayOfMonth = _firstDayOfMonth.LastDayOfMonth().Date;
            CategoryMonth = Category.CategoryMonths.GetCategoryMonth(_firstDayOfMonth);
            CategoryMonth.PropertyChanged += CategoryMonth_PropertyChanged;
            CalculateValues();
            InitializeEventListeners();
        }

        private void InitializeEventListeners()
        {
            _eventSubscription = _model.MessageBus.EventPublished
                .Where(e => ShouldRecalculate(e)).Subscribe(e =>
                {
                    CalculateValues();
                });
        }

        private bool ShouldRecalculate(ModelEvent evt)
        {
            if (evt.EntityType == nameof(Transaction))
            {
                if (evt is EntityCreatedEvent createEvent)
                {
                    FieldChange fieldChange = null;
                    if (createEvent.Changes.TryGetValue(nameof(Transaction.Category), out fieldChange))
                    {
                        if (fieldChange.NewValue is EntityReference reference)
                        {
                            if (reference.EntityType == nameof(Entities.Category) && reference.EntityID == Category.EntityID)
                            {
                                return true;
                            }
                        }
                    }
                }
                else if (evt is EntityUpdatedEvent updateEvent)
                {
                    if (_categoryTransactions.Contains(evt.EntityID))
                    {
                        if (updateEvent.Changes.ContainsKey(nameof(Transaction.Amount)))
                        {
                            return true;
                        }
                    }
                    else if (updateEvent.Changes.ContainsKey(nameof(Transaction.Category)))
                    {
                        var fieldChange = updateEvent.Changes[nameof(Transaction.Category)];
                        if (fieldChange.NewValue is EntityReference reference)
                        {
                            if (reference.EntityType == nameof(Entities.Category) && reference.EntityID == Category.EntityID)
                            {
                                return true;
                            }
                        }
                    }
                }
                else if (evt is GroupedFieldChangeEvent)
                {

                }
            }
            else if (evt.EntityType == nameof(Entities.CategoryMonth))
            {

            }

            return false;
        }

        private void CategoryMonth_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Entities.CategoryMonth.AmountBudgeted))
            {
                RaisePropertyChanged(nameof(AmountBudgeted));
                CalculateValues();
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


        private void CalculateValues()
        {
            decimal beginningBalance = 0m;
            decimal endBalance = 0m;
            decimal previousMonthsBudgeted = 0m;
            decimal amountBudgeted = AmountBudgeted;
            decimal transactionsInMonth = 0m;

            _categoryTransactions.Clear();

            foreach (var categoryMonth in Category.CategoryMonths.GetAllMaterialized().Where(cm => cm.Month < _firstDayOfMonth.Date))
            {
                previousMonthsBudgeted += categoryMonth.AmountBudgeted;
            }

            foreach (var transaction in _model.Budget.Accounts.SelectMany(a => a.Transactions))
            {
                if (transaction.TransactionDate.Date > _lastDayOfMonth) continue;
                if (transaction.TransactionType == TransactionTypes.Normal)
                {
                    if (transaction.TransactionCategory != null && transaction.TransactionCategory.EntityID == Category.EntityID)
                    {
                        _categoryTransactions.Add(transaction.EntityID);
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
                        if (subTransaction.TransactionCategory != null && subTransaction.TransactionCategory.EntityID == Category.EntityID)
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
            CategoryMonth.PropertyChanged -= CategoryMonth_PropertyChanged;
            _eventSubscription?.Dispose();
        }
    }
}
