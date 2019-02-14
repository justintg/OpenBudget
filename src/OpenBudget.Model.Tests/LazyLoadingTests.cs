using NUnit.Framework;
using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Tests
{
    [TestFixture]
    public class LazyLoadingTests
    {
        TestBudget TestBudget;

        /*[SetUp]
        public void Setup()
        {
            TestBudget = BudgetSetup.CreateBudget();
        }*/

        [Test]
        public void InitialBudget_IsAttachedAfterCreate()
        {
            Budget initialBudget = new Budget();
            Account account = new Account() { Name = "Checking" };
            initialBudget.Accounts.Add(account);

            var model = BudgetModel.CreateNew(Guid.NewGuid(), new MemoryBudgetStore(), initialBudget);
            Assert.That(model.EventStore.GetEvents().Count(), Is.EqualTo(2));
            Assert.That(initialBudget.SaveState, Is.EqualTo(EntitySaveState.AttachedNoChanges));
            Assert.That(account.SaveState, Is.EqualTo(EntitySaveState.AttachedNoChanges));
        }
    }
}
