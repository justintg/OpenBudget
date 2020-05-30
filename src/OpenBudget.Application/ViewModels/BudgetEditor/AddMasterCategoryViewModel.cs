using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using OpenBudget.Model.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.ViewModels.BudgetEditor
{
    public class AddMasterCategoryViewModel : ViewModelBase
    {
        private readonly BudgetEditorViewModel _budgetEditor;

        public AddMasterCategoryViewModel(BudgetEditorViewModel budgetEditor)
        {
            _budgetEditor = budgetEditor ?? throw new ArgumentNullException(nameof(budgetEditor));

            AddMasterCategoryCommand = new RelayCommand(AddMasterCategory, CanAddMasterCategory);
        }

        private bool _isEditorOpen;

        public bool IsEditorOpen
        {
            get { return _isEditorOpen; }
            set { _isEditorOpen = value; RaisePropertyChanged(); }
        }

        private string _newMasterCategoryName;

        public string NewMasterCategoryName
        {
            get { return _newMasterCategoryName; }
            set { _newMasterCategoryName = value; RaisePropertyChanged(); AddMasterCategoryCommand.RaiseCanExecuteChanged(); }
        }

        public RelayCommand AddMasterCategoryCommand { get; private set; }

        private void AddMasterCategory()
        {
            var model = _budgetEditor.BudgetModel;
            var budget = model.GetBudget();
            budget.MasterCategories.Add(new MasterCategory() { Name = NewMasterCategoryName });
            model.SaveChanges();
            IsEditorOpen = false;
            NewMasterCategoryName = string.Empty;
        }

        private bool CanAddMasterCategory()
        {
            return !string.IsNullOrWhiteSpace(NewMasterCategoryName);
        }
    }
}
