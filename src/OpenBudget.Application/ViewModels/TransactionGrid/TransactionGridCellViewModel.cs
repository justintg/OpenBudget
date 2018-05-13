using OpenBudget.Model.Entities;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.ViewModels.TransactionGrid
{
    public abstract class TransactionGridCellViewModel : ViewModelBase, IDisposable
    {
        public TransactionGridCellViewModel(TransactionGridColumnViewModel column, TransactionGridRowViewModel row, Transaction transaction)
        {
            _column = column;
            _row = row;
            _row.PropertyChanged += Row_PropertyChanged;
            _transaction = transaction;
            _isEditing = row.IsEditing;
        }

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

        private bool _isEditing;

        public bool IsEditing
        {
            get { return _isEditing; }
            internal set { _isEditing = value; RaisePropertyChanged(); }
        }

        public virtual void Dispose()
        {
            _row.PropertyChanged -= Row_PropertyChanged;
        }
    }

    public abstract class TransactionGridCellViewModel<T> : TransactionGridCellViewModel
    {
        protected TransactionGridColumnViewModel<T> InternalColumn { get; private set; }

        public TransactionGridCellViewModel(TransactionGridColumnViewModel<T> column, TransactionGridRowViewModel row, Transaction transaction) : base(column, row, transaction)
        {
            this.InternalColumn = column;
            this.Transaction.PropertyChanged += Transaction_PropertyChanged;
        }

        private void Transaction_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == this.Column.PropertyName && !_settingValue)
            {
                RaisePropertyChanged(nameof(Value));
            }
        }

        private bool _settingValue = false;

        public T Value
        {
            get
            {
                return InternalColumn.Getter(this.Transaction);
            }
            set
            {
                _settingValue = true;
                InternalColumn.Setter(this.Transaction, value);
                RaisePropertyChanged();
                _settingValue = false;
            }
        }

        public override void Dispose()
        {
            this.Transaction.PropertyChanged -= Transaction_PropertyChanged;
            base.Dispose();
        }
    }
}
