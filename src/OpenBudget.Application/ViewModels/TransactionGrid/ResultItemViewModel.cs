using OpenBudget.Model.Infrastructure.Entities;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Application.ViewModels.TransactionGrid
{
    public enum ResultItemType
    {
        Account,
        Payee,
        Category,
        IncomeCategory,
        SplitCategory
    }

    public class ResultItemViewModel : ViewModelBase
    {
        public ResultItemViewModel(string displayText, EntityBase referencedEntity, ResultItemType itemType)
        {
            _displayText = displayText;
            _referencedEntity = referencedEntity;
            _itemType = itemType;
        }

        private string _displayText;

        public string DisplayText
        {
            get { return _displayText; }
            set { _displayText = value; RaisePropertyChanged(); }
        }

        private EntityBase _referencedEntity;

        public EntityBase ReferencedEntity
        {
            get { return _referencedEntity; }
            set { _referencedEntity = value; RaisePropertyChanged(); }
        }

        private ResultItemType _itemType;

        public ResultItemType ItemType
        {
            get { return _itemType; }
            set { _itemType = value; }
        }
    }
}
