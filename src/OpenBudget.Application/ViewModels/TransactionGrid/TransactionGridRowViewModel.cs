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
    public class TransactionGridRowViewModel : ViewModelBase, IDisposable
    {
        private BudgetModel _model;
        private Account _addToAccount;

        public TransactionGridRowViewModel(ObservableCollection<TransactionGridColumnViewModel> columns, Transaction transaction, BudgetModel model)
        {
            _model = model;
            _columns = columns;
            _transaction = transaction;

            InitializeCommands();
            InitializeCells();
        }

        public TransactionGridRowViewModel(ObservableCollection<TransactionGridColumnViewModel> columns, Account addToAccount, BudgetModel model)
        {
            _model = model;
            _columns = columns;
            _transaction = new Transaction();
            _transaction.TransactionDate = DateTime.Today;
            _addToAccount = addToAccount;
            _isAdding = true;
            _isEditing = true;

            InitializeCommands();
            InitializeCells();
        }

        private void InitializeCommands()
        {
            BeginEditCommand = new RelayCommand(BeginEdit);
            CancelEditCommand = new RelayCommand(CancelEdit);
            SaveCommand = new RelayCommand(Save);
            DeleteTransactionCommand = new RelayCommand(Delete);
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

        private ObservableCollection<TransactionGridCellViewModel> _cells;

        public ObservableCollection<TransactionGridCellViewModel> Cells
        {
            get { return _cells; }
            set { _cells = value; RaisePropertyChanged(); }
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
                Transaction.CancelCurrentChanges();
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
            foreach (var cell in Cells)
            {
                cell.Dispose();
            }
        }
    }
}
