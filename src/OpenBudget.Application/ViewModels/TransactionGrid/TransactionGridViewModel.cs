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
using OpenBudget.Application.Collections;

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
            columns.Add(new PayeeColumnViewModel(t => t.PayeeOrAccount, (t, val) => { t.PayeeOrAccount = val; }, "Payee", nameof(Transaction.Payee), 200, _account, _model.Budget.Payees, _model.Budget.Accounts));
            columns.Add(new CategoryColumnViewModel(t => t.Category, (t, val) => { t.Category = val; }, "Category", nameof(Transaction.Category), 200, _model.Budget.BudgetCategories, _model.Budget.IncomeCategories));
            columns.Add(new StringColumnViewModel(t => t.Memo, (t, val) => { t.Memo = val; }, "Memo", nameof(Transaction.Memo), 300));
            columns.Add(new DecimalColumnViewModel((Transaction t) =>
            {
                if (t.Amount > 0) return t.Amount;
                else return 0;
            }, (t, val) => { t.Amount = val; }, "Inflow", nameof(Transaction.Amount), 100));
            columns.Add(new DecimalColumnViewModel((Transaction t) =>
            {
                if (t.Amount < 0) return -t.Amount;
                else return 0;
            }, (t, val) => { t.Amount = -val; }, "Outflow", nameof(Transaction.Amount), 100));

            _columns = columns;

            InitializeSubTransactionColumns();
        }

        private void InitializeSubTransactionColumns()
        {
            ObservableCollection<TransactionGridColumnViewModel> subTransactionColumns = new ObservableCollection<TransactionGridColumnViewModel>();
            _subTransactionColumns = subTransactionColumns;
            subTransactionColumns.Add(new DecimalColumnViewModel((SubTransaction t) =>
            {
                if (t.Amount > 0) return t.Amount;
                else return 0;
            }, (t, val) => { t.Amount = val; }, "Inflow", nameof(Transaction.Amount), 100));
            _subTransactionColumns[0].MarginLeft = 800;
            subTransactionColumns.Add(new DecimalColumnViewModel((SubTransaction t) =>
            {
                if (t.Amount < 0) return -t.Amount;
                else return 0;
            }, (t, val) => { t.Amount = -val; }, "Outflow", nameof(Transaction.Amount), 100));
        }

        private void InitializeGrid(Account account)
        {
            if (_account != null && Rows != null)
            {
                Rows.Dispose();
            }
            _account = account;
            Rows = new TransformingObservableCollection<Transaction, TransactionGridRowViewModel>(
                _account.Transactions,
                (transaction) =>
                {
                    var row = new TransactionGridRowViewModel(this.Columns, this.SubTransactionColumns, transaction, _model);
                    row.PropertyChanged += Row_PropertyChanged;
                    return row;
                },
                (transformed) =>
                {
                    transformed.Dispose();
                    transformed.PropertyChanged -= Row_PropertyChanged;
                });

            SortRows();
        }

        private void SortRows()
        {
            Rows.Sort(t => t.Transaction.TransactionDate);
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

        private ObservableCollection<TransactionGridColumnViewModel> _subTransactionColumns;

        public ObservableCollection<TransactionGridColumnViewModel> SubTransactionColumns
        {
            get { return _subTransactionColumns; }
            private set { _subTransactionColumns = value; RaisePropertyChanged(); }
        }

        private TransformingObservableCollection<Transaction, TransactionGridRowViewModel> _rows;

        public TransformingObservableCollection<Transaction, TransactionGridRowViewModel> Rows
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
            transaction.Dispose();
        }

        private void OnTransactionAddFinished(object sender, EventArgs e)
        {
            CurrentAddingTransaction = null;
        }

        public RelayCommand AddTransactionCommand { get; private set; }

        private void AddTransaction()
        {
            var addTransaction = new TransactionGridRowViewModel(Columns, this.SubTransactionColumns, this.Account, _model);
            CurrentAddingTransaction = addTransaction;
        }

        public virtual void Dispose()
        {
            Rows.Dispose();
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
