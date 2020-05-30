using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using OpenBudget.Model.Entities;
using OpenBudget.Util.Collections;
using System;

namespace OpenBudget.Application.ViewModels.BudgetEditor
{
    public class MasterCategoryRowViewModel : ViewModelBase, IDisposable
    {
        private BudgetEditorViewModel _budgetEditor;

        public MasterCategoryRowViewModel(MasterCategory masterCategory, BudgetEditorViewModel budgetEditor)
        {
            _masterCategory = masterCategory;
            _budgetEditor = budgetEditor;
            _addCategoryEditor = new AddCategoryViewModel(this);
            _editMasterCategoryEditor = new EditMasterCategoryViewModel(this);

            if (!_masterCategory.Categories.IsLoaded)
                _masterCategory.Categories.LoadCollection();

            _categories = new TransformingObservableCollection<Category, CategoryRowViewModel>(
                masterCategory.Categories,
                c => { return new CategoryRowViewModel(this, c, _budgetEditor); },
                cvm => { cvm.Dispose(); });
        }

        private MasterCategory _masterCategory;

        public MasterCategory MasterCategory
        {
            get { return _masterCategory; }
            set { _masterCategory = value; RaisePropertyChanged(); }
        }

        private TransformingObservableCollection<Category, CategoryRowViewModel> _categories;

        public TransformingObservableCollection<Category, CategoryRowViewModel> Categories
        {
            get { return _categories; }
            set { _categories = value; RaisePropertyChanged(); }
        }

        private AddCategoryViewModel _addCategoryEditor;

        public AddCategoryViewModel AddCategoryEditor
        {
            get { return _addCategoryEditor; }
            private set { _addCategoryEditor = value; RaisePropertyChanged(); }
        }

        private EditMasterCategoryViewModel _editMasterCategoryEditor;

        public EditMasterCategoryViewModel EditMasterCategoryEditor
        {
            get { return _editMasterCategoryEditor; }
            private set { _editMasterCategoryEditor = value; RaisePropertyChanged(); }
        }

        public void Dispose()
        {
            _categories?.Dispose();
        }
    }
}
