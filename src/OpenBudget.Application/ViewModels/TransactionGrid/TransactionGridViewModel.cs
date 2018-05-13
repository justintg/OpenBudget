using OpenBudget.Application.ViewModels.TransactionGrid.Columns;
using OpenBudget.Model;
using OpenBudget.Model.Entities;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace OpenBudget.Application.ViewModels.TransactionGrid
{
    public class TransactionGridViewModel : ViewModelBase, IDisposable
    {
        private BudgetModel _model;

        public TransactionGridViewModel(Account account)
        {
            _account = account;
            _model = account.Model;

            AddTransactionCommand = new RelayCommand(AddTransaction);

            InitializeColumns();
            InitializeGrid(_account);
        }

        private void InitializeColumns()
        {
            ObservableCollection<TransactionGridColumnViewModel> columns = new ObservableCollection<TransactionGridColumnViewModel>();
            columns.Add(new DateColumnViewModel(t => t.TransactionDate, (t, val) => { t.TransactionDate = val; }, "Date", nameof(Transaction.TransactionDate), 100));
            columns.Add(new PayeeColumnViewModel(t => t.PayeeOrAccount, (t, val) => { t.PayeeOrAccount = val; }, "Payee", nameof(Transaction.Payee), 300, _account, _model.Budget.Payees, _model.Budget.Accounts));
            columns.Add(new StringColumnViewModel(t => t.Memo, (t, val) => { t.Memo = val; }, "Memo", nameof(Transaction.Memo), 500));
            columns.Add(new DecimalColumnViewModel(t =>
            {
                if (t.Amount > 0) return t.Amount;
                else return 0;
            }, (t, val) => { t.Amount = val; }, "Inflow", nameof(Transaction.Amount), 100));
            columns.Add(new DecimalColumnViewModel(t =>
            {
                if (t.Amount < 0) return -t.Amount;
                else return 0;
            }, (t, val) => { t.Amount = -val; }, "Outflow", nameof(Transaction.Amount), 100));


            _columns = columns;
        }

        private void InitializeGrid(Account account)
        {
            if (_account != null)
            {
                _account.Transactions.CollectionChanged -= Transactions_CollectionChanged;
            }
            _account = account;
            _account.Transactions.CollectionChanged += Transactions_CollectionChanged;
            var rows = _account.Transactions.Select(t =>
            {
                var row = new TransactionGridRowViewModel(this.Columns, t, _model);
                row.PropertyChanged += Row_PropertyChanged;
                return row;

            }).OrderBy(r => r.Transaction.TransactionDate);
            Rows = new ObservableCollection<TransactionGridRowViewModel>(rows);
        }

        private void SortRows()
        {
            Rows = new ObservableCollection<TransactionGridRowViewModel>(Rows.OrderBy(r => r.Transaction.TransactionDate));
        }

        private void Transactions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Transaction transaction in e.NewItems)
                {
                    var row = new TransactionGridRowViewModel(this.Columns, transaction, _model);
                    row.PropertyChanged += Row_PropertyChanged;
                    Rows.Add(row);
                    SortRows();
                }
            }
            if (e.OldItems != null)
            {
                foreach (Transaction transaction in e.OldItems)
                {
                    var row = Rows.Where(r => r.Transaction == transaction).FirstOrDefault();
                    if (row != null)
                    {
                        Rows.Remove(row);
                        row.PropertyChanged -= Row_PropertyChanged;
                    }
                }
            }
        }

        private void Row_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TransactionGridRowViewModel.IsEditing))
            {
                var row = sender as TransactionGridRowViewModel;
                if (row.IsEditing)
                {
                    CurrentEditingTransaction = row;
                }
                else
                {
                    if (row == CurrentEditingTransaction)
                    {
                        CurrentEditingTransaction = null;
                    }
                }
            }

        }

        private Account _account;

        public Account Account
        {
            get { return _account; }
        }

        private double _width;

        public double Width
        {
            get { return _width; }
            set
            {
                _width = value; RaisePropertyChanged();
            }
        }

        private ObservableCollection<TransactionGridColumnViewModel> _columns;

        public ObservableCollection<TransactionGridColumnViewModel> Columns
        {
            get { return _columns; }
            private set { _columns = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<TransactionGridRowViewModel> _rows;

        public ObservableCollection<TransactionGridRowViewModel> Rows
        {
            get { return _rows; }
            set { _rows = value; RaisePropertyChanged(); }
        }

        private TransactionGridRowViewModel _currentEditingTransaction;

        public TransactionGridRowViewModel CurrentEditingTransaction
        {
            get { return _currentEditingTransaction; }
            private set
            {
                if (_currentEditingTransaction != null && _currentEditingTransaction != value)
                {
                    _currentEditingTransaction.CancelEditCommand.Execute(null);
                }
                _currentEditingTransaction = value;
                RaisePropertyChanged();
            }
        }

        private TransactionGridRowViewModel _currentAddingTransaction;

        public TransactionGridRowViewModel CurrentAddingTransaction
        {
            get { return _currentAddingTransaction; }
            private set
            {
                if (_currentAddingTransaction != null)
                    DisconnectAddingTransaction(_currentAddingTransaction);


                _currentAddingTransaction = value;

                if (_currentAddingTransaction != null)
                    ConnectAddingTransaction(_currentAddingTransaction);

                RaisePropertyChanged();
                RaisePropertyChanged(() => IsAdding);
            }
        }

        private void ConnectAddingTransaction(TransactionGridRowViewModel transaction)
        {
            transaction.TransactionAddFinished += OnTransactionAddFinished;
        }

        private void DisconnectAddingTransaction(TransactionGridRowViewModel transaction)
        {
            transaction.TransactionAddFinished -= OnTransactionAddFinished;
        }

        private void OnTransactionAddFinished(object sender, EventArgs e)
        {
            CurrentAddingTransaction = null;
        }

        public RelayCommand AddTransactionCommand { get; private set; }

        private void AddTransaction()
        {
            var addTransaction = new TransactionGridRowViewModel(Columns, this.Account, _model);
            CurrentAddingTransaction = addTransaction;
        }

        public virtual void Dispose()
        {
            _account.Transactions.CollectionChanged -= Transactions_CollectionChanged;
            foreach (var row in Rows)
            {
                row.Dispose();
            }
        }

        public bool IsAdding
        {
            get
            {
                return CurrentAddingTransaction != null;
            }
        }
    }
}
