using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using OpenBudget.Model.Entities;
using System;

namespace OpenBudget.Application.ViewModels.BudgetEditor
{
    public class CategoryMonthNoteEditorViewModel : ViewModelBase, IDisposable
    {
        public CategoryMonthNoteEditorViewModel(CategoryMonth categoryMonth)
        {
            SaveNoteCommand = new RelayCommand(SaveNote);
            CloseEditorCommand = new RelayCommand(CloseEditor);
            CategoryMonth = categoryMonth;
        }

        private CategoryMonth _categoryMonth;

        public CategoryMonth CategoryMonth
        {
            get { return _categoryMonth; }
            private set { _categoryMonth = value; RaisePropertyChanged(); }
        }

        private bool _isOpen;

        public bool IsOpen
        {
            get { return _isOpen; }
            set
            {
                _isOpen = value;
                if (_isOpen)
                {
                    this.NewNoteText = this.CategoryMonth.Note;
                }
                else
                {
                    this.NewNoteText = this.CategoryMonth.Note;
                }

                RaisePropertyChanged();
            }
        }

        private string _newNoteText;

        public string NewNoteText
        {
            get { return _newNoteText; }
            set { _newNoteText = value; RaisePropertyChanged(); }
        }

        public bool HasNote
        {
            get
            {
                return !string.IsNullOrWhiteSpace(CategoryMonth?.Note);
            }
        }

        public RelayCommand CloseEditorCommand { get; private set; }

        private void CloseEditor()
        {
            if(IsOpen)
            {
                IsOpen = false;
            }
        }

        public RelayCommand SaveNoteCommand { get; private set; }

        private void SaveNote()
        {
            CategoryMonth.Note = NewNoteText;
            CategoryMonth.Model.SaveChanges();

            IsOpen = false;
        }

        public void Dispose()
        {

        }
    }
}
