using GalaSoft.MvvmLight;
using OpenBudget.Model;
using OpenBudget.Model.BudgetView;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Util;
using OpenBudget.Util.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
                (mc) => { return new MasterCategoryRowViewModel(mc, this); },
                mcvm => { mcvm.Dispose(); });
        }

        public void MakeNumberOfMonthsVisible(int number)
        {
            if (VisibleMonthViews.Count == number) return;
            if (VisibleMonthViews.Count < number)
            {
                DateTime rightMostDate = VisibleMonthViews.Last().Date;
                int monthsToAdd = number - VisibleMonthViews.Count;
                for (int i = 0; i < monthsToAdd; i++)
                {
                    rightMostDate = rightMostDate.AddMonths(1);
                    BudgetMonthView newView = new BudgetMonthView(_budgetModel, rightMostDate);
                    VisibleMonthViews.Add(newView);
                }
            }
            else if (VisibleMonthViews.Count > number)
            {
                int monthsToRemove = VisibleMonthViews.Count - number;
                for (int i = VisibleMonthViews.Count - monthsToRemove; i < VisibleMonthViews.Count; i++)
                {
                    BudgetMonthView monthToRemove = VisibleMonthViews[i];
                    VisibleMonthViews.Remove(monthToRemove);
                    monthToRemove.Dispose();
                }
            }
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
