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
    }
}
