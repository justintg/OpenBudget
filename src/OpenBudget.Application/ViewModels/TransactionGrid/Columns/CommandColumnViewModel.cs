using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using OpenBudget.Model.Entities;

namespace OpenBudget.Application.ViewModels.TransactionGrid.Columns
{
    public class CommandColumnViewModel : TransactionGridColumnViewModel
    {
        Func<TransactionGridRowViewModel, ICommand> _transactionCommandGetter;
        Func<SubTransactionRowViewModel, ICommand> _subTransactionCommandGetter;

        public CommandColumnViewModel(string header, int width, Func<TransactionGridRowViewModel, ICommand> transactionCommandGetter)
            : base(header, null, width)
        {
            _transactionCommandGetter = transactionCommandGetter;
        }

        public CommandColumnViewModel(string header, int width, Func<SubTransactionRowViewModel, ICommand> subTransactionCommandGetter)
            : base(header, null, width)
        {
            _subTransactionCommandGetter = subTransactionCommandGetter;
        }

        public override TransactionGridCellViewModel CreateCell(TransactionGridRowViewModel row, Transaction transaction)
        {
            return new CommandCellViewModel(this, row, transaction, _transactionCommandGetter);
        }

        public override TransactionGridCellViewModel CreateCell(TransactionGridRowViewModel row, Transaction transaction, SubTransactionRowViewModel subTransactionRow, SubTransaction subTransaction)
        {
            return new CommandCellViewModel(this, row, transaction, subTransactionRow, subTransaction, _subTransactionCommandGetter);
        }
    }
}
