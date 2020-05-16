using OpenBudget.Model.Entities;
using OpenBudget.Model.Infrastructure.Entities;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenBudget.Application.ViewModels.TransactionGrid.Columns
{
    public class PayeeCellViewModel : ResultsCellViewModel
    {
        public EntityCollection<Payee> PayeeSource { get; private set; }
        public EntityCollection<Account> AccountSource { get; private set; }
        public Account CurrentAccount { get; private set; }

        public PayeeCellViewModel(
            TransactionGridColumnViewModel<EntityBase> column,
            TransactionGridRowViewModel row,
            Transaction transaction,
            Account currentAccount,
            EntityCollection<Payee> payeeSource,
            EntityCollection<Account> accountSource)
            : base(column, row, transaction)
        {
            CurrentAccount = currentAccount;
            PayeeSource = payeeSource;
            AccountSource = accountSource;
        }

        public PayeeCellViewModel(
            TransactionGridColumnViewModel<EntityBase> column,
            TransactionGridRowViewModel row,
            Transaction transaction,
            SubTransactionRowViewModel subTransactionRow,
            SubTransaction subTransaction,
            Account currentAccount,
            EntityCollection<Account> accountSource)
            : base(column, row, transaction, subTransactionRow, subTransaction)
        {
            CurrentAccount = currentAccount;
            AccountSource = accountSource;
        }

        private IEnumerable<ResultCategoryViewModel> ConvertToResultCategories(List<Account> accountResults, List<Payee> payeeResults)
        {
            if (accountResults.Count > 0)
                yield return new ResultCategoryViewModel("Transfer To/From Account",
                    accountResults.Select(a => new ResultItemViewModel(ConvertToDisplayText(a), a, ResultItemType.Account)));

            if (payeeResults != null && payeeResults.Count > 0)
                yield return new ResultCategoryViewModel("Payee", payeeResults.Select(p => new ResultItemViewModel(ConvertToDisplayText(p), p, ResultItemType.Payee)));
        }

        protected override string ConvertToDisplayText(EntityBase value)
        {
            if (value == null)
            {
                return null;
            }
            else if (value is Account a)
            {
                return "Transfer: " + a.Name;
            }
            else if (value is Payee p)
            {
                return p.Name;
            }

            throw new InvalidOperationException("Unrecognized entity in PayeeCellViewModel ConvertToDisplayText.");
        }

        protected override ObservableCollection<ResultCategoryViewModel> FilterResults()
        {
            var filteredAccounts = AccountSource.Where(a => a.Name.StartsWith(SearchText, StringComparison.CurrentCultureIgnoreCase)).ToList();
            List<Payee> filteredPayees = null;
            if (this.CellType == ColumnType.Transaction)
            {
                filteredPayees = PayeeSource.Where(p => p.Name.StartsWith(SearchText, StringComparison.CurrentCultureIgnoreCase)).ToList();
            }

            return new ObservableCollection<ResultCategoryViewModel>(ConvertToResultCategories(filteredAccounts, filteredPayees));

        }

        protected override EntityBase SearchTextSetValue(out bool shouldSetValue)
        {
            if (this.CellType == ColumnType.Transaction)
            {
                shouldSetValue = true;
                return new Payee() { Name = SearchText };
            }
            else
            {
                shouldSetValue = false;
                return null;
            }
        }
    }
}
