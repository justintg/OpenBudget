using OpenBudget.Model.Event;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using OpenBudget.Model.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Entities
{
    public enum TransactionTypes
    {
        None = 0,
        Normal = 1,
        Transfer = 2,
        SplitTransaction = 3,
        SplitOtherSide = 4
    }

    public class Transaction : EntityBase
    {
        public Transaction()
            : base(Guid.NewGuid().ToString())
        {
            SubTransactions = RegisterSubEntityCollection(new SubEntityCollection<SubTransaction>(this, SubTransactionInitializer));
            TransactionType = TransactionTypes.Normal;
        }

        internal Transaction(EntityCreatedEvent evt)
            : base(evt)
        {
            SubTransactions = RegisterSubEntityCollection(new SubEntityCollection<SubTransaction>(this, SubTransactionInitializer));
        }

        private SubTransaction SubTransactionInitializer()
        {
            if (TransactionType != TransactionTypes.SplitTransaction)
                throw new InvalidBudgetActionException("You cannot create a SubTransaction on a Transaction that is not a split transaction");

            return new SubTransaction();
        }

        public Payee Payee
        {
            get { return ResolveEntityReference<Payee>(nameof(PayeeOrAccount)); }
            set
            {
                PayeeOrAccount = value;
            }
        }

        public Account TransferAccount
        {
            get
            {
                return ResolveEntityReference<Account>(nameof(PayeeOrAccount));
            }
            set
            {
                PayeeOrAccount = value;
            }
        }

        public EntityBase PayeeOrAccount
        {
            get { return ResolveEntityReference<EntityBase>(); }
            set
            {
                IEnumerable<Type> ValidTypes = (new Type[] { typeof(Payee), typeof(Account) });
                if (value != null && !ValidTypes.Contains(value.GetType()))
                    throw new InvalidOperationException("Invalid type for setter on PayeeOrAcount Property");

                SetEntityReference(value);

                if (value == null || value is Payee)
                {
                    MakeNormalTransaction();
                }
                else if (value is Account)
                {
                    MakeTransfer(value as Account);
                }

                RaisePropertyChanged(nameof(Payee));
                RaisePropertyChanged(nameof(TransferAccount));
            }
        }

        public TransactionTypes TransactionType
        {
            get => GetProperty<TransactionTypes>();
            internal set => SetProperty(value);
        }

        public decimal Amount
        {
            get { return GetProperty<decimal>(); }
            set
            {
                if (this.TransactionType == TransactionTypes.SplitOtherSide)
                    throw new InvalidBudgetException("This transaction is part of a Split Transaction.  In order to change the Amount you must make the change on the Split Transaction.");

                SetProperty(value);
            }
        }

        protected void ForceSetProperty<T>(T value, [CallerMemberName]string property = null)
        {
            SetProperty(value, property);
        }

        public string Memo
        {
            get { return GetProperty<string>(); }
            set
            {
                SetProperty(value);
            }
        }

        public DateTime TransactionDate
        {
            get { return GetProperty<DateTime>(); }
            set
            {
                SetProperty(value);
            }
        }

        public BudgetSubCategory TransactionCategory
        {
            get { return ResolveEntityReference<BudgetSubCategory>("Category"); }
            set { Category = value; }
        }

        public IncomeCategory IncomeCategory
        {
            get { return ResolveEntityReference<IncomeCategory>("Category"); }
            set { Category = value; }
        }

        public EntityBase Category
        {
            get { return ResolveEntityReference<EntityBase>(); }
            set
            {
                IEnumerable<Type> ValidTypes = (new Type[] { typeof(IncomeCategory), typeof(BudgetSubCategory) });
                if (value != null && !ValidTypes.Contains(value.GetType()))
                    throw new InvalidOperationException("You must assign either a IncomeCategory or BudgetSubCategory to the Category Property!");

                SetEntityReference(value);
                RaisePropertyChanged(nameof(IncomeCategory));
                RaisePropertyChanged(nameof(TransactionCategory));
            }
        }

        internal Transaction OtherTransaction
        {
            get => ResolveEntityReference<Transaction>();
            set => SetEntityReference(value);
        }

        public SubEntityCollection<SubTransaction> SubTransactions { get; private set; }

        private void MakeTransfer(Account account)
        {
            if (this.TransactionType == TransactionTypes.Transfer)
                return; // Throw exception

            TransactionType = TransactionTypes.Transfer;
        }

        public void MakeNormalTransaction()
        {
            if (this.TransactionType == TransactionTypes.Normal)
                return; // Throw exception
        }

        public void MakeSplitTransaction()
        {
            TransactionType = TransactionTypes.SplitTransaction;
        }

        internal override void BeforeSaveChanges()
        {
            EnsurePayeeSaved();
            EnsureTransferSync();

            base.BeforeSaveChanges();
        }

        private void EnsureTransferSync()
        {
            if (this.TransactionType != TransactionTypes.Transfer || !this.HasChanges)
                return;

            Transaction otherTransaction = null;
            if (OtherTransaction != null)
            {
                otherTransaction = this.OtherTransaction;
            }
            else
            {
                otherTransaction = new Transaction();
                this.OtherTransaction = otherTransaction;

                otherTransaction.TransferAccount = this.Parent as Account;
                otherTransaction.OtherTransaction = this;
                this.TransferAccount.Transactions.Add(otherTransaction);
            }

            if (this.Parent != otherTransaction.TransferAccount)
                otherTransaction.TransferAccount = this.Parent as Account;

            if (otherTransaction.Amount != -this.Amount)
                otherTransaction.Amount = -this.Amount;

            if (otherTransaction.Memo != this.Memo)
                otherTransaction.Memo = this.Memo;

            if (otherTransaction.TransactionDate != this.TransactionDate)
                otherTransaction.TransactionDate = this.TransactionDate;
        }

        private void EnsurePayeeSaved()
        {
            if (!this.HasChanges) return;

            var payeeCollection = this.Model.Budget.Payees;
            if (this.Payee != null && !this.Payee.IsAttached)
            {
                var newPayee = this.Payee;
                newPayee.Name = newPayee.Name.Trim();

                var duplicatePayee = payeeCollection.Where(p => p.Name.Equals(newPayee.Name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (duplicatePayee != null)
                {
                    this.Payee = duplicatePayee;
                }
                else
                {
                    payeeCollection.Add(this.Payee);
                }
            }
        }
    }
}
