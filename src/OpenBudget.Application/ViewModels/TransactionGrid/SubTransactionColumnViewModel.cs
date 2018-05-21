using GalaSoft.MvvmLight;
using OpenBudget.Model.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.ViewModels.TransactionGrid
{
    public abstract class SubTransactionColumnViewModel : ViewModelBase
    {
        public SubTransactionColumnViewModel(string propertyName, int width)
        {
            _propertyName = propertyName;
            _width = width;
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

        public abstract TransactionGridCellViewModel CreateCell(SubTransactionRowViewModel row, SubTransaction subTransaction);
    }

    public abstract class SubTransactionColumnViewModel<T> : SubTransactionColumnViewModel
    {
        public Func<SubTransaction, T> Getter { get; private set; }
        public Action<SubTransaction, T> Setter { get; private set; }

        public SubTransactionColumnViewModel(Func<SubTransaction, T> getter, Action<SubTransaction, T> setter, string header, string propertyName, int width) : base(propertyName, width)
        {
            this.Getter = getter;
            this.Setter = setter;
        }
    }
}
