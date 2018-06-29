using OpenBudget.Model.Entities;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Text;
using OpenBudget.Model.Infrastructure.Entities;

namespace OpenBudget.Application.ViewModels.TransactionGrid
{
    public enum ColumnType
    {
        Transaction,
        SubTransaction
    }

    public abstract class TransactionGridColumnViewModel : ViewModelBase
    {
        public TransactionGridColumnViewModel(string header, string propertyName, double desiredWidth)
        {
            _propertyName = propertyName;
            _header = header;
            _desiredWidth = desiredWidth;
            _width = desiredWidth;
            _marginLeft = 0;
        }

        private string _header;

        public string Header
        {
            get { return _header; }
            set { _header = value; RaisePropertyChanged(); }
        }

        private string _propertyName;

        public string PropertyName
        {
            get { return _propertyName; }
            private set { _propertyName = value; RaisePropertyChanged(); }
        }

        private double _marginLeft;

        public double MarginLeft
        {
            get { return _marginLeft; }
            set { _marginLeft = value; RaisePropertyChanged(); }
        }

        private double _width;

        public double Width
        {
            get { return _width; }
            set { _width = value; RaisePropertyChanged(); }
        }

        private double _desiredWidth;

        public double DesiredWidth
        {
            get { return _desiredWidth; }
            private set { _desiredWidth = value; RaisePropertyChanged(); }
        }


        public abstract TransactionGridCellViewModel CreateCell(
            TransactionGridRowViewModel row,
            Transaction transaction);

        public abstract TransactionGridCellViewModel CreateCell(
            TransactionGridRowViewModel row,
            Transaction transaction,
            SubTransactionRowViewModel subTransactionRow,
            SubTransaction subTransaction);
    }

    public abstract class TransactionGridColumnViewModel<T> : TransactionGridColumnViewModel
    {
        public ColumnType ColumnType { get; private set; }
        public Func<EntityBase, T> Getter { get; private set; }
        public Action<EntityBase, T> Setter { get; private set; }

        public TransactionGridColumnViewModel(
            Func<Transaction, T> getter,
            Action<Transaction, T> setter,
            string header,
            string propertyName,
            int width)
            : base(header, propertyName, width)
        {
            this.Getter = (entity) => getter((Transaction)entity);
            this.Setter = (entity, value) => setter((Transaction)entity, value);
            ColumnType = ColumnType.Transaction;
        }

        public TransactionGridColumnViewModel(
            Func<SubTransaction, T> getter,
            Action<SubTransaction, T> setter,
            string header,
            string propertyName,
            int width)
            : base(header, propertyName, width)
        {
            this.Getter = (entity) => getter((SubTransaction)entity);
            this.Setter = (entity, value) => setter((SubTransaction)entity, value);
            ColumnType = ColumnType.SubTransaction;
        }
    }
}
