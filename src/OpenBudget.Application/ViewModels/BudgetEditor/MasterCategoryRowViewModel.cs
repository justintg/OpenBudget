using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using OpenBudget.Model.BudgetView;
using OpenBudget.Model.Entities;
using OpenBudget.Util.Collections;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;

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

            InitializeCategories(masterCategory);

            InitializeMonthViews();
            UpdateIsFirstCategoryRow();
        }

        private void InitializeMonthViews()
        {
            _masterCategoryMonthViews = new TransformingObservableCollection<BudgetMonthViewModel, MasterCategoryMonthViewModel>(
            _budgetEditor.VisibleMonthViews, v =>
            {
                //BudgetMonthView holds it's own copy of the Budget and Categories so you have to match them up based on entityId
                //instead of ReferenceEquals on the instance
                BudgetMonthView view = v.BudgetMonthView;
                MasterCategoryMonthView masterView = view.MasterCategories.Where(mcv => mcv.MasterCategory.EntityID == _masterCategory.EntityID).SingleOrDefault();
                if (masterView != null)
                {
                    return new MasterCategoryMonthViewModel(_budgetEditor, v, this, masterView);
                }
                else
                {
                    return new MasterCategoryMonthViewModel(_budgetEditor, v, this, view, _masterCategory.EntityID);
                }
            },
            cmv =>
            {
            });
        }

        private void InitializeCategories(MasterCategory masterCategory)
        {
            _categories = new TransformingObservableCollection<Category, CategoryRowViewModel>(
                masterCategory.Categories,
                c => { return new CategoryRowViewModel(this, c, _budgetEditor); },
                cvm => { cvm.Dispose(); });

            _categories.Sort(c => c.Category.SortOrder);
            _categories.CollectionChanged += Categories_CollectionChanged;
        }

        private void Categories_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateIsFirstCategoryRow();
        }

        private void UpdateIsFirstCategoryRow()
        {
            for (int i = 0; i < _categories.Count; i++)
            {
                if (i == 0)
                {
                    _categories[i].IsFirstCategoryRow = true;
                }
                else
                {
                    _categories[i].IsFirstCategoryRow = false;
                }
            }
        }

        private MasterCategory _masterCategory;

        public MasterCategory MasterCategory
        {
            get { return _masterCategory; }
            set { _masterCategory = value; RaisePropertyChanged(); }
        }

        private TransformingObservableCollection<BudgetMonthViewModel, MasterCategoryMonthViewModel> _masterCategoryMonthViews;

        public TransformingObservableCollection<BudgetMonthViewModel, MasterCategoryMonthViewModel> MasterCategoryMonthViews
        {
            get { return _masterCategoryMonthViews; }
            private set { _masterCategoryMonthViews = value; RaisePropertyChanged(); }
        }

        private TransformingObservableCollection<Category, CategoryRowViewModel> _categories;

        public TransformingObservableCollection<Category, CategoryRowViewModel> Categories
        {
            get { return _categories; }
            private set { _categories = value; RaisePropertyChanged(); }
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
