using OpenBudget.Model.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.ViewModels.TransactionGrid.Columns
{
    public class StringColumnViewModel : TransactionGridColumnViewModel<string>
    {
        public StringColumnViewModel(Func<Transaction, string> getter, Action<Transaction, string> setter, string header, string properyName, int width) : base(getter, setter, header, properyName, width)
        {
        }

        public StringColumnViewModel(Func<SubTransaction, string> getter, Action<SubTransaction, string> setter, string header, string propertyName, int width) : base(getter, setter, header, propertyName, width)
        {
        }

        public override TransactionGridCellViewModel CreateCell(TransactionGridRowViewModel row, Transaction transaction)
        {
            return new StringCellViewModel(this, row, transaction);
        }

        public override TransactionGridCellViewModel CreateCell(TransactionGridRowViewModel row, Transaction transaction, SubTransactionRowViewModel subTransactionRow, SubTransaction subTransaction)
        {
            return new StringCellViewModel(this, row, transaction, subTransactionRow, subTransaction);
        }
    }
}
