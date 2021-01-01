using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.BudgetView.Model;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Infrastructure.Entities;
using OpenBudget.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CategoryResultsDictionary = System.Collections.Generic.Dictionary<OpenBudget.Model.BudgetView.Model.CategoryMonthKey, OpenBudget.Model.BudgetView.Model.BudgetViewCategoryMonth>;
using TransactionDictionary = System.Collections.Generic.Dictionary<OpenBudget.Model.BudgetView.Model.CategoryMonthKey, decimal>;

namespace OpenBudget.Model.BudgetView.Calculator
{
    public class BudgetViewCalculator
    {
        private BudgetModel _model;
        private readonly IBudgetStore _budgetStore;
        private readonly ISnapshotStore _snapshotStore;

        public BudgetViewCalculator(BudgetModel budgetModel, IBudgetStore budgetStore)
        {
            _model = budgetModel ?? throw new ArgumentNullException(nameof(budgetModel));
            _budgetStore = budgetStore ?? throw new ArgumentNullException(nameof(budgetStore));
            _snapshotStore = _budgetStore.SnapshotStore;
        }

        public BudgetViewCalculatorResult Calculate()
        {
            //Iterate transactions and group their balances into categories
            Dictionary<CategoryMonthKey, decimal> groupedTransactions = GroupTransactions();

            CategoryResultsDictionary resultDictionary = InitializeResults(groupedTransactions);
            Dictionary<CategoryKey, List<BudgetViewCategoryMonth>> categoryResultsOrdered = SortResultsByDate(resultDictionary);
            Dictionary<DateTime, List<BudgetViewCategoryMonth>> categoryResultsByMonth = GroupResultsByMonth(resultDictionary);
            CalculateCategoryBalances(categoryResultsOrdered);

            Dictionary<DateTime, BudgetViewMonth> months = InitializeMonths(categoryResultsByMonth, groupedTransactions);
            List<BudgetViewMonth> monthsOrdered = months.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToList();
            CalculateMonthBalances(monthsOrdered);


            return new BudgetViewCalculatorResult()
            {
                CategoryMonths = resultDictionary,
                CategoryMonthsOrdered = categoryResultsOrdered,
                CategoryMonthsByMonth = categoryResultsByMonth,
                Months = months,
                MonthsByDate = monthsOrdered
            };
        }

        private void CalculateMonthBalances(List<BudgetViewMonth> monthsOrdered)
        {
            BudgetViewMonth previousMonth = null;
            foreach (var currentMonth in monthsOrdered)
            {
                if (previousMonth != null)
                {
                    bool isLastMonth = currentMonth.Month.AddMonths(-1).FirstDayOfMonth() == previousMonth.Month;
                    currentMonth.OverUnderBudgetedPreviousMonth = previousMonth.AvailableToBudget;
                    if (!isLastMonth)
                    {
                        currentMonth.OverUnderBudgetedPreviousMonth -= previousMonth.OverspentPreviousMonth;
                    }
                }

                currentMonth.AvailableToBudget = currentMonth.Income - currentMonth.Budgeted + currentMonth.OverspentPreviousMonth + currentMonth.OverUnderBudgetedPreviousMonth;

                previousMonth = currentMonth;
            }
        }

        private Dictionary<DateTime, BudgetViewMonth> InitializeMonths(Dictionary<DateTime, List<BudgetViewCategoryMonth>> categoriesByMonth, TransactionDictionary groupedTransactions)
        {
            Dictionary<DateTime, BudgetViewMonth> months = groupedTransactions.Where(kvp => kvp.Key.EntityType == nameof(IncomeCategory)).ToDictionary(kvp => kvp.Key.FirstDayOfMonth, kvp => new BudgetViewMonth(kvp.Key.FirstDayOfMonth) { Income = kvp.Value });

            foreach (var category in categoriesByMonth)
            {
                BudgetViewMonth month = null;
                BudgetViewMonth nextMonth = null;
                DateTime nextMonthDate = category.Key.AddMonths(1).FirstDayOfMonth();
                months.TryGetValue(nextMonthDate, out nextMonth);

                if (!months.TryGetValue(category.Key, out month))
                {
                    month = new BudgetViewMonth(category.Key);
                    months[category.Key] = month;
                }

                foreach (var categoryMonth in category.Value)
                {
                    month.Budgeted += categoryMonth.AmountBudgeted;
                    if (categoryMonth.EndBalance < 0M && categoryMonth.NegativeBalanceHandling == NegativeBalanceHandlingTypes.AvailableToBudget)
                    {
                        if (nextMonth == null)
                        {
                            nextMonth = new BudgetViewMonth(nextMonthDate);
                            months[nextMonthDate] = nextMonth;
                        }
                        nextMonth.OverspentPreviousMonth += categoryMonth.EndBalance;
                    }
                }
            }

            return months;
        }

