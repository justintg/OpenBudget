using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using OpenBudget.Application.ViewModels.TransactionGrid.Columns;
using OpenBudget.Model;
using OpenBudget.Model.Entities;
using OpenBudget.Util.Collections;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenBudget.Application.ViewModels.TransactionGrid
{
    public class TransactionGridViewModel : ViewModelBase, IDisposable
    {
        private const double MAX_MEMO_COLUMN_WIDTH = 400.0;
        private BudgetModel _model;
        private Budget _budget;
        private double _visibleWidth;

        public TransactionGridViewModel(BudgetModel model, string accountId)
        {
            _model = model;
            _account = _model.FindEntity<Account>(accountId);
            if (_account == null)
                throw new ArgumentException("Cannot find Account in BudgetModel", nameof(accountId));

            //Load collections for use in Payee/Category lookup
            _budget = _account.GetParentBudget();
            _budget.Accounts.LoadCollection();
            _budget.MasterCategories.LoadCollection();
            _budget.Payees.LoadCollection();

            AddTransactionCommand = new RelayCommand(AddTransaction);

            InitializeColumns();
            InitializeGrid(_account);
        }

        public void SetVisibleWidth(double width)
        {
            if (_visibleWidth == width)
                return;

            _visibleWidth = width;
            ResizeColumns();
        }

        private void ResizeColumns()
        {
            var memoColumn = FindColumn("Memo");

            double totalDesiredWidth = _columns.Sum(c => c.DesiredWidth);
            double difference = _visibleWidth - totalDesiredWidth;
            if ((memoColumn.DesiredWidth + difference) > memoColumn.DesiredWidth)
            {
                double memoColumnWidth = memoColumn.DesiredWidth + difference;
                double widthAdjustment = 0.0;
                if (memoColumnWidth > MAX_MEMO_COLUMN_WIDTH)
                {
                    widthAdjustment = (memoColumnWidth - MAX_MEMO_COLUMN_WIDTH) / _columns.Count;
                    memoColumnWidth = MAX_MEMO_COLUMN_WIDTH;
                }
                foreach (var column in _columns)
                {
                    if (column == memoColumn) continue;
                    column.Width = column.DesiredWidth + widthAdjustment;
                }
                memoColumn.Width = memoColumnWidth + widthAdjustment;
            }
            else
            {
                int numberColumns = _columns.Count;
                double widthAdjustment = difference / numberColumns;
                foreach (var column in _columns)
                {
                    column.Width = column.DesiredWidth + widthAdjustment;
                }
            }
            SetSubtransactionColumnWidths();
        }

        private void SetSubtransactionColumnWidths()
        {
            var dateColumn = FindColumn("Date");
            var payeeColumn = FindColumn("Payee");
            var categoryColumn = FindColumn("Category");
            var memoColumn = FindColumn("Memo");
            var inflowColumn = FindColumn("Inflow");
            var outflowColumn = FindColumn("Outflow");

            var deleteSubColumn = FindSubColumn("Delete");
            var payeeSubColumn = FindSubColumn("Payee");
            var categorySubColumn = FindSubColumn("Category");
            var memoSubColumn = FindSubColumn("Memo");
            var inflowSubColumn = FindSubColumn("Inflow");
            var outflowSubColumn = FindSubColumn("Outflow");

            deleteSubColumn.MarginLeft = dateColumn.Width - deleteSubColumn.Width;
            payeeSubColumn.Width = payeeColumn.Width;
            categorySubColumn.Width = categoryColumn.Width;
            memoSubColumn.Width = memoColumn.Width;
            inflowSubColumn.Width = inflowColumn.Width;
            outflowSubColumn.Width = outflowColumn.Width;
        }

        private TransactionGridColumnViewModel FindColumn(string header)
        {
            return _columns.Where(c => c.Header == header).Single();
        }

        private TransactionGridColumnViewModel FindSubColumn(string header)
        {
            return _subTransactionColumns.Where(c => c.Header == header).Single();
        }

        private void InitializeColumns()
        {
            ObservableCollection<TransactionGridColumnViewModel> columns = new ObservableCollection<TransactionGridColumnViewModel>();
            columns.Add(new DateColumnViewModel(t => t.TransactionDate, (t, val) => { t.TransactionDate = val; }, "Date", nameof(Transaction.TransactionDate), 100));
            columns.Add(new PayeeColumnViewModel(t => t.PayeeOrAccount, (t, val) => { t.PayeeOrAccount = val; }, "Payee", nameof(Transaction.Payee), 200, _account, _budget.Payees, _budget.Accounts));
            columns.Add(new CategoryColumnViewModel((Transaction t) => t.Category, (t, val) => { t.Category = val; }, "Category", nameof(Transaction.Category), 200, _budget.MasterCategories, _budget.IncomeCategories));
            columns.Add(new StringColumnViewModel((Transaction t) => t.Memo, (t, val) => { t.Memo = val; }, "Memo", nameof(Transaction.Memo), 150));
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

            subTransactionColumns.Add(new CommandColumnViewModel("Delete", 10, (st) => st.DeleteSubTransactionCommand));
            subTransactionColumns.Add(new PayeeColumnViewModel((SubTransaction t) => t.TransferAccount, (t, val) => { t.TransferAccount = val as Account; }, "Payee", nameof(SubTransaction.TransferAccount), 200, _account, _budget.Accounts));
            subTransactionColumns.Add(new CategoryColumnViewModel((SubTransaction t) => t.Category, (t, val) => { t.Category = val; }, "Category", nameof(SubTransaction.Category), 200, _budget.MasterCategories, _budget.IncomeCategories));
            subTransactionColumns.Add(new StringColumnViewModel((SubTransaction t) => t.Memo, (t, val) => { t.Memo = val; }, "Memo", nameof(SubTransaction.Memo), 300));
            subTransactionColumns.Add(new DecimalColumnViewModel((SubTransaction t) =>
            {
                if (t.Amount > 0) return t.Amount;
                else return 0;
            }, (t, val) => { t.Amount = val; }, "Inflow", nameof(SubTransaction.Amount), 100));
            subTransactionColumns.Add(new DecimalColumnViewModel((SubTransaction t) =>
            {
                if (t.Amount < 0) return -t.Amount;
                else return 0;
            }, (t, val) => { t.Amount = -val; }, "Outflow", nameof(SubTransaction.Amount), 100));

            _subTransactionColumns[0].MarginLeft = 90;
        }

        private void InitializeGrid(Account account)
        {
            if (_account != null && Rows != null)
            {
                Rows.Dispose();
            }
            _account = account;
            _account.Transactions.EnsureCollectionLoaded();
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
