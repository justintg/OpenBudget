using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using OpenBudget.Model.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace OpenBudget.Application.ViewModels.TransactionGrid
{
    public class SubTransactionRowViewModel : ViewModelBase, IDisposable
    {
        private Transaction _transaction;
        private TransactionGridRowViewModel _parentRow;
        private SubTransaction _subTransaction;

        public SubTransactionRowViewModel(ObservableCollection<TransactionGridColumnViewModel> columns,
            Transaction transaction,
            TransactionGridRowViewModel row,
            SubTransaction subTransaction)
        {
            _columns = columns;
            _transaction = transaction;
            _parentRow = row;
            _subTransaction = subTransaction;

            InitializeCommands();
            InitializeCells();
        }

        private void InitializeCells()
        {
            List<TransactionGridCellViewModel> cells = Columns.Select(col => col.CreateCell(_parentRow, _transaction, this, _subTransaction)).ToList();
            _cells = new ObservableCollection<TransactionGridCellViewModel>(cells);
        }

        private void InitializeCommands()
        {
            DeleteSubTransactionCommand = new RelayCommand(DeleteSubTransaction);
        }

        public RelayCommand DeleteSubTransactionCommand { get; private set; }

        private void DeleteSubTransaction()
        {
            _subTransaction.Delete();
        }

        private ObservableCollection<TransactionGridColumnViewModel> _columns;

        public ObservableCollection<TransactionGridColumnViewModel> Columns
        {
            get { return _columns; }
            private set { _columns = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<TransactionGridCellViewModel> _cells;

        public ObservableCollection<TransactionGridCellViewModel> Cells
        {
            get { return _cells; }
            set { _cells = value; RaisePropertyChanged(); }
        }

        public void Dispose()
        {
            foreach (var cell in Cells)
            {
                cell.Dispose();
            }
        }
    }
}