        private Dictionary<DateTime, List<BudgetViewCategoryMonth>> GroupResultsByMonth(CategoryResultsDictionary resultDictionary)
        {
            Dictionary<DateTime, List<BudgetViewCategoryMonth>> categoryResultsByMonth = new Dictionary<DateTime, List<BudgetViewCategoryMonth>>();
            var groups = resultDictionary.GroupBy(kvp => kvp.Key.FirstDayOfMonth);

            foreach (var group in groups)
            {
                categoryResultsByMonth[group.Key] = group.Select(g => g.Value).ToList();
            }

            return categoryResultsByMonth;
        }

        private Dictionary<CategoryKey, List<BudgetViewCategoryMonth>>
            SortResultsByDate(CategoryResultsDictionary resultDictionary)
        {
            Dictionary<CategoryKey, List<BudgetViewCategoryMonth>> categoryResultsOrdered = new Dictionary<CategoryKey, List<BudgetViewCategoryMonth>>();

            var groups = resultDictionary.GroupBy(kvp => new CategoryKey(kvp.Key));
            foreach (var group in groups)
            {
                categoryResultsOrdered[group.Key] = group.Select(g => g.Value).OrderBy(cm => cm.Month).ToList();
            }


            return categoryResultsOrdered;
        }

        private void CalculateCategoryBalances(Dictionary<CategoryKey, List<BudgetViewCategoryMonth>> categoryResultsByDate)
        {
            foreach (var category in categoryResultsByDate)
            {
                int count = 0;
                foreach (var result in category.Value)
                {
                    if (count > 0)
                    {
                        var previousMonth = category.Value[count - 1];
                        decimal previousEndBalance = previousMonth.EndBalance;

                        if (previousEndBalance < 0M &&
                            previousMonth.NegativeBalanceHandling == NegativeBalanceHandlingTypes.AvailableToBudget)
                        {
                            result.BeginningBalance = 0M;
                        }
                        else
                        {
                            result.BeginningBalance = previousEndBalance;
                        }
                    }

                    result.EndBalance = result.BeginningBalance + result.AmountBudgeted + result.TransactionsInMonth;

                    count++;
                }
            }
        }

        private CategoryResultsDictionary InitializeResults(TransactionDictionary groupedTransactions)
        {
            CategoryResultsDictionary results = AddTransactionsInMonth(groupedTransactions);
            PopulateCategoryMonthData(results);
            return results;
            //return results.ToDictionary(r => r.Key, r => r.Value.Values.OrderBy(rcm => rcm.Month).ToList());
        }

        private CategoryResultsDictionary AddTransactionsInMonth(TransactionDictionary groupedTransactions)
        {
            return groupedTransactions.ToDictionary(td => td.Key, td => new BudgetViewCategoryMonth(td.Key.EntityID, td.Key.FirstDayOfMonth) { TransactionsInMonth = td.Value });
        }

