using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace OpenBudget.Application.ViewModels.TransactionGrid
{
    public class ResultCategoryViewModel : ViewModelBase
    {
        public ResultCategoryViewModel(string header, IEnumerable<ResultItemViewModel> items)
        {
            _header = header;
            _items = new ObservableCollection<ResultItemViewModel>(items);
        }

        private string _header;

        public string Header
        {
            get { return _header; }
            private set { _header = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<ResultItemViewModel> _items;

        public ObservableCollection<ResultItemViewModel> Items
        {
            get { return _items; }
            private set { _items = value; RaisePropertyChanged(); }
        }

    }
}
