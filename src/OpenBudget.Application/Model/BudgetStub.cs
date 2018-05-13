using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace OpenBudget.Application.Model
{
    [DataContract]
    public class BudgetStub : ViewModelBase
    {
        [DataMember]
        private string _budgetName;

        public string BudgetName
        {
            get { return _budgetName; }
            set { _budgetName = value; RaisePropertyChanged(); }
        }

        [DataMember]
        private string _budgetPath;

        public string BudgetPath
        {
            get { return _budgetPath; }
            set { _budgetPath = value; RaisePropertyChanged(); }
        }

        [DataMember]
        private DateTime _lastEdited;

        public DateTime LastEdited
        {
            get { return _lastEdited; }
            set { _lastEdited = value; RaisePropertyChanged(); }
        }

    }
}
