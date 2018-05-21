using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Entities.Generators;
using OpenBudget.Model.Infrastructure.Entities;

namespace OpenBudget.Application.ViewModels.TransactionGrid.Columns
{
    public class CategoryCellViewModel : ResultsCellViewModel
    {
        private ObservableCollection<BudgetCategory> _categorySource;
        private IncomeCategoryFinder _incomeCategorySource;

        public CategoryCellViewModel(
            TransactionGridColumnViewModel<EntityBase> column,
            TransactionGridRowViewModel row,
            Transaction transaction,
            ObservableCollection<BudgetCategory> categorySource,
            IncomeCategoryFinder incomeCategorySource)
            : base(column, row, transaction)
        {
            _categorySource = categorySource;
            _incomeCategorySource = incomeCategorySource;
        }

        protected override string ConvertToDisplayText(EntityBase value)
        {
            if (value == null && Transaction.TransactionType == TransactionTypes.SplitTransaction)
            {
                return "Split Transaction";
            }
            else if (value is BudgetSubCategory cat)
            {
                return cat.Name;
            }
            else if (value is IncomeCategory income)
            {
                return $"Income For {income.Month.ToString("MMMM yyyy")}";
            }
            else if (value == null)
            {
                return null;
            }

            return "ERROR";
        }

        protected override void SetResultItemToValue(ResultItemViewModel item)
        {
            if (item.ItemType == ResultItemType.SplitCategory)
            {
                Transaction.MakeSplitTransaction();
            }
            else
            {
                base.SetResultItemToValue(item);
            }
        }

        protected override ObservableCollection<ResultCategoryViewModel> FilterResults()
        {
            var results = new ObservableCollection<ResultCategoryViewModel>(FilterInternal());
            return results;
        }

        private IEnumerable<ResultCategoryViewModel> FilterInternal()
        {
            var split = GetFilteredSplitCategory();
            if (split != null) yield return split;

            var income = GetFilteredIncomeCategory();
            if (income != null) yield return income;
        }

        private ResultCategoryViewModel GetFilteredSplitCategory()
        {
            return new ResultCategoryViewModel("Split", new ResultItemViewModel[] { new ResultItemViewModel("Split Transaction", null, ResultItemType.SplitCategory) });
        }

        private ResultCategoryViewModel GetFilteredIncomeCategory()
        {
            var transactionDate = Transaction.TransactionDate == default(DateTime)
                ? DateTime.Today
                : Transaction.TransactionDate;

            var thisMonthDate = new DateTime(transactionDate.Year, transactionDate.Month, 1);
            var nextMonthDate = thisMonthDate.AddMonths(1);

            var thisMonth = _incomeCategorySource.GetIncomeCategory(thisMonthDate);
            var nextMonth = _incomeCategorySource.GetIncomeCategory(nextMonthDate);

            ResultItemViewModel[] incomeItems =
                new ResultItemViewModel[] {
                    new ResultItemViewModel(ConvertToDisplayText(thisMonth), thisMonth, ResultItemType.IncomeCategory),
                    new ResultItemViewModel(ConvertToDisplayText(nextMonth), nextMonth, ResultItemType.IncomeCategory)
                };

            return new ResultCategoryViewModel("Income", incomeItems);
        }

        protected override EntityBase SearchTextSetValue(out bool shouldSetValue)
        {
            shouldSetValue = false;
            return null;
        }
    }
}
