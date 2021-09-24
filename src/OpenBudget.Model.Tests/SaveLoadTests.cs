using NUnit.Framework;
using OpenBudget.Model.Entities;
using OpenBudget.Tests.Shared;
using System;
using System.Linq;

namespace OpenBudget.Model.Tests
{
    [TestFixture(BudgetBackends.Memory)]
    [TestFixture(BudgetBackends.SQLite)]
    public class SaveLoadTests
    {
        TestBudget TestBudget;
        private readonly BudgetBackends _backend;

        public SaveLoadTests(BudgetBackends backend)
        {
            _backend = backend;
        }

        [SetUp]
        public void Setup()
        {
            TestBudget = BudgetSetup.CreateBudget(_backend);
        }

        [TearDown]
        public void Teardown()
        {
            TestBudget?.BudgetModel?.Dispose();
            TestBudget = null;
        }

        [Test]
        public void On_Reload_SplitTransactions_Made_Normal_Have_SubTransactions_Cleared()
        {
            Account Account = TestBudget.Budget.Accounts[0];
            Transaction transaction = new Transaction();
            transaction.MakeSplitTransaction();
            Account.Transactions.Add(transaction);

            var subTransaction = transaction.SubTransactions.Create();
            subTransaction.Amount = 100;

            TestBudget.BudgetModel.SaveChanges();

            var category = TestBudget.Budget.IncomeCategories.GetIncomeCategory(DateTime.Today);
            transaction.Category = category;
            TestBudget.BudgetModel.SaveChanges();

            TestBudget.ReloadBudget();
            transaction = TestBudget.BudgetModel.FindEntity<Transaction>(transaction.EntityID);
            Assert.That(transaction.TransactionType, Is.EqualTo(TransactionTypes.Normal));
            Assert.That(transaction.SubTransactions.Count, Is.EqualTo(0));
        }

        [Test]
        public void On_Reload_Deleted_Transaction_Is_Deleted()
        {
            Account account = TestBudget.Budget.Accounts[0];
            Transaction transaction = new Transaction() { Amount = 1000 };
            account.Transactions.Add(transaction);
            TestBudget.BudgetModel.SaveChanges();

            transaction.Delete();
            TestBudget.BudgetModel.SaveChanges();
            TestBudget.ReloadBudget();

            account = TestBudget.BudgetModel.FindEntity<Account>(account.EntityID);
            account.Transactions.LoadCollection();
            transaction = TestBudget.BudgetModel.FindEntity<Transaction>(transaction.EntityID);
            Assert.That(transaction, Is.Null);
            Assert.That(account.Transactions.Count, Is.EqualTo(0));
        }

        [Test]
        public void On_Reload_Deleted_SubTransaction_IsDeleted()
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
            TestBudget.ReloadBudget();

            transaction = TestBudget.BudgetModel.FindEntity<Transaction>(transaction.EntityID);
            Assert.That(transaction.SubTransactions.Count, Is.EqualTo(1));
            Assert.That(transaction.SubTransactions[0].EntityID, Is.EqualTo(subTransaction2.EntityID));
            Assert.That(transaction.SubTransactions[0].Amount, Is.EqualTo(100));
        }

        [Test]
        public void On_Reload_Transaction_Moves_Between_Accounts()
        {
            Account parent = this.TestBudget.Budget.Accounts[0];
            Account otherAccount = this.TestBudget.Budget.Accounts[1];

            Transaction transaction = new Transaction();
            transaction.Amount = 100;
            transaction.Memo = "Test Memo";
            transaction.TransactionDate = DateTime.Today;
            parent.Transactions.Add(transaction);

            TestBudget.BudgetModel.SaveChanges();

            otherAccount.Transactions.Add(transaction);
            TestBudget.BudgetModel.SaveChanges();
            TestBudget.ReloadBudget();

            transaction = TestBudget.BudgetModel.FindEntity<Transaction>(transaction.EntityID);
            parent = TestBudget.BudgetModel.FindEntity<Account>(parent.EntityID);
            otherAccount = TestBudget.BudgetModel.FindEntity<Account>(otherAccount.EntityID);

            Assert.That(transaction.Parent.EntityID, Is.EqualTo(otherAccount.EntityID));

            parent.Transactions.LoadCollection();
            otherAccount.Transactions.LoadCollection();
            Assert.That(parent.Transactions.Select(t => t.EntityID), Has.No.Member(transaction.EntityID));
            Assert.That(otherAccount.Transactions.Select(t => t.EntityID), Contains.Item(transaction.EntityID));
        }
    }
}
