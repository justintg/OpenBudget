using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
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

        public BudgetModel BudgetModel => _budgetModel;

        private Budget _budget;
        private BudgetMonthViewModel _selectedMonth;

        public BudgetEditorViewModel(BudgetModel budgetModel)
        {
            _budgetModel = budgetModel;

            InitializeRelayCommands();
            _addMasterCategoryEditor = new AddMasterCategoryViewModel(this);

            _budget = _budgetModel.GetBudget();
            _budget.MasterCategories.LoadCollection();
            _monthSelector = new MonthSelectorViewModel();
            _monthSelector.OnMonthSelected += MonthSelector_OnMonthSelected;

            InitializeMonthViews();
            _masterCategories = new TransformingObservableCollection<MasterCategory, MasterCategoryRowViewModel>(
                _budget.MasterCategories,
                (mc) => { return new MasterCategoryRowViewModel(mc, this); },
                mcvm => { mcvm.Dispose(); });
            _masterCategories.Sort(mcr => mcr.MasterCategory.SortOrder);

        }

        private AddMasterCategoryViewModel _addMasterCategoryEditor;

        public AddMasterCategoryViewModel AddMasterCategoryEditor
        {
            get { return _addMasterCategoryEditor; }
            private set { _addMasterCategoryEditor = value; RaisePropertyChanged(); }
        }

        private void InitializeRelayCommands()
        {
            SelectNextMonthCommand = new RelayCommand(SelectNextMonth);
            SelectPreviousMonthCommand = new RelayCommand(SelectPreviousMonth);
        }

        private void MonthSelector_OnMonthSelected(DateTime month)
        {
            SelectMonth(month);
        }

        public RelayCommand SelectNextMonthCommand { get; private set; }

        private void SelectNextMonth()
        {
            var nextMonth = _selectedMonth.BudgetMonthView.Date.AddMonths(1).FirstDayOfMonth();
            SelectMonth(nextMonth);
        }

        public RelayCommand SelectPreviousMonthCommand { get; private set; }

        private void SelectPreviousMonth()
        {
            var previousMonth = _selectedMonth.BudgetMonthView.Date.AddMonths(-1).FirstDayOfMonth();
            SelectMonth(previousMonth);
        }

        private void SelectMonth(DateTime month)
        {
            if (_selectedMonth.BudgetMonthView.Date == month) return;

            int visibleMonthsCount = VisibleMonthViews.Count;
            var desiredMonths = CalculateDesiredMonths(month, visibleMonthsCount);
            List<BudgetMonthViewModel> monthsToKeep = new List<BudgetMonthViewModel>();
            for (int i = 0; i < desiredMonths.Count; i++)
            {
                var desiredMonth = desiredMonths[i];
                var monthToKeep = VisibleMonthViews.Where(vm => vm.BudgetMonthView.Date == desiredMonth.desiredMonthDate).SingleOrDefault();
                if (monthToKeep != null)
                {
                    desiredMonth.desiredMonth = monthToKeep;
                    monthsToKeep.Add(monthToKeep);
                }
                else
                {
                    desiredMonth.desiredMonth = new BudgetMonthViewModel(new BudgetMonthView(_budgetModel, desiredMonth.desiredMonthDate));
                }
                desiredMonths[i] = desiredMonth;
            }

            var monthsToDispose = VisibleMonthViews.Where(vm => !monthsToKeep.Contains(vm)).ToList();
            foreach (var monthToDispose in monthsToDispose)
            {
                monthToDispose.Dispose();
            }

            for (int i = 0; i < visibleMonthsCount; i++)
            {
                VisibleMonthViews[i] = desiredMonths[i].desiredMonth;
            }

            _selectedMonth = VisibleMonthViews[0];
            UpdateMonthSelector();
            SetFirstVisibleMonthProperty();
        }

        private List<(DateTime desiredMonthDate, BudgetMonthViewModel desiredMonth)> CalculateDesiredMonths(DateTime startDate, int count)
        {
            List<ValueTuple<DateTime, BudgetMonthViewModel>> desiredMonths = new List<ValueTuple<DateTime, BudgetMonthViewModel>>();
            desiredMonths.Add(new ValueTuple<DateTime, BudgetMonthViewModel>(startDate, null));
            startDate = startDate.FirstDayOfMonth();
            DateTime nextDate = startDate;
            for (int i = 1; i < count; i++)
            {
                nextDate = nextDate.AddMonths(1);
                desiredMonths.Add(new ValueTuple<DateTime, BudgetMonthViewModel>(nextDate, null));
            }

            return desiredMonths;
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
                while (VisibleMonthViews.Count > number)
                {
                    BudgetMonthViewModel monthToRemove = VisibleMonthViews[VisibleMonthViews.Count - 1];
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
