﻿using GalaSoft.MvvmLight;
using OpenBudget.Model.BudgetView;
using OpenBudget.Model.Entities;
using OpenBudget.Util.Collections;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenBudget.Application.ViewModels.BudgetEditor
{
    public class CategoryRowViewModel : ViewModelBase, IDisposable
    {
        private BudgetEditorViewModel _budgetEditor;
        public MasterCategoryRowViewModel MasterCategory { get; private set; }

        public CategoryRowViewModel(MasterCategoryRowViewModel masterCategory, Category category, BudgetEditorViewModel budgetEditor)
        {
            MasterCategory = masterCategory;
            _category = category;
            _budgetEditor = budgetEditor;
            _categoryMonthViews = new TransformingObservableCollection<BudgetMonthView, CategoryMonthViewModel>(
                _budgetEditor.VisibleMonthViews, v =>
                {
                    MasterCategoryMonthView masterView = v.MasterCategories.Where(mcv => mcv.MasterCategory == category.Parent).Single();
                    return new CategoryMonthViewModel(masterView.Categories.Where(c => c.Category == _category).Single());
                },
                cmv => { cmv.Dispose(); });
        }

        private Category _category;

        public Category Category
        {
            get { return _category; }
            private set { _category = value; RaisePropertyChanged(); }
        }

        private TransformingObservableCollection<BudgetMonthView, CategoryMonthViewModel> _categoryMonthViews;

        public TransformingObservableCollection<BudgetMonthView, CategoryMonthViewModel> CategoryMonthViews
        {
            get { return _categoryMonthViews; }
            set { _categoryMonthViews = value; RaisePropertyChanged(); }
        }

        public void Dispose()
        {
            _categoryMonthViews?.Dispose();
        }

    }
}
