using OpenBudget.Model.Entities;
using OpenBudget.Model.Infrastructure.Entities;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenBudget.Application.ViewModels.TransactionGrid.Columns
{
    public class PayeeCellViewModel : TransactionGridCellViewModel<EntityBase>
    {
        public ObservableCollection<Payee> PayeeSource { get; private set; }
        public ObservableCollection<Account> AccountSource { get; private set; }
        public Account CurrentAccount { get; private set; }

        public PayeeCellViewModel(TransactionGridColumnViewModel<EntityBase> column, TransactionGridRowViewModel row, Transaction transaction, Account currentAccount, ObservableCollection<Payee> payeeSource, ObservableCollection<Account> accountSource) : base(column, row, transaction)
        {
            CurrentAccount = currentAccount;
            PayeeSource = payeeSource;
            AccountSource = accountSource;

            SelectResultItemCommand = new RelayCommand<ResultItemViewModel>(SelectResultItem);
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

        private void ResetEditState()
        {
            _searchText = ConvertToDisplayText(this.Value);
            RaisePropertyChanged(nameof(SearchText));

            _results = null;
            RaisePropertyChanged(nameof(Results));

            _showResults = false;
            RaisePropertyChanged(nameof(ShowResults));
        }

        public RelayCommand<ResultItemViewModel> SelectResultItemCommand { get; private set; }

        private void SelectResultItem(ResultItemViewModel item)
        {
            try
            {
                _forceSetItem = true;
                SearchText = item.DisplayText;
                ShowResults = false;
                Value = item.ReferencedEntity;
                Results = null;
            }
            finally
            {
                _forceSetItem = false;
            }
        }

        private bool _forceSetItem = false;

        private void FilterResultsAndSetPayee()
        {
            if (_forceSetItem) return;

            var filteredAccounts = AccountSource.Where(a => a.Name.StartsWith(SearchText, StringComparison.CurrentCultureIgnoreCase)).ToList();
            var filteredPayees = PayeeSource.Where(p => p.Name.StartsWith(SearchText, StringComparison.CurrentCultureIgnoreCase)).ToList();

            Results = new ObservableCollection<ResultCategoryViewModel>(ConvertToResultCategories(filteredAccounts, filteredPayees));
            if (Results.Count > 0)
                ShowResults = true;
            else
                ShowResults = false;

            Value = new Payee() { Name = SearchText };
        }

        private IEnumerable<ResultCategoryViewModel> ConvertToResultCategories(List<Account> accountResults, List<Payee> payeeResults)
        {
            if (accountResults.Count > 0)
                yield return new ResultCategoryViewModel("Transfer To/From Account",
                    accountResults.Select(a => new ResultItemViewModel(ConvertToDisplayText(a), a, ResultItemType.Account)));

            if (payeeResults.Count > 0)
                yield return new ResultCategoryViewModel("Payee", payeeResults.Select(p => new ResultItemViewModel(ConvertToDisplayText(p), p, ResultItemType.Payee)));
        }

        private string ConvertToDisplayText(EntityBase entity)
        {
            if (entity == null)
            {
                return null;
            }
            else if (entity is Account)
            {
                return "Transfer: " + (entity as Account).Name;
            }
            else if (entity is Payee)
            {
                return (entity as Payee).Name;
            }

            return "ERROR";
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
            set { _searchText = value; RaisePropertyChanged(); FilterResultsAndSetPayee(); }
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
    }
}
