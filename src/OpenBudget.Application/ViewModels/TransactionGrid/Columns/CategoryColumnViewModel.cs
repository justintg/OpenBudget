using OpenBudget.Model.Entities;
using OpenBudget.Model.Entities.Generators;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace OpenBudget.Application.ViewModels.TransactionGrid.Columns
{
    public class CategoryColumnViewModel : TransactionGridColumnViewModel<EntityBase>
    {
        private ObservableCollection<BudgetCategory> _categorySource;
        private IncomeCategoryFinder _incomeCategorySource;

        public CategoryColumnViewModel(
            Func<Transaction, EntityBase> getter,
            Action<Transaction, EntityBase> setter,
            string header,
            string propertyName,
            int width,
            ObservableCollection<BudgetCategory> categorySource,
            IncomeCategoryFinder incomeCategorySource)
            : base(getter, setter, header, propertyName, width)
        {
            _categorySource = categorySource;
            _incomeCategorySource = incomeCategorySource;
        }

        public override TransactionGridCellViewModel CreateCell(TransactionGridRowViewModel row, Transaction transaction)
        {
            return new CategoryCellViewModel(this, row, transaction, _categorySource, _incomeCategorySource);
        }
    }
}
