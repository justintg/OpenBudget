using OpenBudget.Model.Entities;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace OpenBudget.Application.ViewModels.TransactionGrid.Columns
{
    public class PayeeColumnViewModel : TransactionGridColumnViewModel<EntityBase>
    {
        ObservableCollection<Payee> _payeeSource;
        ObservableCollection<Account> _accountSource;
        Account _currentAccount;

        public PayeeColumnViewModel(
            Func<Transaction, EntityBase> getter,
            Action<Transaction, EntityBase> setter,
            string header,
            string propertyName,
            int width,
            Account currentAccount,
            ObservableCollection<Payee> payeeSource,
            ObservableCollection<Account> accountSource) :
                base(getter, setter, header, propertyName, width)
        {
            _payeeSource = payeeSource;
            _accountSource = accountSource;
            _currentAccount = currentAccount;
        }

        public PayeeColumnViewModel(
            Func<SubTransaction, EntityBase> getter,
            Action<SubTransaction, EntityBase> setter,
            string header,
            string propertyName,
            int width,
            Account currentAccount,
            ObservableCollection<Account> accountSource) :
                base(getter, setter, header, propertyName, width)
        {
            _accountSource = accountSource;
            _currentAccount = currentAccount;
        }

        public override TransactionGridCellViewModel CreateCell(TransactionGridRowViewModel row, Transaction transaction)
        {
            return new PayeeCellViewModel(this, row, transaction, _currentAccount, _payeeSource, _accountSource);
        }

        public override TransactionGridCellViewModel CreateCell(TransactionGridRowViewModel row, Transaction transaction, SubTransactionRowViewModel subTransactionRow, SubTransaction subTransaction)
        {
            return new PayeeCellViewModel(this, row, transaction, subTransactionRow, subTransaction, _currentAccount, _accountSource);
        }
    }
}
