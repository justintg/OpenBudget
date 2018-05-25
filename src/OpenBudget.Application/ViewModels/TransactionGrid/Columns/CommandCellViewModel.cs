using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using OpenBudget.Model.Entities;

namespace OpenBudget.Application.ViewModels.TransactionGrid.Columns
{
    public class CommandCellViewModel : TransactionGridCellViewModel
    {
        public ICommand Command { get; private set; }

        public CommandCellViewModel(
            TransactionGridColumnViewModel column,
            TransactionGridRowViewModel row,
            Transaction transaction,
            Func<TransactionGridRowViewModel, ICommand> commandGetter)
            : base(column, row, transaction)
        {
            Command = commandGetter(row);
        }

        public CommandCellViewModel(
            TransactionGridColumnViewModel column,
            TransactionGridRowViewModel row,
            Transaction transaction,
            SubTransactionRowViewModel subTransactionRow,
            SubTransaction subTransaction,
            Func<SubTransactionRowViewModel, ICommand> commandGetter)
            : base(column, row, transaction, subTransactionRow, subTransaction)
        {
            Command = commandGetter(subTransactionRow);
        }
    }
}
