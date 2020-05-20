﻿using NUnit.Framework;
using OpenBudget.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Tests
{
    [TestFixture]
    public class AccountTests
    {
        TestBudget TestBudget;

        [SetUp]
        public void Setup()
        {
            TestBudget = BudgetSetup.CreateBudget();
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
