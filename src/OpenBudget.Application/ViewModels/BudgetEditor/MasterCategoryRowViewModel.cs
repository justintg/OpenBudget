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
            InitializeCommands();

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

        private void InitializeCommands()
        {
            AddCategoryCommand = new RelayCommand(AddCategory, CanAddCategory);
        }

        public RelayCommand AddCategoryCommand { get; private set; }

        private void AddCategory()
        {
            var budgetModel = _masterCategory.Model;
            var masterCategory = budgetModel.FindEntity<MasterCategory>(_masterCategory.EntityID);
            masterCategory.Categories.Add(new Category() { Name = NewCategoryName });
            budgetModel.SaveChanges();
            IsNewCategoryEditorOpen = false;
        }

        private bool CanAddCategory()
        {
            return !string.IsNullOrWhiteSpace(NewCategoryName);
        }

        private string _newCategoryName;

        public string NewCategoryName
        {
            get { return _newCategoryName; }
            set
            {
                _newCategoryName = value; RaisePropertyChanged();
                AddCategoryCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _isNewCategoryEditorOpen;

        public bool IsNewCategoryEditorOpen
        {
            get { return _isNewCategoryEditorOpen; }
            set
            {
                _isNewCategoryEditorOpen = value;
                if (!_isNewCategoryEditorOpen)
                {
                    NewCategoryName = string.Empty;
                }
                RaisePropertyChanged();
            }
        }

        public void Dispose()
        {
            _categories?.Dispose();
        }
    }
}
