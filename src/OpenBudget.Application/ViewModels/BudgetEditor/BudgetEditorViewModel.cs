using GalaSoft.MvvmLight;
using OpenBudget.Model;
using OpenBudget.Model.Entities;
using OpenBudget.Util.Collections;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.ViewModels.BudgetEditor
{
    public class BudgetEditorViewModel : ViewModelBase, IDisposable
    {
        private BudgetModel _budgetModel;

        public BudgetEditorViewModel(BudgetModel budgetModel)
        {
            _budgetModel = budgetModel;
            _categories = new TransformingObservableCollection<BudgetCategory, BudgetCategoryRowViewModel>(
                _budgetModel.Budget.BudgetCategories,
                (bc) => { return new BudgetCategoryRowViewModel(); },
                bc => { });
        }

        private TransformingObservableCollection<BudgetCategory, BudgetCategoryRowViewModel> _categories;

        public TransformingObservableCollection<BudgetCategory, BudgetCategoryRowViewModel> Categories
        {
            get { return _categories; }
            private set { _categories = value; RaisePropertyChanged(); }
        }


        public void Dispose()
        {
            _categories.Dispose();
        }
    }
}
