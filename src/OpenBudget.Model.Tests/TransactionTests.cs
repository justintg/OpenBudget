using OpenBudget.Model.Entities;
using OpenBudget.Model.Events;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Tests
{
    [TestFixture]
    public class TransactionTests
    {
        TestBudget TestBudget;

        [SetUp]
        public void Setup()
        {
            TestBudget = BudgetSetup.CreateBudget();
        }

        [Test]
        public void CanDeleteTransaction()
        {
            Account account = TestBudget.Budget.Accounts[0];
            Transaction transaction = new Transaction() { Amount = 1000 };
            account.Transactions.Add(transaction);
            TestBudget.BudgetModel.SaveChanges();

            transaction.Delete();
            TestBudget.BudgetModel.SaveChanges();

            Assert.That(transaction.IsDeleted, Is.True);
            Assert.That(account.Transactions.Contains(transaction), Is.False);
        }

        [Test]
        public void CanDeleteSubTransactionBeforeSave()
        {
            Account Account = TestBudget.Budget.Accounts[0];
            Transaction transaction = new Transaction();
            transaction.Amount = 100;
            transaction.MakeSplitTransaction();
            Account.Transactions.Add(transaction);

            var subTransaction = transaction.SubTransactions.Create();
            subTransaction.Amount = 100;

            var subTransaction2 = transaction.SubTransactions.Create();
            subTransaction2.Amount = 100;

            subTransaction.Delete();
            Assert.That(transaction.SubTransactions.Count, Is.EqualTo(1));
            Assert.That(transaction.SubTransactions, Has.No.Member(subTransaction));
            Assert.That(transaction.SubTransactions, Contains.Item(subTransaction2));
        }

        [Test]
        public void CanDeleteSubTransactionAfterSave()
        {
            Account Account = TestBudget.Budget.Accounts[0];
            Transaction transaction = new Transaction();
            transaction.Amount = 100;
            transaction.MakeSplitTransaction();
            Account.Transactions.Add(transaction);

            var subTransaction = transaction.SubTransactions.Create();
            subTransaction.Amount = 50;

            var subTransaction2 = transaction.SubTransactions.Create();
            subTransaction2.Amount = 50;

            TestBudget.BudgetModel.SaveChanges();

            subTransaction.Delete();
            subTransaction2.Amount = 100;

            TestBudget.BudgetModel.SaveChanges();

            Assert.That(transaction.SubTransactions.Count, Is.EqualTo(1));
            Assert.That(transaction.SubTransactions, Has.No.Member(subTransaction));
            Assert.That(transaction.SubTransactions, Contains.Item(subTransaction2));
        }

        [Test]
        public void CanCancelDeleteTransaction()
        {
            Account account = TestBudget.Budget.Accounts[0];
            Transaction transaction = new Transaction() { Amount = 1000 };
            account.Transactions.Add(transaction);
            TestBudget.BudgetModel.SaveChanges();

            transaction.Delete();
            Assert.That(transaction.IsDeleted, Is.True);
            transaction.CancelCurrentChanges();
            TestBudget.BudgetModel.SaveChanges();

            Assert.That(transaction.IsDeleted, Is.False);
            Assert.That(account.Transactions.Contains(transaction), Is.True);
        }

        [Test]
        public void CreateSubTransaction()
        {
            Account Account = TestBudget.Budget.Accounts[0];
            Transaction transaction = new Transaction();
            transaction.MakeSplitTransaction();
            Account.Transactions.Add(transaction);

            var subTransaction = transaction.SubTransactions.Create();
            subTransaction.Amount = 100;

            TestBudget.BudgetModel.SaveChanges();
            Assert.That(TestBudget.TestEvents.Count, Is.EqualTo(1));

            GroupedFieldChangeEvent evt = TestBudget.TestEvents[0] as GroupedFieldChangeEvent;
            Assert.NotNull(evt);

            EntityCreatedEvent subTransactionCreatedEvent = evt.GroupedEvents[1] as EntityCreatedEvent;
            Assert.NotNull(subTransactionCreatedEvent);

            Assert.That(subTransactionCreatedEvent.EntityType, Is.EqualTo(nameof(SubTransaction)));
            Assert.That(subTransactionCreatedEvent.Changes[nameof(SubTransaction.Amount)].NewValue, Is.EqualTo(100));
            Assert.That(transaction.SubTransactions[0], Is.EqualTo(subTransaction));
        }

        [Test]
        public void SetCategoryMakesSplitTransacationNormal()
        {
            Account Account = TestBudget.Budget.Accounts[0];
            Transaction transaction = new Transaction();
            transaction.MakeSplitTransaction();
            Account.Transactions.Add(transaction);

            var subTransaction = transaction.SubTransactions.Create();
            subTransaction.Amount = 100;

            TestBudget.BudgetModel.SaveChanges();
            Assert.That(transaction.TransactionType, Is.EqualTo(TransactionTypes.SplitTransaction));
            Assert.That(transaction.SubTransactions.Count, Is.EqualTo(1));

            var category = TestBudget.Budget.IncomeCategories.GetIncomeCategory(DateTime.Today);
            transaction.Category = category;

            TestBudget.BudgetModel.SaveChanges();
            Assert.That(transaction.TransactionType, Is.EqualTo(TransactionTypes.Normal));
            Assert.That(transaction.SubTransactions.Count, Is.EqualTo(0));
        }

        [Test]
        public void CanCancelMakingTransactionSplit()
        {
            Account Account = TestBudget.Budget.Accounts[0];
            Transaction transaction = new Transaction();
            Account.Transactions.Add(transaction);
            transaction.Amount = 300;
            TestBudget.BudgetModel.SaveChanges();
            Assert.That(transaction.TransactionType, Is.EqualTo(TransactionTypes.Normal));

            transaction.MakeSplitTransaction();

            var subTransaction = transaction.SubTransactions.Create();
            subTransaction.Amount = 100;

            var subTransaction2 = transaction.SubTransactions.Create();
            subTransaction2.Amount = 200;

            transaction.CancelCurrentChanges();
            Assert.That(transaction.TransactionType, Is.EqualTo(TransactionTypes.Normal));
            Assert.That(transaction.SubTransactions.Count, Is.EqualTo(0));
        }

        [Test]
        public void Cancel_Changes_On_Transaction_Also_Cancels_Changes_On_SubTransactions()
        {
            Account Account = TestBudget.Budget.Accounts[0];
            Transaction transaction = new Transaction();
            Account.Transactions.Add(transaction);
            transaction.Amount = 100;

            transaction.MakeSplitTransaction();
            var subTransaction = transaction.SubTransactions.Create();
            subTransaction.Amount = 100;
            TestBudget.BudgetModel.SaveChanges();

            int eventCount = TestBudget.TestEvents.Count;
            subTransaction.Amount = 200;
            transaction.CancelCurrentChanges();
            TestBudget.BudgetModel.SaveChanges();

            Assert.That(TestBudget.TestEvents.Count, Is.EqualTo(eventCount));
            Assert.That(subTransaction.Amount, Is.EqualTo(100));
        }

        [Test]
        public void CannotCreateSubTransactionOnNormalTransaction()
        {
            Transaction transaction = new Transaction();
            Assert.That(transaction.TransactionType, Is.EqualTo(TransactionTypes.Normal));
            Assert.Throws<InvalidBudgetActionException>(() => transaction.SubTransactions.Create());
            Assert.That(transaction.SubTransactions.Count, Is.EqualTo(0));
        }

        [Test]
        public void NewTransactionIsNormal()
        {
            Transaction transaction = new Transaction();
            Assert.That(transaction.TransactionType, Is.EqualTo(TransactionTypes.Normal));
        }

        [Test]
        public void CanCreateTransfer()
        {
            Account parent = this.TestBudget.Budget.Accounts[0];
            Account transferAccount = this.TestBudget.Budget.Accounts[1];

            Transaction transfer = new Transaction();
            transfer.PayeeOrAccount = transferAccount;
            transfer.Amount = 100;
            transfer.Memo = "Test Memo";
            transfer.TransactionDate = DateTime.Today;

            parent.Transactions.Add(transfer);

            TestBudget.BudgetModel.SaveChanges();

            var otherTransaction = transferAccount.Transactions[0];

            Assert.That(TestBudget.TestEvents.Count, Is.EqualTo(2));
            Assert.That(transfer.TransactionType, Is.EqualTo(TransactionTypes.Transfer));
            Assert.That(otherTransaction.TransactionType, Is.EqualTo(TransactionTypes.Transfer));
            Assert.That(otherTransaction.TransferAccount, Is.EqualTo(parent));
            Assert.That(otherTransaction.Amount, Is.EqualTo(-transfer.Amount));
            Assert.That(otherTransaction.Memo, Is.EqualTo(transfer.Memo));
            Assert.That(otherTransaction.TransactionDate, Is.EqualTo(transfer.TransactionDate));
        }

        [Test]
        public void TransferStaysInSync()
        {
            Account parent = this.TestBudget.Budget.Accounts[0];
            Account transferAccount = this.TestBudget.Budget.Accounts[1];

            Transaction transfer = new Transaction();
            transfer.PayeeOrAccount = transferAccount;
            transfer.Amount = 100;
            transfer.Memo = "Test Memo";
            transfer.TransactionDate = DateTime.Today;

            parent.Transactions.Add(transfer);

            TestBudget.BudgetModel.SaveChanges();

            var otherTransaction = transferAccount.Transactions[0];

            otherTransaction.Amount = -110;
            otherTransaction.Memo = "New Memo";
            otherTransaction.TransactionDate = DateTime.Today.AddDays(1);
            TestBudget.BudgetModel.SaveChanges();

            Assert.That(otherTransaction.TransferAccount, Is.EqualTo(parent));
            Assert.That(otherTransaction.Amount, Is.EqualTo(-transfer.Amount));
            Assert.That(otherTransaction.Memo, Is.EqualTo(transfer.Memo));
            Assert.That(otherTransaction.TransactionDate, Is.EqualTo(transfer.TransactionDate));

            transfer.Amount = 100;
            transfer.Memo = "Test Memo";
            transfer.TransactionDate = DateTime.Today;
            TestBudget.BudgetModel.SaveChanges();

            Assert.That(otherTransaction.TransferAccount, Is.EqualTo(parent));
            Assert.That(otherTransaction.Amount, Is.EqualTo(-transfer.Amount));
            Assert.That(otherTransaction.Memo, Is.EqualTo(transfer.Memo));
            Assert.That(otherTransaction.TransactionDate, Is.EqualTo(transfer.TransactionDate));
        }

        [Test]
        public void MovingTransfersUpdatesOtherTransactionTransferAccount()
        {
            Account parent = this.TestBudget.Budget.Accounts[0];
            Account transferAccount = this.TestBudget.Budget.Accounts[1];
            Account otherAccount = this.TestBudget.Budget.Accounts[2];

            Transaction transfer = new Transaction();
            transfer.PayeeOrAccount = transferAccount;
            transfer.Amount = 100;
            transfer.Memo = "Test Memo";
            transfer.TransactionDate = DateTime.Today;

            parent.Transactions.Add(transfer);

            TestBudget.BudgetModel.SaveChanges();
            var otherTransaction = transferAccount.Transactions[0];

            otherAccount.Transactions.Add(transfer);
            TestBudget.BudgetModel.SaveChanges();

            Assert.That(otherTransaction.TransferAccount, Is.EqualTo(otherAccount));
        }

        [Test]
        public void PayeeTransferAccountProperty()
        {
            Payee payee = new Payee();
            payee.Name = "Test Payee";

            Account account = new Account();
            account.Name = "Test Account";

            Transaction transaction = new Transaction();
            transaction.TransferAccount = account;

            Assert.That(transaction.PayeeOrAccount, Is.EqualTo(account));
            Assert.That(transaction.TransferAccount, Is.EqualTo(account));
            Assert.That(transaction.Payee, Is.EqualTo(null));

            transaction.PayeeOrAccount = payee;
            Assert.That(transaction.PayeeOrAccount, Is.EqualTo(payee));
            Assert.That(transaction.TransferAccount, Is.EqualTo(null));
            Assert.That(transaction.Payee, Is.EqualTo(payee));
        }

        [Test]
        public void PayeeIsSavedByTransaction()
        {
            var account = TestBudget.Budget.Accounts[0];

            var payee = new Payee() { Name = "Test Payee" };

            Transaction transaction = new Transaction();
            transaction.Payee = payee;
            transaction.Amount = 100;
            transaction.TransactionDate = DateTime.Today;
            account.Transactions.Add(transaction);

            Assert.That(payee.IsAttached, Is.EqualTo(false));
            Assert.That(payee.Model, Is.EqualTo(null));

            TestBudget.BudgetModel.SaveChanges();

            Assert.That(payee.IsAttached, Is.EqualTo(true));
            Assert.That(payee.Model, Is.EqualTo(TestBudget.BudgetModel));
            Assert.That(TestBudget.Budget.Payees[0].EntityID, Is.EqualTo(payee.EntityID));
            Assert.That(TestBudget.Budget.Payees[0].Name, Is.EqualTo(payee.Name));
        }

        [Test]
        public void PayeeIsSavedBeforeTransaction()
        {
            var account = TestBudget.Budget.Accounts[0];

            var payee = new Payee() { Name = "Test Payee" };

            Transaction transaction = new Transaction();
            transaction.Payee = payee;
            transaction.Amount = 100;
            transaction.TransactionDate = DateTime.Today;
            account.Transactions.Add(transaction);

            TestBudget.BudgetModel.SaveChanges();

            Assert.That(TestBudget.TestEvents.Count, Is.EqualTo(2));
            Assert.That(TestBudget.TestEvents[0].EntityType, Is.EqualTo(nameof(Payee)));
            Assert.That(TestBudget.TestEvents[1].EntityType, Is.EqualTo(nameof(Transaction)));
        }

        [Test]
        public void PayeeDoesNotDuplicate()
        {
            var account = TestBudget.Budget.Accounts[0];

            var payee = new Payee() { Name = "Test Payee" };
            TestBudget.Budget.Payees.Add(payee);
            TestBudget.BudgetModel.SaveChanges();

            var duplicatePayee = new Payee() { Name = "Test Payee" };

            Transaction transaction = new Transaction();
            transaction.Payee = duplicatePayee;
            transaction.Amount = 100;
            transaction.TransactionDate = DateTime.Today;
            account.Transactions.Add(transaction);

            TestBudget.BudgetModel.SaveChanges();

            Assert.That(transaction.Payee, Is.EqualTo(payee));
        }

        [Test]
        public void TransactionCanMoveBetweenAccounts()
        {
            Account parent = this.TestBudget.Budget.Accounts[0];
            Account otherAccount = this.TestBudget.Budget.Accounts[1];

            Transaction transaction = new Transaction();
            transaction.Amount = 100;
            transaction.Memo = "Test Memo";
            transaction.TransactionDate = DateTime.Today;
            parent.Transactions.Add(transaction);

            TestBudget.BudgetModel.SaveChanges();

            Assert.That(transaction.Parent, Is.EqualTo(parent));
            Assert.That(parent.Transactions, Contains.Item(transaction));

            otherAccount.Transactions.Add(transaction);
            TestBudget.BudgetModel.SaveChanges();

            Assert.That(transaction.Parent, Is.EqualTo(otherAccount));
            Assert.That(parent.Transactions, Has.No.Member(transaction));
            Assert.That(otherAccount.Transactions, Contains.Item(transaction));
        }

        [Test]
        public void Can_Cancel_Move_Between_Accounts()
        {
            Account parent = this.TestBudget.Budget.Accounts[0];
            Account otherAccount = this.TestBudget.Budget.Accounts[1];

            Transaction transaction = new Transaction();
            transaction.Amount = 100;
            transaction.Memo = "Test Memo";
            transaction.TransactionDate = DateTime.Today;
            parent.Transactions.Add(transaction);

            TestBudget.BudgetModel.SaveChanges();
            TestBudget.TestEvents.Clear();

            Assert.That(transaction.Parent, Is.EqualTo(parent));
            Assert.That(parent.Transactions, Contains.Item(transaction));

            otherAccount.Transactions.Add(transaction);
            transaction.CancelCurrentChanges();
            TestBudget.BudgetModel.SaveChanges();

            Assert.That(transaction.Parent, Is.EqualTo(parent));
            Assert.That(parent.Transactions, Contains.Item(transaction));
            Assert.That(otherAccount.Transactions, Has.No.Member(transaction));
            Assert.That(TestBudget.TestEvents.Count, Is.EqualTo(0));
        }

        [TearDown]
        public void Teardown()
        {
            TestBudget = null;
        }
    }
}
