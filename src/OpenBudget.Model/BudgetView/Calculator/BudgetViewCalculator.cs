using OpenBudget.Model.BudgetView.Model;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using CategoryResultsDictionary = System.Collections.Generic.Dictionary<OpenBudget.Model.BudgetView.Model.CategoryMonthKey, OpenBudget.Model.BudgetView.Model.BudgetViewCategoryMonth>;
using TransactionDictionary = System.Collections.Generic.Dictionary<OpenBudget.Model.BudgetView.Model.CategoryMonthKey, decimal>;

namespace OpenBudget.Model.BudgetView.Calculator
{
    public class BudgetViewCalculator
    {
        private BudgetModel _model;

        public BudgetViewCalculator(BudgetModel budgetModel)
        {
            _model = budgetModel;
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
                    if (categoryMonth.EndBalance < 0M)
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
                        decimal previousEndBalance = category.Value[count - 1].EndBalance;
                        result.BeginningBalance = (previousEndBalance > 0M) ? previousEndBalance : 0M;
                    }

                    result.EndBalance = result.BeginningBalance + result.AmountBudgeted + result.TransactionsInMonth;

                    count++;
                }
            }
        }

        private CategoryResultsDictionary InitializeResults(TransactionDictionary groupedTransactions)
        {
            CategoryResultsDictionary results = AddTransactionsInMonth(groupedTransactions);
            AddAmountsBudgeted(results);
            return results;
            //return results.ToDictionary(r => r.Key, r => r.Value.Values.OrderBy(rcm => rcm.Month).ToList());
        }

        private CategoryResultsDictionary AddTransactionsInMonth(TransactionDictionary groupedTransactions)
        {
            return groupedTransactions.ToDictionary(td => td.Key, td => new BudgetViewCategoryMonth(td.Key.EntityID, td.Key.FirstDayOfMonth) { TransactionsInMonth = td.Value });
        }

        private void AddAmountsBudgeted(CategoryResultsDictionary results)
        {
            foreach (var categoryMonth in _model.GetBudget().MasterCategories.SelectMany(mc => mc.Categories).SelectMany(c => c.CategoryMonths.GetAllMaterialized()))
            {
                if (categoryMonth.AmountBudgeted == 0M) continue;

                CategoryMonthKey monthKey = new CategoryMonthKey(categoryMonth.Parent as Category, categoryMonth.Month);
                BudgetViewCategoryMonth monthResult = null;

                BudgetViewCategoryMonth monthValues = null;
                if (results.TryGetValue(monthKey, out monthValues))
                {
                    monthValues.AmountBudgeted = categoryMonth.AmountBudgeted;
                }
                else
                {
                    monthResult = new BudgetViewCategoryMonth(monthKey.EntityID, monthKey.FirstDayOfMonth) { AmountBudgeted = categoryMonth.AmountBudgeted };
                    results[monthKey] = monthResult;
                }
            }
        }

        private TransactionDictionary GroupTransactions()
        {
            var groupedTransactions = new Dictionary<CategoryMonthKey, decimal>();

            foreach (var transaction in _model.GetBudget().Accounts.SelectMany(a => a.Transactions))
            {
                if (transaction.TransactionType == TransactionTypes.Normal)
                {
                    CategoryMonthKey category = GetCategoryMonthKey(transaction);
                    if (category == null) continue;

                    if (groupedTransactions.ContainsKey(category))
                    {
                        groupedTransactions[category] += transaction.Amount;
                    }
                    else
                    {
                        groupedTransactions[category] = transaction.Amount;
                    }
                }
                else if (transaction.TransactionType == TransactionTypes.SplitTransaction)
                {
                    foreach (var subTransaction in transaction.SubTransactions)
                    {
                        CategoryMonthKey category = GetCategoryMonthKey(transaction, subTransaction);
                        if (category == null) continue;

                        if (groupedTransactions.ContainsKey(category))
                        {
                            groupedTransactions[category] += subTransaction.Amount;
                        }
                        else
                        {
                            groupedTransactions[category] = subTransaction.Amount;
                        }
                    }
                }
            }

            return groupedTransactions;
        }

        private CategoryMonthKey GetCategoryMonthKey(Transaction transaction)
        {
            CategoryMonthKey category = null;
            if (transaction.TransactionCategory != null)
            {
                category = new CategoryMonthKey(transaction.TransactionCategory, transaction.TransactionDate);
            }
            else if (transaction.IncomeCategory != null)
            {
                category = new CategoryMonthKey(transaction.IncomeCategory);
            }

            return category;
        }

        private CategoryMonthKey GetCategoryMonthKey(Transaction transaction, SubTransaction subTransaction)
        {
            CategoryMonthKey category = null;
            if (subTransaction.TransactionCategory != null)
            {
                category = new CategoryMonthKey(subTransaction.TransactionCategory, transaction.TransactionDate);
            }
            else if (subTransaction.IncomeCategory != null)
            {
                category = new CategoryMonthKey(subTransaction.IncomeCategory);
            }

            return category;
        }
    }


}
