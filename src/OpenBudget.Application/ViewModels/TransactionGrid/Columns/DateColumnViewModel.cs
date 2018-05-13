using System;
using System.Collections.Generic;
using System.Text;
using OpenBudget.Model.Entities;

namespace OpenBudget.Application.ViewModels.TransactionGrid.Columns
{
    public class DateColumnViewModel : TransactionGridColumnViewModel<DateTime>
    {
        public DateColumnViewModel(Func<Transaction, DateTime> getter, Action<Transaction, DateTime> setter, string header, string properyName, int width) : base(getter, setter, header, properyName, width)
        {
        }

        public override TransactionGridCellViewModel CreateCell(TransactionGridRowViewModel row, Transaction transaction)
        {
            return new DateCellViewModel(this, row, transaction);
        }
    }
}
