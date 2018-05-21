using System;
using System.Collections.Generic;
using System.Text;
using OpenBudget.Model.Entities;

namespace OpenBudget.Application.ViewModels.TransactionGrid.Columns
{
    public class DecimalCellViewModel : TransactionGridCellViewModel<decimal>
    {
        public DecimalCellViewModel(TransactionGridColumnViewModel<decimal> column, TransactionGridRowViewModel row, Transaction transaction) : base(column, row, transaction)
        {
        }

        public DecimalCellViewModel(TransactionGridColumnViewModel<decimal> column, TransactionGridRowViewModel row, Transaction transaction, SubTransactionRowViewModel subTransactionRow, SubTransaction subTransaction)
            : base(column, row, transaction, subTransactionRow, subTransaction)
        {
        }
    }
}
