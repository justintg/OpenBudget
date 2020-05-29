using GalaSoft.MvvmLight;
using OpenBudget.Model.BudgetView;
using OpenBudget.Model.Entities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace OpenBudget.Application.ViewModels.BudgetEditor
{
    public class CategoryMonthViewModel : ViewModelBase, IDisposable
    {
        private CategoryMonthView _categoryMonthView;

        public CategoryMonthView CategoryMonthView
        {
            get { return _categoryMonthView; }
            set { _categoryMonthView = value; RaisePropertyChanged(); }
        }

        public BudgetMonthViewModel BudgetMonthViewModel { get; private set; }

        public CategoryMonthViewModel(BudgetMonthViewModel budgetMonthViewModel, CategoryMonthView monthView)
        {
            BudgetMonthViewModel = budgetMonthViewModel;
            CategoryMonthView = monthView;
            CategoryMonthView.PropertyChanged += MonthView_PropertyChanged;
        }

        public CategoryMonthViewModel(BudgetMonthViewModel budgetMonthViewModel, MasterCategoryMonthView masterCategoryMonthView, string categoryId)
        {
            BudgetMonthViewModel = budgetMonthViewModel;
            RegisterForCallBack(masterCategoryMonthView, categoryId);
        }

        private MasterCategoryMonthView _masterCategoryMonthView;
        private string _categoryId;

        private void RegisterForCallBack(MasterCategoryMonthView masterCategoryMonthView, string categoryId)
        {
            _masterCategoryMonthView = masterCategoryMonthView;
            _categoryId = categoryId;
            masterCategoryMonthView.Categories.CollectionChanged += CategoryCollectionChanged;
        }

        private void CategoryCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (CategoryMonthView item in e.NewItems)
                {
                    if (item.Category.EntityID == _categoryId)
                    {
                        CategoryMonthView = item;
                        CategoryMonthView.PropertyChanged += MonthView_PropertyChanged;
                        _masterCategoryMonthView.Categories.CollectionChanged -= CategoryCollectionChanged;
                        _masterCategoryMonthView = null;
                        _categoryId = null;
                    }
                }
            }
        }

        private void MonthView_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OpenBudget.Model.BudgetView.CategoryMonthView.AmountBudgeted))
            {
                base.RaisePropertyChanged(nameof(AmountBudgeted));
            }
        }

        public void Dispose()
        {
            CategoryMonthView.PropertyChanged -= MonthView_PropertyChanged;
        }

        public decimal AmountBudgeted
        {
            get { return CategoryMonthView.AmountBudgeted; }
            set { CategoryMonthView.CategoryMonth.AmountBudgeted = value; CategoryMonthView.CategoryMonth.Model.SaveChanges(); }
        }

    }
}
