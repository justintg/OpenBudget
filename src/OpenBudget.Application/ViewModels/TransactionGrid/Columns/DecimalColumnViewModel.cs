using System;
using System.Collections.Generic;
using System.Text;
using OpenBudget.Model.Entities;

namespace OpenBudget.Application.ViewModels.TransactionGrid.Columns
{
    public class DecimalColumnViewModel : TransactionGridColumnViewModel<decimal>
    {
        public DecimalColumnViewModel(Func<Transaction, decimal> getter, Action<Transaction, decimal> setter, string header, string properyName, int width) : base(getter, setter, header, properyName, width)
        {
        }

        public DecimalColumnViewModel(Func<SubTransaction, decimal> getter, Action<SubTransaction, decimal> setter, string header, string properyName, int width)
            : base(getter, setter, header, properyName, width)
        {
        }

        public override TransactionGridCellViewModel CreateCell(TransactionGridRowViewModel row, Transaction transaction)
        {
            return new DecimalCellViewModel(this, row, transaction);
        }

        public override TransactionGridCellViewModel CreateCell(TransactionGridRowViewModel row, Transaction transaction, SubTransactionRowViewModel subTransactionRow, SubTransaction subTransaction)
        {
            return new DecimalCellViewModel(this, row, transaction, subTransactionRow, subTransaction);
        }
    }
}
