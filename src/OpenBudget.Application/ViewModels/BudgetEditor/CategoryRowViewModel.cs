using GalaSoft.MvvmLight;
using OpenBudget.Model.Entities;

namespace OpenBudget.Application.ViewModels.BudgetEditor
{
    public class CategoryRowViewModel : ViewModelBase
    {
        public CategoryRowViewModel(Category category)
        {
            _category = category;
        }

        private Category _category;

        public Category Category
        {
            get { return _category; }
            set { _category = value; RaisePropertyChanged(); }
        }

    }
}
