using OpenBudget.Model.Entities;
using OpenBudget.Model.Util;
using OpenBudget.Util.Collections;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.BudgetView
{
    public class BudgetMonthView : PropertyChangedBase, IDisposable
    {
        private BudgetModel _model;
        private DateTime _date;

        public BudgetMonthView(BudgetModel model, DateTime date)
        {
            _model = model;
            _date = date.FirstDayOfMonth();

            _masterCategories = new TransformingObservableCollection<MasterCategory, MasterCategoryMonthView>(
                _model.Budget.MasterCategories,
                mc => { return new MasterCategoryMonthView(mc, _date); },
                mcv => { mcv.Dispose(); });
        }

        private TransformingObservableCollection<MasterCategory, MasterCategoryMonthView> _masterCategories;

        public TransformingObservableCollection<MasterCategory, MasterCategoryMonthView> MasterCategories
        {
            get { return _masterCategories; }
            private set { _masterCategories = value; RaisePropertyChanged(); }
        }

        public void Dispose()
        {
            _masterCategories?.Dispose();
        }
    }
}
