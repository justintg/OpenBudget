using System;
using System.Collections.Generic;
using System.Text;
using OpenBudget.Model.Entities;

namespace OpenBudget.Application.ViewModels.TransactionGrid.Columns
{
    public class DateCellViewModel : TransactionGridCellViewModel<DateTime>
    {
        public DateCellViewModel(TransactionGridColumnViewModel<DateTime> column, TransactionGridRowViewModel row, Transaction transaction) : base(column, row, transaction)
        {
        }
    }
}
