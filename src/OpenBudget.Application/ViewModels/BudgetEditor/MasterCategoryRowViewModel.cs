using GalaSoft.MvvmLight;
using OpenBudget.Model.Entities;
using OpenBudget.Util.Collections;
using System;

namespace OpenBudget.Application.ViewModels.BudgetEditor
{
    public class MasterCategoryRowViewModel : ViewModelBase, IDisposable
    {
        public MasterCategoryRowViewModel(MasterCategory masterCategory)
        {
            _masterCategory = masterCategory;
            _categories = new TransformingObservableCollection<Category, CategoryRowViewModel>(
                masterCategory.Categories,
                c => { return new CategoryRowViewModel(c); },
                cvm => { });
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

        public void Dispose()
        {
            _categories?.Dispose();
        }
    }
}
