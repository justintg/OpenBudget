using OpenBudget.Model.Entities;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.ViewModels.TransactionGrid
{
    public abstract class TransactionGridCellViewModel : ViewModelBase, IDisposable
    {
        public TransactionGridCellViewModel(
            TransactionGridColumnViewModel column,
            TransactionGridRowViewModel row,
            Transaction transaction)
        {
            _column = column;
            _row = row;
            _row.PropertyChanged += Row_PropertyChanged;
            _transaction = transaction;
            _isEditing = row.IsEditing;
            CellType = ColumnType.Transaction;
        }

        public TransactionGridCellViewModel(
            TransactionGridColumnViewModel column,
            TransactionGridRowViewModel row,
            Transaction transaction,
            SubTransactionRowViewModel subTransactionRow,
            SubTransaction subTransaction)
        {
            _column = column;
            _row = row;
            _row.PropertyChanged += Row_PropertyChanged;
            _transaction = transaction;
            _subTransaction = subTransaction;
            _subTransactionRow = subTransactionRow;
            _isEditing = row.IsEditing;
            CellType = ColumnType.SubTransaction;
        }

        public ColumnType CellType { get; private set; }

        private void Row_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TransactionGridRowViewModel.IsEditing))
            {
                if (_row.IsEditing)
                {
                    this.IsEditing = true;
                }
                else
                {
                    this.IsEditing = false;
                }
            }
        }

        protected TransactionGridCellViewModel(TransactionGridColumnViewModel column)
        {
            Column = column;
        }

        private Transaction _transaction;

        public Transaction Transaction
        {
            get { return _transaction; }
            private set { _transaction = value; RaisePropertyChanged(); }
        }

        private TransactionGridColumnViewModel _column;

        public TransactionGridColumnViewModel Column
        {
            get { return _column; }
            private set { _column = value; RaisePropertyChanged(); }
        }

        private TransactionGridRowViewModel _row;

        public TransactionGridRowViewModel Row
        {
            get { return _row; }
            private set { _row = value; RaisePropertyChanged(); }
        }

        private SubTransactionRowViewModel _subTransactionRow;

        public SubTransactionRowViewModel SubTransactionRow
        {
            get { return _subTransactionRow; }
            private set { _subTransactionRow = value; RaisePropertyChanged(); }
        }

        private SubTransaction _subTransaction;

        public SubTransaction SubTransaction
        {
            get { return _subTransaction; }
            private set { _subTransaction = value; RaisePropertyChanged(); }
        }

        private bool _isEditing;

        public bool IsEditing
        {
            get { return _isEditing; }
            internal set
            {
                bool wasEditing = _isEditing;
                _isEditing = value;

                if (_isEditing && !wasEditing)
                {
                    OnBeginEdit();
                }

                if (wasEditing && !_isEditing)
                {
                    OnEndEdit();
                }
                RaisePropertyChanged();
            }
        }

        protected virtual void OnBeginEdit()
        {

        }

        protected virtual void OnEndEdit()
        {

        }

        public virtual void Dispose()
        {
            _row.PropertyChanged -= Row_PropertyChanged;
        }
    }

    public abstract class TransactionGridCellViewModel<T> : TransactionGridCellViewModel
    {
        protected TransactionGridColumnViewModel<T> InternalColumn { get; private set; }

        public TransactionGridCellViewModel(
            TransactionGridColumnViewModel<T> column,
            TransactionGridRowViewModel row,
            Transaction transaction) : base(column, row, transaction)
        {
            this.InternalColumn = column;
            this.Transaction.PropertyChanged += Transaction_PropertyChanged;
        }

        public TransactionGridCellViewModel(
            TransactionGridColumnViewModel<T> column,
            TransactionGridRowViewModel row,
            Transaction transaction,
             SubTransactionRowViewModel subTransactionRow,
            SubTransaction subTransaction) : base(column, row, transaction, subTransactionRow, subTransaction)
        {
            this.InternalColumn = column;
            this.SubTransaction.PropertyChanged += Transaction_PropertyChanged;
        }

        private void Transaction_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == this.Column.PropertyName && !_settingValue)
            {
                RaisePropertyChanged(nameof(Value));
                OnValueChanged();
            }
        }

        protected virtual void OnValueChanged()
        {

        }

        private bool _settingValue = false;

        public T Value
        {
            get
            {
                if (this.CellType == ColumnType.SubTransaction)
                    return InternalColumn.SubTransactionGetter(this.SubTransaction);
                else
                    return InternalColumn.TransactionGetter(this.Transaction);
            }
            set
            {
                _settingValue = true;
                if (this.CellType == ColumnType.SubTransaction)
                    InternalColumn.SubTransactionSetter(this.SubTransaction, value);
                else
                    InternalColumn.TransactionSetter(this.Transaction, value);
                RaisePropertyChanged();
                _settingValue = false;
            }
        }

        public override void Dispose()
        {
            if (this.CellType == ColumnType.Transaction)
                this.Transaction.PropertyChanged -= Transaction_PropertyChanged;
            else
                this.SubTransaction.PropertyChanged -= Transaction_PropertyChanged;
            base.Dispose();
        }
    }
}
