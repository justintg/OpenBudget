using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using OpenBudget.Model.Entities;
using System;

namespace OpenBudget.Application.ViewModels.BudgetEditor
{
    public class AddCategoryViewModel : ViewModelBase
    {
        private readonly MasterCategoryRowViewModel _masterCategoryRow;

        public AddCategoryViewModel(MasterCategoryRowViewModel masterCategoryRow)
        {
            _masterCategoryRow = masterCategoryRow ?? throw new ArgumentNullException(nameof(masterCategoryRow));

            AddCategoryCommand = new RelayCommand(AddMasterCategory, CanAddCategory);
        }

        private bool _isEditorOpen;

        public bool IsEditorOpen
        {
            get { return _isEditorOpen; }
            set { _isEditorOpen = value; RaisePropertyChanged(); }
        }

        private string _newCategoryName;

        public string NewCategoryName
        {
            get { return _newCategoryName; }
            set { _newCategoryName = value; RaisePropertyChanged(); AddCategoryCommand.RaiseCanExecuteChanged(); }
        }

        public RelayCommand AddCategoryCommand { get; private set; }

        private void AddMasterCategory()
        {
            var model = _masterCategoryRow.MasterCategory.Model;
            var masterCategory = model.FindEntity<MasterCategory>(_masterCategoryRow.MasterCategory.EntityID);
            masterCategory.Categories.Add(new Category() { Name = NewCategoryName });
            model.SaveChanges();
            IsEditorOpen = false;
            NewCategoryName = string.Empty;
        }

        private bool CanAddCategory()
        {
            return !string.IsNullOrWhiteSpace(NewCategoryName);
        }
    }
}
