using OpenBudget.Model.Entities;
using OpenBudget.Model.Util;
using OpenBudget.Util.Collections;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.BudgetView
{
    public class MasterCategoryMonthView : PropertyChangedBase, IDisposable
    {
        private BudgetCategory _category;
        private DateTime _date;

        public MasterCategoryMonthView(BudgetCategory category, DateTime date)
        {
            _category = category;
            _date = date.FirstDayOfMonth();

            _categories = new TransformingObservableCollection<BudgetSubCategory, BudgetSubCategoryMonthView>(_category.SubCategories,
                sc =>
                {
                    return new BudgetSubCategoryMonthView(sc, _date);
                },
                scv =>
                {
                    scv.Dispose();
                });
        }

        private TransformingObservableCollection<BudgetSubCategory, BudgetSubCategoryMonthView> _categories;

        public TransformingObservableCollection<BudgetSubCategory, BudgetSubCategoryMonthView> Categories
        {
            get { return _categories; }
            private set { _categories = value; RaisePropertyChanged(); }
        }


        public void Dispose()
        {
            _categories.Dispose();
        }
    }
}
