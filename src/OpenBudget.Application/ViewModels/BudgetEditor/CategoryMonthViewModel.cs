using OpenBudget.Model.BudgetView;
using System;
using System.Collections.Specialized;

namespace OpenBudget.Application.ViewModels.BudgetEditor
{
    public class CategoryMonthViewModel : UtilViewModelBase, IDisposable
    {
        private CategoryMonthView _categoryMonthView;

        public CategoryMonthView CategoryMonthView
        {
            get { return _categoryMonthView; }
            set
            {
                MountSet(ref _categoryMonthView, value, MountCategoryMonthView, UnmountCategoryMonthView);
                RaisePropertyChanged();
            }
        }

        private void MountCategoryMonthView(CategoryMonthView obj)
        {
            obj.PropertyChanged += MonthView_PropertyChanged;
            NoteEditor = new CategoryMonthNoteEditorViewModel(obj.CategoryMonth);
        }

        private void UnmountCategoryMonthView(CategoryMonthView obj)
        {
            obj.PropertyChanged -= MonthView_PropertyChanged;
        }

        private CategoryMonthNoteEditorViewModel _noteEditor;

        public CategoryMonthNoteEditorViewModel NoteEditor
        {
            get { return _noteEditor; }
            private set { MountSet(ref _noteEditor, value, null, obj => obj.Dispose()); RaisePropertyChanged(); }
        }

        public BudgetMonthViewModel BudgetMonthViewModel { get; private set; }
        public CategoryRowViewModel CategoryRowViewModel { get; private set; }

        public CategoryMonthViewModel(BudgetMonthViewModel budgetMonthViewModel, CategoryRowViewModel categoryRowViewModel, CategoryMonthView monthView)
        {
            BudgetMonthViewModel = budgetMonthViewModel;
            CategoryRowViewModel = categoryRowViewModel;
            CategoryMonthView = monthView;
        }

        public CategoryMonthViewModel(BudgetMonthViewModel budgetMonthViewModel, CategoryRowViewModel categoryRowViewModel, MasterCategoryMonthView masterCategoryMonthView, string categoryId)
        {
            BudgetMonthViewModel = budgetMonthViewModel;
            CategoryRowViewModel = categoryRowViewModel;
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
            NoteEditor?.Dispose();
        }

        public decimal AmountBudgeted
        {
            get { return CategoryMonthView.AmountBudgeted; }
            set { CategoryMonthView.CategoryMonth.AmountBudgeted = value; CategoryMonthView.CategoryMonth.Model.SaveChanges(); }
        }

    }
}
