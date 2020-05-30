using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using OpenBudget.Model.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.ViewModels.BudgetEditor
{
    public class EditMasterCategoryViewModel : ViewModelBase
    {
        private readonly MasterCategoryRowViewModel _masterCategoryRow;

        public EditMasterCategoryViewModel(MasterCategoryRowViewModel masterCategoryRow)
        {
            _masterCategoryRow = masterCategoryRow ?? throw new ArgumentNullException(nameof(masterCategoryRow));

            RenameMasterCategoryCommand = new RelayCommand(RenameMasterCategory, CanRenameMasterCategory);
        }

        private bool _isEditorOpen;

        public bool IsEditorOpen
        {
            get { return _isEditorOpen; }
            set
            {
                _isEditorOpen = value;
                if (_isEditorOpen)
                {
                    NewMasterCategoryName = _masterCategoryRow.MasterCategory.Name;
                }
                RaisePropertyChanged();
            }
        }

        private string _newMasterCategoryName;

        public string NewMasterCategoryName
        {
            get { return _newMasterCategoryName; }
            set { _newMasterCategoryName = value; RaisePropertyChanged(); RenameMasterCategoryCommand.RaiseCanExecuteChanged(); }
        }

        public RelayCommand RenameMasterCategoryCommand { get; private set; }

        private void RenameMasterCategory()
        {
            var model = _masterCategoryRow.MasterCategory.Model;
            var masterCategory = model.FindEntity<MasterCategory>(_masterCategoryRow.MasterCategory.EntityID);
            masterCategory.Name = NewMasterCategoryName;
            model.SaveChanges();
            IsEditorOpen = false;
            NewMasterCategoryName = string.Empty;
        }

        private bool CanRenameMasterCategory()
        {
            return !string.IsNullOrWhiteSpace(NewMasterCategoryName);
        }
    }
}
