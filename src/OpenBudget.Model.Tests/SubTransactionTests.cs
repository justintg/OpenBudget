using NUnit.Framework;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Tests
{
    [TestFixture]
    public class SubTransactionTests
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
        public void CanChange_Attached_SubTransaction()
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
            TestBudget.ClearEvents();

            subTransaction.Amount = 100;

            TestBudget.BudgetModel.SaveChanges();

            Assert.That(TestBudget.TestEvents, Has.Count.EqualTo(1));
            Assert.That(TestBudget.TestEvents[0], Is.TypeOf<GroupedFieldChangeEvent>());

            GroupedFieldChangeEvent groupedFieldChangeEvent = (GroupedFieldChangeEvent)TestBudget.TestEvents[0];
            Assert.That(groupedFieldChangeEvent.GroupedEvents, Has.Count.EqualTo(1));
            Assert.That(groupedFieldChangeEvent.GroupedEvents[0].EntityType, Is.EqualTo(nameof(SubTransaction)));
            var change = groupedFieldChangeEvent.GroupedEvents[0].Changes[nameof(SubTransaction.Amount)];
            Assert.That(change.NewValue, Is.EqualTo(100M));
        }

        [Test]
        public void Can_Delete_SubTransaction()
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
            TestBudget.ClearEvents();

            subTransaction.Delete();

            TestBudget.BudgetModel.SaveChanges();

            Assert.That(TestBudget.TestEvents, Has.Count.EqualTo(1));
            Assert.That(TestBudget.TestEvents[0], Is.TypeOf<GroupedFieldChangeEvent>());

            GroupedFieldChangeEvent groupedFieldChangeEvent = (GroupedFieldChangeEvent)TestBudget.TestEvents[0];
            Assert.That(groupedFieldChangeEvent.GroupedEvents, Has.Count.EqualTo(1));
            Assert.That(groupedFieldChangeEvent.GroupedEvents[0].EntityType, Is.EqualTo(nameof(SubTransaction)));
            var change = groupedFieldChangeEvent.GroupedEvents[0].Changes[nameof(SubTransaction.IsDeleted)];
            Assert.That(change.NewValue, Is.True);

            Assert.That(transaction.SubTransactions, Has.No.Member(subTransaction));
        }
    }
}
