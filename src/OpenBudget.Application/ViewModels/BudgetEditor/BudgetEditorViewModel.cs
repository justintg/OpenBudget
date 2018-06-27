using GalaSoft.MvvmLight;
using OpenBudget.Model;
using OpenBudget.Model.BudgetView;
using OpenBudget.Model.Entities;
using OpenBudget.Util.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OpenBudget.Application.ViewModels.BudgetEditor
{
    public class BudgetEditorViewModel : ViewModelBase, IDisposable
    {
        private BudgetModel _budgetModel;
        private BudgetMonthView _selectedMonth;

        public BudgetEditorViewModel(BudgetModel budgetModel)
        {
            _budgetModel = budgetModel;
            InitializeMonthViews();
            _masterCategories = new TransformingObservableCollection<MasterCategory, MasterCategoryRowViewModel>(
                _budgetModel.Budget.MasterCategories,
                (mc) => { return new MasterCategoryRowViewModel(mc); },
                mcvm => { mcvm.Dispose(); });
        }

        private void InitializeMonthViews()
        {
            VisibleMonthViews = new ObservableCollection<BudgetMonthView>();

            _selectedMonth = new BudgetMonthView(_budgetModel, DateTime.Today);
            VisibleMonthViews.Add(_selectedMonth);
        }

        private TransformingObservableCollection<MasterCategory, MasterCategoryRowViewModel> _masterCategories;

        public TransformingObservableCollection<MasterCategory, MasterCategoryRowViewModel> MasterCategories
        {
            get { return _masterCategories; }
            private set { _masterCategories = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<BudgetMonthView> _visibleMonthViews;

        public ObservableCollection<BudgetMonthView> VisibleMonthViews
        {
            get { return _visibleMonthViews; }
            set { _visibleMonthViews = value; RaisePropertyChanged(); }
        }



        public void Dispose()
        {
            _masterCategories.Dispose();
        }
    }
}
