﻿using OpenBudget.Model.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.ViewModels.TransactionGrid.Columns
{
    public class StringCellViewModel : TransactionGridCellViewModel<string>
    {
        private StringColumnViewModel _stringColumn;

        public StringCellViewModel(StringColumnViewModel column, TransactionGridRowViewModel row, Transaction transaction) : base(column, row, transaction)
        {
            _stringColumn = column;
        }

        public StringCellViewModel(StringColumnViewModel column, TransactionGridRowViewModel row, Transaction transaction, SubTransactionRowViewModel subTransactionRow, SubTransaction subTransaction) : base(column, row, transaction, subTransactionRow, subTransaction)
        {
            _stringColumn = column;
        }
    }
}
