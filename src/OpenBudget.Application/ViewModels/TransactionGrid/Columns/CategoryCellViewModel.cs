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
        private EntityCollection<MasterCategory> _masterCategorySource;
        private IncomeCategoryFinder _incomeCategorySource;

        public CategoryCellViewModel(
            TransactionGridColumnViewModel<EntityBase> column,
            TransactionGridRowViewModel row,
            Transaction transaction,
            EntityCollection<MasterCategory> masterCategorySource,
            IncomeCategoryFinder incomeCategorySource)
            : base(column, row, transaction)
        {
            _masterCategorySource = masterCategorySource;
            _incomeCategorySource = incomeCategorySource;
        }

        public CategoryCellViewModel(
            TransactionGridColumnViewModel<EntityBase> column,
            TransactionGridRowViewModel row, Transaction transaction,
            SubTransactionRowViewModel subTransactionRow,
            SubTransaction subTransaction,
            EntityCollection<MasterCategory> masterCategorySource,
            IncomeCategoryFinder incomeCategorySource)
            : base(column, row, transaction, subTransactionRow, subTransaction)
        {
            _masterCategorySource = masterCategorySource;
            _incomeCategorySource = incomeCategorySource;
        }

        protected override string ConvertToDisplayText(EntityBase value)
        {
            if (this.CellType == ColumnType.Transaction
                && value == null
                && Transaction.TransactionType == TransactionTypes.SplitTransaction)
            {
                return "Split Transaction";
            }
            else if (value is Category cat)
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

            throw new InvalidOperationException("Unrecognized entity in CategoryCellViewModel ConvertToDisplayText.");
        }

        protected override void SetResultItemToValue(ResultItemViewModel item)
        {
            if (this.CellType == ColumnType.Transaction
                && item.ItemType == ResultItemType.SplitCategory)
            {
                Transaction.MakeSplitTransaction();
                Transaction.SubTransactions.Create();
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
            if (string.IsNullOrWhiteSpace(SearchText)) yield break;

            if (this.CellType == ColumnType.Transaction)
            {
                var split = GetFilteredSplitCategory();
                if (split != null) yield return split;
            }

            var income = GetFilteredIncomeCategory();
            if (income != null) yield return income;

            foreach (ResultCategoryViewModel category in GetFilteredCategories())
            {
                yield return category;
            }
        }

        private IEnumerable<ResultCategoryViewModel> GetFilteredCategories()
        {
            foreach (MasterCategory masterCategory in _masterCategorySource)
            {
                var matchingCategories = masterCategory.Categories.Where(c => c.Name.StartsWith(SearchText, StringComparison.CurrentCultureIgnoreCase)).ToList();
                if (matchingCategories.Count == 0) continue;

                yield return new ResultCategoryViewModel(masterCategory.Name, matchingCategories.Select(cat => new ResultItemViewModel(cat.Name, cat, ResultItemType.Category)));
            }
        }

        private ResultCategoryViewModel GetFilteredSplitCategory()
        {
            if ("Split Transaction".StartsWith(SearchText, StringComparison.CurrentCultureIgnoreCase))
            {
                return new ResultCategoryViewModel("Split", new ResultItemViewModel[] { new ResultItemViewModel("Split Transaction", null, ResultItemType.SplitCategory) });
            }
            else
            {
                return null;
            }
        }

        private ResultCategoryViewModel GetFilteredIncomeCategory()
        {
            if (!"Income".StartsWith(SearchText, StringComparison.CurrentCultureIgnoreCase))
            {
                return null;
            }

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
