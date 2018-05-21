using NUnit.Framework;
using OpenBudget.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Tests
{
    [TestFixture]
    public class SaveLoadTests
    {
        TestBudget TestBudget;

        [SetUp]
        public void Setup()
        {
            TestBudget = BudgetSetup.CreateBudget();
        }

        [TearDown]
        public void Teardown()
        {
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

            transaction = TestBudget.BudgetModel.FindEntity<Transaction>(transaction.EntityID);
            Assert.That(transaction, Is.Null);
            Assert.That(account.Transactions.Count, Is.EqualTo(0));
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

            Assert.That(transaction.Parent, Is.EqualTo(otherAccount));
            Assert.That(parent.Transactions, Has.No.Member(transaction));
            Assert.That(otherAccount.Transactions, Contains.Item(transaction));
        }
    }
}
