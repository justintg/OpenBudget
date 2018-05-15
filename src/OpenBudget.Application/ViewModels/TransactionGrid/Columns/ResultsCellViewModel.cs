using GalaSoft.MvvmLight.Command;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace OpenBudget.Application.ViewModels.TransactionGrid.Columns
{
    public abstract class ResultsCellViewModel : TransactionGridCellViewModel<EntityBase>
    {
        protected bool _forceSetItem;

        public ResultsCellViewModel(TransactionGridColumnViewModel<EntityBase> column, TransactionGridRowViewModel row, Transaction transaction) : base(column, row, transaction)
        {
            this.SelectResultItemCommand = new RelayCommand<ResultItemViewModel>(SelectResultItem);
        }

        public RelayCommand<ResultItemViewModel> SelectResultItemCommand { get; private set; }

        protected virtual void SelectResultItem(ResultItemViewModel item)
        {
            try
            {
                _forceSetItem = true;
                ShowResults = false;
                Results = null;
                SetResultItemToValue(item);
                SearchText = DisplayText;
            }
            finally
            {
                _forceSetItem = false;
            }
        }

        protected virtual void SetResultItemToValue(ResultItemViewModel item)
        {
            Value = item.ReferencedEntity;
        }

        public string DisplayText
        {
            get
            {
                return ConvertToDisplayText(this.Value);
            }
        }

        private string _searchText;

        public string SearchText
        {
            get { return _searchText; }
            set { _searchText = value; RaisePropertyChanged(); OnSearchTextChanged(); }
        }

        private bool _showResults;

        public bool ShowResults
        {
            get { return _showResults; }
            set { _showResults = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<ResultCategoryViewModel> _results;

        public ObservableCollection<ResultCategoryViewModel> Results
        {
            get { return _results; }
            set { _results = value; RaisePropertyChanged(); }
        }

        protected abstract string ConvertToDisplayText(EntityBase value);

        protected virtual void OnSearchTextChanged()
        {
            if (!_forceSetItem)
            {
                Results = FilterResults();
                if (Results.Count > 0)
                    ShowResults = true;
                else
                    ShowResults = false;

                bool shouldSetValue;
                EntityBase value = SearchTextSetValue(out shouldSetValue);

                if (shouldSetValue) Value = value;
            }
        }

        protected abstract EntityBase SearchTextSetValue(out bool shouldSetValue);

        protected abstract ObservableCollection<ResultCategoryViewModel> FilterResults();

        protected virtual void ResetEditState()
        {
            _searchText = ConvertToDisplayText(this.Value);
            RaisePropertyChanged(nameof(SearchText));

            _results = null;
            RaisePropertyChanged(nameof(Results));

            _showResults = false;
            RaisePropertyChanged(nameof(ShowResults));
        }

        protected override void OnBeginEdit()
        {
            ResetEditState();
        }

        protected override void OnEndEdit()
        {
            ResetEditState();
        }

        protected override void OnValueChanged()
        {
            RaisePropertyChanged(nameof(DisplayText));
        }
    }
}