        private void PopulateCategoryMonthData(CategoryResultsDictionary results)
        {
            var categoryMonths = _snapshotStore.GetAllSnapshots<CategoryMonthSnapshot>();
            foreach (var categoryMonth in categoryMonths)
            {
                if (categoryMonth.AmountBudgeted == 0M && categoryMonth.NegativeBalanceHandling == null) continue;

                CategoryMonthKey monthKey = new CategoryMonthKey(categoryMonth.Parent, categoryMonth.Month);
                BudgetViewCategoryMonth monthResult = null;

                BudgetViewCategoryMonth monthValues = null;
                if (results.TryGetValue(monthKey, out monthValues))
                {
                    if (categoryMonth.AmountBudgeted != 0M)
                    {
                        monthValues.AmountBudgeted = GetCategoryMonthAmount(categoryMonth);
                    }

                    if (categoryMonth.NegativeBalanceHandling != null)
                    {
                        monthValues.NegativeBalanceHandlingIsExplicit = true;
                        monthValues.NegativeBalanceHandling = categoryMonth.NegativeBalanceHandling.Value;
                    }
                }
                else
                {
                    monthResult = new BudgetViewCategoryMonth(monthKey.EntityID, monthKey.FirstDayOfMonth);
                    if (categoryMonth.AmountBudgeted != 0M)
                    {
                        monthResult.AmountBudgeted = GetCategoryMonthAmount(categoryMonth);
                    }

                    if (categoryMonth.NegativeBalanceHandling != null)
                    {
                        monthResult.NegativeBalanceHandlingIsExplicit = true;
                        monthResult.NegativeBalanceHandling = categoryMonth.NegativeBalanceHandling.Value;
                    }
                    results[monthKey] = monthResult;
                }
            }
        }

        private decimal GetCategoryMonthAmount(CategoryMonthSnapshot snapshot)
        {
            return CurrencyConverter.ToDecimalValue(snapshot.AmountBudgeted, snapshot.AmountBudgeted_Denominator);
        }

        private decimal GetTransactionAmount(TransactionSnapshot snapshot)
        {
            return CurrencyConverter.ToDecimalValue(snapshot.Amount, snapshot.Amount_Denominator);
        }

        private decimal GetTransactionAmount(SubTransactionSnapshot snapshot)
        {
            return CurrencyConverter.ToDecimalValue(snapshot.Amount, snapshot.Amount_Denominator);
        }

        private TransactionDictionary GroupTransactions()
        {
            var groupedTransactions = new Dictionary<CategoryMonthKey, decimal>();
            var transacations = _snapshotStore.GetAllSnapshots<TransactionSnapshot>().Where(t => !t.IsDeleted).ToList();
            var transactionReferences = transacations.Select(t => new EntityReference(nameof(Transaction), t.EntityID)).ToList();
            var subTransactionsLookup = _snapshotStore.GetChildSnapshots<SubTransactionSnapshot>(transactionReferences)?.ToDictionary(kvp => kvp.Key.EntityID, kvp => kvp.Value);

            foreach (var transaction in transacations)
            {
                if (transaction.TransactionType == TransactionTypes.Normal)
                {
                    CategoryMonthKey category = GetCategoryMonthKey(transaction);
                    if (category == null) continue;

                    if (groupedTransactions.ContainsKey(category))
                    {
                        groupedTransactions[category] += GetTransactionAmount(transaction);
                    }
                    else
                    {
                        groupedTransactions[category] = GetTransactionAmount(transaction);
                    }
                }
                else if (transaction.TransactionType == TransactionTypes.SplitTransaction)
                {
                    if (subTransactionsLookup != null
                        && subTransactionsLookup.TryGetValue(transaction.EntityID, out List<SubTransactionSnapshot> subTransactions))
                    {
                        foreach (var subTransaction in subTransactions)
                        {
                            CategoryMonthKey category = GetCategoryMonthKey(transaction, subTransaction);
                            if (category == null) continue;

                            if (groupedTransactions.ContainsKey(category))
                            {
                                groupedTransactions[category] += GetTransactionAmount(subTransaction);
                            }
                            else
                            {
                                groupedTransactions[category] = GetTransactionAmount(subTransaction);
                            }
                        }
                    }
                }
            }

            return groupedTransactions;
        }

        private CategoryMonthKey GetCategoryMonthKey(TransactionSnapshot transaction)
        {
            if (transaction.Category == null) return null;

            return new CategoryMonthKey(transaction.Category, transaction.TransactionDate);
        }

        private CategoryMonthKey GetCategoryMonthKey(TransactionSnapshot transaction, SubTransactionSnapshot subTransaction)
        {
            if (subTransaction.Category == null) return null;

            return new CategoryMonthKey(subTransaction.Category, transaction.TransactionDate);
        }
    }


}
