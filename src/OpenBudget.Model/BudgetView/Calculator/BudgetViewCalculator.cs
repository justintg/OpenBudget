using OpenBudget.Model.Entities;
using OpenBudget.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TransactionDictionary = System.Collections.Generic.Dictionary<OpenBudget.Model.BudgetView.Calculator.BudgetViewCalculatorCategoryMonth, decimal>;
using CategoryResultsDictionary = System.Collections.Generic.Dictionary<OpenBudget.Model.BudgetView.Calculator.BudgetViewCalculatorCategory, System.Collections.Generic.Dictionary<OpenBudget.Model.BudgetView.Calculator.BudgetViewCalculatorCategoryMonth, OpenBudget.Model.BudgetView.Calculator.BudgetViewCalculatorCategoryMonthResult>>;

namespace OpenBudget.Model.BudgetView.Calculator
{
    internal class BudgetViewCalculator
    {
        private BudgetModel _model;

        public BudgetViewCalculator(BudgetModel budgetModel)
        {
            _model = budgetModel;
        }

        public BudgetViewCalculatorResult Calculate()
        {
            Dictionary<BudgetViewCalculatorCategoryMonth, decimal> groupedTransactions = GroupTransactions();
            var results = CalculateCategoryResults(groupedTransactions);

            return null;
        }

        private Dictionary<BudgetViewCalculatorCategory, List<BudgetViewCalculatorCategoryMonthResult>>
            CalculateCategoryResults(Dictionary<BudgetViewCalculatorCategoryMonth, decimal> groupedTransactions)
        {
            Dictionary<BudgetViewCalculatorCategory, List<BudgetViewCalculatorCategoryMonthResult>> results = InitializeResults(groupedTransactions);
            foreach (var category in results)
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

            return results;
        }

        private Dictionary<BudgetViewCalculatorCategory, List<BudgetViewCalculatorCategoryMonthResult>> InitializeResults(TransactionDictionary groupedTransactions)
        {
            CategoryResultsDictionary results = new CategoryResultsDictionary();
            AddTransactionsInMonth(groupedTransactions, results);
            AddAmountsBudgeted(results);

            return results.ToDictionary(r => r.Key, r => r.Value.Values.OrderBy(rcm => rcm.Month).ToList());
        }

        private void AddTransactionsInMonth(TransactionDictionary groupedTransactions, CategoryResultsDictionary results)
        {
            foreach (var categoryMonth in groupedTransactions.Where(cat => cat.Key.EntityType == nameof(Category)))
            {
                BudgetViewCalculatorCategory category = new BudgetViewCalculatorCategory(categoryMonth.Key);
                Dictionary<BudgetViewCalculatorCategoryMonth, BudgetViewCalculatorCategoryMonthResult> resultDict = null;
                if (results.TryGetValue(category, out resultDict))
                {
                    resultDict.Add(categoryMonth.Key, new BudgetViewCalculatorCategoryMonthResult(categoryMonth.Key.EntityID, categoryMonth.Key.FirstDayOfMonth) { TransactionsInMonth = categoryMonth.Value });
                }
                else
                {
                    resultDict = new Dictionary<BudgetViewCalculatorCategoryMonth, BudgetViewCalculatorCategoryMonthResult>();
                    resultDict.Add(categoryMonth.Key, new BudgetViewCalculatorCategoryMonthResult(categoryMonth.Key.EntityID, categoryMonth.Key.FirstDayOfMonth) { TransactionsInMonth = categoryMonth.Value });
                    results[category] = resultDict;
                }
            }
        }

        private void AddAmountsBudgeted(CategoryResultsDictionary results)
        {
            foreach (var categoryMonth in _model.Budget.MasterCategories.SelectMany(mc => mc.Categories).SelectMany(c => c.CategoryMonths.GetAllMaterialized()))
            {
                if (categoryMonth.AmountBudgeted == 0M) continue;

                BudgetViewCalculatorCategoryMonth monthKey = new BudgetViewCalculatorCategoryMonth(categoryMonth.Parent as Category, categoryMonth.Month);
                BudgetViewCalculatorCategory categoryKey = new BudgetViewCalculatorCategory(monthKey);
                BudgetViewCalculatorCategoryMonthResult monthResult = null;

                Dictionary<BudgetViewCalculatorCategoryMonth, BudgetViewCalculatorCategoryMonthResult> resultDict = null;
                if (results.TryGetValue(categoryKey, out resultDict))
                {
                    if (resultDict.TryGetValue(monthKey, out monthResult))
                    {
                        monthResult.AmountBudgeted = categoryMonth.AmountBudgeted;
                    }
                    else
                    {
                        monthResult = new BudgetViewCalculatorCategoryMonthResult(monthKey.EntityID, monthKey.FirstDayOfMonth) { AmountBudgeted = categoryMonth.AmountBudgeted };
                        resultDict[monthKey] = monthResult;
                    }
                }
                else
                {
                    resultDict = new Dictionary<BudgetViewCalculatorCategoryMonth, BudgetViewCalculatorCategoryMonthResult>();
                    monthResult = new BudgetViewCalculatorCategoryMonthResult(monthKey.EntityID, monthKey.FirstDayOfMonth) { AmountBudgeted = categoryMonth.AmountBudgeted };
                    resultDict[monthKey] = monthResult;
                    results[categoryKey] = resultDict;
                }
            }
        }

        private void SortResultLists(Dictionary<BudgetViewCalculatorCategory, List<BudgetViewCalculatorCategoryMonthResult>> results)
        {
            Comparison<BudgetViewCalculatorCategoryMonthResult> comparision = (x, y) =>
            {
                return x.Month.CompareTo(y.Month);
            };

            foreach (var result in results)
            {
                result.Value.Sort(comparision);
            }
        }

        private TransactionDictionary GroupTransactions()
        {
            var groupedTransactions = new Dictionary<BudgetViewCalculatorCategoryMonth, decimal>();

            foreach (var transaction in _model.Budget.Accounts.SelectMany(a => a.Transactions))
            {
                if (transaction.TransactionType == TransactionTypes.Normal)
                {
                    BudgetViewCalculatorCategoryMonth category = null;
                    if (transaction.TransactionCategory != null)
                    {
                        category = new BudgetViewCalculatorCategoryMonth(transaction.TransactionCategory, transaction.TransactionDate);
                    }
                    else if (transaction.IncomeCategory != null)
                    {
                        category = new BudgetViewCalculatorCategoryMonth(transaction.IncomeCategory);
                    }

                    if (groupedTransactions.ContainsKey(category))
                    {
                        groupedTransactions[category] += transaction.Amount;
                    }
                    else
                    {
                        groupedTransactions[category] = transaction.Amount;
                    }
                }
            }

            return groupedTransactions;
        }
    }


}
