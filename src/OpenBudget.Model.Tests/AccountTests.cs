using NUnit.Framework;
using OpenBudget.Model.Entities;
using OpenBudget.Tests.Shared;
using System;

namespace OpenBudget.Model.Tests
{
    [TestFixture(BudgetBackends.Memory)]
    [TestFixture(BudgetBackends.SQLite)]
    public class AccountTests
    {
        private BudgetBackends _backend;
        TestBudget TestBudget;

        public AccountTests(BudgetBackends backend)
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
        public void AccountBalance_IsUpdatedAfterTransactionAdded()
        {
            var account = TestBudget.Budget.Accounts[0];
            var category = TestBudget.Budget.MasterCategories[0].Categories[0];
            Assert.That(account.Balance, Is.EqualTo(0M));

            Transaction transaction = new Transaction();
            transaction.TransactionDate = DateTime.Today;
            transaction.TransactionCategory = category;
            transaction.Amount = -100M;

            account.Transactions.Add(transaction);
            TestBudget.SaveChanges();

            Assert.That(account.Balance, Is.EqualTo(-100M));
        }

        [Test]
        public void AccountBalance_IsUpdatedAfterTransactionDeleted()
        {
            var account = TestBudget.Budget.Accounts[0];
            var category = TestBudget.Budget.MasterCategories[0].Categories[0];
            Assert.That(account.Balance, Is.EqualTo(0M));

            Transaction transaction = new Transaction();
            transaction.TransactionDate = DateTime.Today;
            transaction.TransactionCategory = category;
            transaction.Amount = -100M;

            account.Transactions.Add(transaction);
            TestBudget.SaveChanges();

            Assert.That(account.Balance, Is.EqualTo(-100M));

            transaction.Delete();
            TestBudget.SaveChanges();

            Assert.That(account.Balance, Is.EqualTo(0M));
        }

        [Test]
        public void AccountBalance_CopyOfAccountStartsWithCorrectBalance()
        {
            var account = TestBudget.Budget.Accounts[0];
            var category = TestBudget.Budget.MasterCategories[0].Categories[0];
            Assert.That(account.Balance, Is.EqualTo(0M));

            Transaction transaction = new Transaction();
            transaction.TransactionDate = DateTime.Today;
            transaction.TransactionCategory = category;
            transaction.Amount = -100M;

            account.Transactions.Add(transaction);
            TestBudget.SaveChanges();

            Account accountCopy = TestBudget.BudgetModel.FindEntity<Account>(account.EntityID);
            Assert.That(accountCopy.Balance, Is.EqualTo(-100M));
        }

        [Test]
        public void AccountBalance_MovingTransactionUpdatesBalanceOfBothAccounts()
        {
            var account = TestBudget.Budget.Accounts[0];
            var account2 = TestBudget.Budget.Accounts[1];
            var category = TestBudget.Budget.MasterCategories[0].Categories[0];

            Transaction transaction = new Transaction();
            transaction.TransactionDate = DateTime.Today;
            transaction.TransactionCategory = category;
            transaction.Amount = -100M;

            account.Transactions.Add(transaction);
            TestBudget.SaveChanges();

            Assert.That(account.Balance, Is.EqualTo(-100M));
            Assert.That(account2.Balance, Is.EqualTo(0M));

            account2.Transactions.Add(transaction);
            TestBudget.SaveChanges();

            Assert.That(account.Balance, Is.EqualTo(0M));
            Assert.That(account2.Balance, Is.EqualTo(-100M));
        }
    }
}
