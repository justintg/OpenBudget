using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
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
            InitializeRelayCommands();

            MasterCategory = masterCategory;
            _category = category;
            _budgetEditor = budgetEditor;
            _categoryMonthViews = new TransformingObservableCollection<BudgetMonthViewModel, CategoryMonthViewModel>(
                _budgetEditor.VisibleMonthViews, v =>
                {
                    //BudgetMonthView holds it's own copy of the Budget and Categories so you have to match them up based on entityId
                    //instead of ReferenceEquals on the instance
                    BudgetMonthView view = v.BudgetMonthView;
                    MasterCategoryMonthView masterView = view.MasterCategories.Where(mcv => mcv.MasterCategory.EntityID == category.Parent.EntityID).Single();
                    var categoryMonthView = masterView.Categories.Where(c => c.Category.EntityID == _category.EntityID).SingleOrDefault();
                    if (categoryMonthView != null)
                    {
                        return new CategoryMonthViewModel(v, categoryMonthView);
                    }
                    else
                    {
                        return new CategoryMonthViewModel(v, masterView, _category.EntityID);
                    }
                },
                cmv =>
                {
                    cmv.Dispose();
                });
        }

        private Category _category;

        public Category Category
        {
            get { return _category; }
            private set { _category = value; RaisePropertyChanged(); }
        }


        private void InitializeRelayCommands()
        {
            SaveCategoryNameCommand = new RelayCommand(SaveCategoryName);
            SaveCategoryNoteCommand = new RelayCommand(SaveCategoryNote);
        }

        private bool _categoryEditorOpen;

        public bool CategoryEditorOpen
        {
            get { return _categoryEditorOpen; }
            set
            {
                _categoryEditorOpen = value;
                if (_categoryEditorOpen)
                {
                    OnCategoryEditorOpen();
                }
                else
                {
                    OnCategoryEditorClose();
                }
                RaisePropertyChanged();
            }
        }

        private void OnCategoryEditorClose()
        {
            NewCategoryName = Category.Name;
        }

        private void OnCategoryEditorOpen()
        {
            NewCategoryName = Category.Name;
        }

        private string _newCategoryName;

        public string NewCategoryName
        {
            get { return _newCategoryName; }
            set { _newCategoryName = value; RaisePropertyChanged(); }
        }

        public RelayCommand SaveCategoryNameCommand { get; private set; }

        private void SaveCategoryName()
        {
            if (NewCategoryName != Category.Name)
            {
                var budgetModel = Category.Model;
                var editorCategory = budgetModel.FindEntity<Category>(Category.EntityID);
                editorCategory.Name = NewCategoryName;
                budgetModel.SaveChanges();
            }
            CategoryEditorOpen = false;
        }

        private bool _categoryNoteEditorOpen;

        public bool CategoryNoteEditorOpen
        {
            get { return _categoryNoteEditorOpen; }
            set
            {
                _categoryNoteEditorOpen = value;
                if (_categoryNoteEditorOpen)
                {
                    OnCategoryNoteEditorOpen();
                }
                else
                {
                    OnCategoryNoteEditorClose();
                }
                RaisePropertyChanged();
            }
        }

        private void OnCategoryNoteEditorClose()
        {
            NewNoteText = Category.Note;
        }

        private void OnCategoryNoteEditorOpen()
        {
            NewNoteText = Category.Note;
        }

        private string _newNoteText;

        public string NewNoteText
        {
            get { return _newNoteText; }
            set { _newNoteText = value; RaisePropertyChanged(); }
        }

        public RelayCommand SaveCategoryNoteCommand { get; private set; }

        private void SaveCategoryNote()
        {
            if (NewNoteText != Category.Note)
            {
                var budgetModel = Category.Model;
                var editorCategory = budgetModel.FindEntity<Category>(Category.EntityID);
                editorCategory.Note = NewNoteText;
                budgetModel.SaveChanges();
            }
            CategoryNoteEditorOpen = false;
            RaisePropertyChanged(nameof(HasCategoryNote));
        }

        public bool HasCategoryNote
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Category.Note);
            }
        }

        private TransformingObservableCollection<BudgetMonthViewModel, CategoryMonthViewModel> _categoryMonthViews;

        public TransformingObservableCollection<BudgetMonthViewModel, CategoryMonthViewModel> CategoryMonthViews
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
