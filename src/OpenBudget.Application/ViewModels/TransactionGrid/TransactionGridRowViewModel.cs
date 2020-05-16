using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using OpenBudget.Model;
using OpenBudget.Model.Entities;
using OpenBudget.Util.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenBudget.Application.ViewModels.TransactionGrid
{
    public class TransactionGridRowViewModel : ViewModelBase, IDisposable
    {
        private BudgetModel _model;
        private Account _addToAccount;

        public TransactionGridRowViewModel(
            ObservableCollection<TransactionGridColumnViewModel> columns,
            ObservableCollection<TransactionGridColumnViewModel> subTransactionColumns,
            Transaction transaction,
            BudgetModel model)
        {
            _model = model;
            _columns = columns;
            _subTransactionColumns = subTransactionColumns;
            _transaction = transaction;
            _transaction.PropertyChanged += Transaction_PropertyChanged;

            InitializeSubTransactions();
            InitializeCommands();
            InitializeCells();
        }

        public TransactionGridRowViewModel(
            ObservableCollection<TransactionGridColumnViewModel> columns,
            ObservableCollection<TransactionGridColumnViewModel> subTransactionColumns,
            Account addToAccount,
            BudgetModel model)
        {
            _model = model;
            _columns = columns;
            _subTransactionColumns = subTransactionColumns;
            _transaction = new Transaction();
            _transaction.PropertyChanged += Transaction_PropertyChanged;
            _transaction.TransactionDate = DateTime.Today;
            _addToAccount = addToAccount;
            _isAdding = true;
            _isEditing = true;

            InitializeSubTransactions();
            InitializeCommands();
            InitializeCells();
        }

        private void Transaction_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Transaction.TransactionType))
            {
                RaisePropertyChanged(nameof(IsSplitTransaction));
                RaisePropertyChanged(nameof(ExpandSplitTransactions));
                AddSubTransactionCommand.RaiseCanExecuteChanged();
            }
        }

        private void InitializeSubTransactions()
        {
            _subTransactions = new TransformingObservableCollection<SubTransaction, SubTransactionRowViewModel>(_transaction.SubTransactions,
                (subTransaction) =>
                {
                    return new SubTransactionRowViewModel(SubTransactionColumns, this.Transaction, this, subTransaction);
                },
                (subTransaction) =>
                {
                    subTransaction.Dispose();
                });
        }

        private void InitializeCommands()
        {
            BeginEditCommand = new RelayCommand(BeginEdit);
            CancelEditCommand = new RelayCommand(CancelEdit);
            SaveCommand = new RelayCommand(Save);
            DeleteTransactionCommand = new RelayCommand(Delete);
            AddSubTransactionCommand = new RelayCommand(AddSubTransaction, CanAddSubTransaction);
        }

        private void InitializeCells()
        {
            List<TransactionGridCellViewModel> cells = Columns.Select(col => col.CreateCell(this, _transaction)).ToList();
            _cells = new ObservableCollection<TransactionGridCellViewModel>(cells);
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

        private ObservableCollection<TransactionGridCellViewModel> _cells;

        public ObservableCollection<TransactionGridCellViewModel> Cells
        {
            get { return _cells; }
            set { _cells = value; RaisePropertyChanged(); }
        }

        private TransformingObservableCollection<SubTransaction, SubTransactionRowViewModel> _subTransactions;

        public TransformingObservableCollection<SubTransaction, SubTransactionRowViewModel> SubTransactions
        {
            get { return _subTransactions; }
            set { _subTransactions = value; RaisePropertyChanged(); }
        }

        private Transaction _transaction;

        public Transaction Transaction
        {
            get { return _transaction; }
            set { _transaction = value; RaisePropertyChanged(); }
        }

        private bool _isEditing;

        public bool IsEditing
        {
            get { return _isEditing; }
            internal set
            {
                _isEditing = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ExpandSplitTransactions));
            }
        }

        private bool _isAdding;

        public bool IsAdding
        {
            get { return _isAdding; }
            private set
            {
                _isAdding = value;
                if (_isAdding && !IsEditing) IsEditing = true;
                RaisePropertyChanged();
            }
        }

        private bool _expandSplitTransactions;

        public bool ExpandSplitTransactions
        {
            get
            {
                if (!IsSplitTransaction) return false;
                if (IsEditing) return true;

                return _expandSplitTransactions;
            }
            set
            {
                _expandSplitTransactions = value;
                RaisePropertyChanged();
            }
        }

        public bool IsSplitTransaction
        {
            get
            {
                return Transaction.TransactionType == TransactionTypes.SplitTransaction;
            }
        }

        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                if (!_isSelected && _isEditing)
                {
                    CancelEditCommand.Execute(null);
                }
                RaisePropertyChanged();
            }
        }

        public RelayCommand BeginEditCommand { get; private set; }

        private void BeginEdit()
        {
            if (!IsEditing)
                IsEditing = true;
        }

        public RelayCommand CancelEditCommand { get; private set; }

        private void CancelEdit()
        {
            if (IsAdding)
            {
                RaiseTransactionAddFinished();
            }
            else if (IsEditing)
            {
                IsEditing = false;
                Transaction.Model.CancelChanges();
            }
        }

        public RelayCommand SaveCommand { get; private set; }

        private void Save()
        {
            if (IsAdding)
            {
                _addToAccount.Transactions.Add(Transaction);
                _model.SaveChanges();
                RaiseTransactionAddFinished();
            }
            else if (IsEditing)
            {
                _model.SaveChanges();
                IsEditing = false;
            }
        }

        public RelayCommand AddSubTransactionCommand { get; private set; }

        public bool CanAddSubTransaction()
        {
            return this.IsSplitTransaction;
        }

        private void AddSubTransaction()
        {
            this.Transaction.SubTransactions.Create();
        }

        public RelayCommand DeleteTransactionCommand { get; private set; }

        private void Delete()
        {
            this.Transaction.Delete();
            _model.SaveChanges();
        }

        public event EventHandler TransactionAddFinished;

        private void RaiseTransactionAddFinished()
        {
            TransactionAddFinished?.Invoke(this, EventArgs.Empty);
        }

        public virtual void Dispose()
        {
            SubTransactions?.Dispose();
            Transaction.PropertyChanged -= Transaction_PropertyChanged;
            foreach (var cell in Cells)
            {
                cell.Dispose();
            }
        }
    }
}
