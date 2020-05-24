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
        private Budget _budget;
        private BudgetMonthViewModel _selectedMonth;

        public BudgetEditorViewModel(BudgetModel budgetModel)
        {
            _budgetModel = budgetModel;
            _budget = _budgetModel.GetBudget();
            _budget.MasterCategories.LoadCollection();
            _monthSelector = new MonthSelectorViewModel();
            _monthSelector.OnMonthSelected += MonthSelector_OnMonthSelected;

            InitializeMonthViews();
            _masterCategories = new TransformingObservableCollection<MasterCategory, MasterCategoryRowViewModel>(
                _budget.MasterCategories,
                (mc) => { return new MasterCategoryRowViewModel(mc, this); },
                mcvm => { mcvm.Dispose(); });

        }

        private void MonthSelector_OnMonthSelected(DateTime month)
        {

        }

        public void MakeNumberOfMonthsVisible(int number)
        {
            if (VisibleMonthViews.Count == number) return;
            if (VisibleMonthViews.Count < number)
            {
                DateTime rightMostDate = VisibleMonthViews.Last().BudgetMonthView.Date;
                int monthsToAdd = number - VisibleMonthViews.Count;
                for (int i = 0; i < monthsToAdd; i++)
                {
                    rightMostDate = rightMostDate.AddMonths(1);
                    BudgetMonthViewModel newView = new BudgetMonthViewModel(new BudgetMonthView(_budgetModel, rightMostDate));
                    VisibleMonthViews.Add(newView);
                }
            }
            else if (VisibleMonthViews.Count > number)
            {
                int monthsToRemove = VisibleMonthViews.Count - number;
                for (int i = VisibleMonthViews.Count - monthsToRemove; i < VisibleMonthViews.Count; i++)
                {
                    BudgetMonthViewModel monthToRemove = VisibleMonthViews[i];
                    VisibleMonthViews.Remove(monthToRemove);
                    monthToRemove.Dispose();
                }
            }
            SetFirstVisibleMonthProperty();
            UpdateMonthSelector();
        }

        private void UpdateMonthSelector()
        {
            MonthSelector.SetMonths(_selectedMonth.BudgetMonthView.Date, _visibleMonthViews.Select(mv => mv.BudgetMonthView.Date).ToList());
        }

        private void InitializeMonthViews()
        {
            VisibleMonthViews = new ObservableCollection<BudgetMonthViewModel>();

            _selectedMonth = new BudgetMonthViewModel(new BudgetMonthView(_budgetModel, DateTime.Today));
            VisibleMonthViews.Add(_selectedMonth);
            SetFirstVisibleMonthProperty();
            UpdateMonthSelector();
        }

        private void SetFirstVisibleMonthProperty()
        {
            int count = 0;
            foreach (var visibleMonthView in VisibleMonthViews)
            {
                if (count == 0 && !visibleMonthView.IsFirstVisibleMonth)
                {
                    visibleMonthView.IsFirstVisibleMonth = true;
                }
                else if (count > 0 && visibleMonthView.IsFirstVisibleMonth)
                {
                    visibleMonthView.IsFirstVisibleMonth = false;
                }
                count++;
            }
        }

        private TransformingObservableCollection<MasterCategory, MasterCategoryRowViewModel> _masterCategories;

        public TransformingObservableCollection<MasterCategory, MasterCategoryRowViewModel> MasterCategories
        {
            get { return _masterCategories; }
            private set { _masterCategories = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<BudgetMonthViewModel> _visibleMonthViews;

        public ObservableCollection<BudgetMonthViewModel> VisibleMonthViews
        {
            get { return _visibleMonthViews; }
            set { _visibleMonthViews = value; RaisePropertyChanged(); }
        }

        private MonthSelectorViewModel _monthSelector;

        public MonthSelectorViewModel MonthSelector
        {
            get { return _monthSelector; }
            private set { _monthSelector = value; RaisePropertyChanged(); }
        }



        public void Dispose()
        {
            _masterCategories.Dispose();
            foreach (var view in VisibleMonthViews)
            {
                view.Dispose();
            }
        }
    }
}
