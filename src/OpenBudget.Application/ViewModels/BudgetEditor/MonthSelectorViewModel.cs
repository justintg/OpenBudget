using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace OpenBudget.Application.ViewModels.BudgetEditor
{
    public class MonthSelectorViewModel : ViewModelBase
    {
        private ObservableCollection<MonthSelectorMonthViewModel> _months;

        public ObservableCollection<MonthSelectorMonthViewModel> Months
        {
            get { return _months; }
            private set { _months = value; RaisePropertyChanged(); }
        }

        private string _year;

        public string Year
        {
            get { return _year; }
            private set { _year = value; RaisePropertyChanged(); }
        }

        public RelayCommand<MonthSelectorMonthViewModel> MonthSelectedCommand { get; private set; }

        public MonthSelectorViewModel()
        {
            _months = new ObservableCollection<MonthSelectorMonthViewModel>();
            MonthSelectedCommand = new RelayCommand<MonthSelectorMonthViewModel>(MonthSelected);
        }

        private void MonthSelected(MonthSelectorMonthViewModel month)
        {
            OnMonthSelected?.Invoke(month.FirstDayOfMonth);
        }

        public event Action<DateTime> OnMonthSelected;

        public void SetMonths(DateTime selectedDate, IList<DateTime> visibleDates)
        {
            int year = selectedDate.Year;
            Year = year.ToString();
            var months = GetFirstDaysOfMonths(year).Select(d =>
            {
                return new MonthSelectorMonthViewModel()
                {
                    FirstDayOfMonth = d,
                    IsSelected = d == selectedDate,
                    IsVisible = visibleDates.Contains(d)
                };
            }).ToList();

            if (Months.Count == 12)
            {
                for (int i = 0; i < 12; i++)
                {
                    Months[i] = months[i];
                }
            }
            else
            {
                foreach (var month in months)
                {
                    Months.Add(month);
                }
            }
        }

        private List<DateTime> GetFirstDaysOfMonths(int year)
        {
            List<DateTime> firstDaysOfMonths = new List<DateTime>();
            for (int i = 1; i <= 12; i++)
            {
                firstDaysOfMonths.Add(new DateTime(year, i, 1));
            }
            return firstDaysOfMonths;
        }
    }

    public class MonthSelectorMonthViewModel : ViewModelBase
    {
        private DateTime _firstDayOfMonth;

        public DateTime FirstDayOfMonth
        {
            get { return _firstDayOfMonth; }
            set { _firstDayOfMonth = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(MonthName)); }
        }

        public string MonthName
        {
            get { return FirstDayOfMonth.ToString("MMMM"); }
        }

        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; RaisePropertyChanged(); }
        }

        private bool _isVisible;

        public bool IsVisible
        {
            get { return _isVisible; }
            set { _isVisible = value; RaisePropertyChanged(); }
        }
    }
}
