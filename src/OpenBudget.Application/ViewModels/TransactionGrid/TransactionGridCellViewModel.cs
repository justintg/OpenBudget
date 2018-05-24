using OpenBudget.Model.Entities;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Text;
using OpenBudget.Model.Infrastructure.Entities;

namespace OpenBudget.Application.ViewModels.TransactionGrid
{
    public abstract class TransactionGridCellViewModel : ViewModelBase, IDisposable
    {
        protected EntityBase _entity;

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
            _entity = transaction;
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
            _entity = subTransaction;
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
            _entity.PropertyChanged += Entity_PropertyChanged;
        }

        public TransactionGridCellViewModel(
            TransactionGridColumnViewModel<T> column,
            TransactionGridRowViewModel row,
            Transaction transaction,
             SubTransactionRowViewModel subTransactionRow,
            SubTransaction subTransaction) : base(column, row, transaction, subTransactionRow, subTransaction)
        {
            this.InternalColumn = column;
            _entity.PropertyChanged += Entity_PropertyChanged;
        }

        private void Entity_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
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
                return this.InternalColumn.Getter(_entity);
            }
            set
            {
                _settingValue = true;
                this.InternalColumn.Setter(_entity, value);
                RaisePropertyChanged();
                _settingValue = false;
            }
        }

        public override void Dispose()
        {
            _entity.PropertyChanged -= Entity_PropertyChanged;
            base.Dispose();
        }
    }
}
