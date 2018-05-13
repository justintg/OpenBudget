using OpenBudget.Model.Entities;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.ViewModels.TransactionGrid
{
    public abstract class TransactionGridColumnViewModel : ViewModelBase
    {
        public TransactionGridColumnViewModel(string header, string propertyName, int width)
        {
            _propertyName = propertyName;
            _header = header;
            _width = width;
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

        private int _width;

        public int Width
        {
            get { return _width; }
            set { _width = value; RaisePropertyChanged(); }
        }

        public abstract TransactionGridCellViewModel CreateCell(TransactionGridRowViewModel row, Transaction transaction);
    }

    public abstract class TransactionGridColumnViewModel<T> : TransactionGridColumnViewModel
    {
        public Func<Transaction, T> Getter { get; private set; }
        public Action<Transaction, T> Setter { get; private set; }

        public TransactionGridColumnViewModel(Func<Transaction, T> getter, Action<Transaction, T> setter, string header, string propertyName, int width) : base(header, propertyName, width)
        {
            this.Getter = getter;
            this.Setter = setter;
        }
    }
}
