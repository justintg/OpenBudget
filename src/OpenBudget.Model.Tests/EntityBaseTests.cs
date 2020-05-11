using OpenBudget.Model.Entities;
using OpenBudget.Model.Events;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Tests
{
    [TestFixture]
    public class EntityBaseTests
    {
        TestBudget TestBudget;
        BudgetModel BudgetModel;
        Budget Budget;
        Account Account;
        List<ModelEvent> TestEvents;

        [SetUp]
        public void Setup()
        {
            TestBudget = BudgetSetup.CreateBudget();
            BudgetModel = TestBudget.BudgetModel;
            Budget = TestBudget.Budget;
            Account = Budget.Accounts[0];
            TestEvents = TestBudget.EventStore.TestEvents;
        }

        /// <summary>
        /// The FieldChangeEvent should keep the previous value of a property since the last time
        /// SaveChanges was called so a CancelChanges or Undo can properly revert to the previous state.
        /// </summary>
        [Test]
        public void PropertyChangeStoresPreviousStateInEvent()
        {
            FieldChangeEvent currentEvent = Budget.CurrentEvent;

            string PreviousName = Budget.Name;

            Budget.Name = "Budget Name";

            Assert.That((string)currentEvent.Changes[nameof(Budget.Name)].PreviousValue, Is.EqualTo(PreviousName));

            Budget.Name = "New Budget Name";

            Assert.That((string)currentEvent.Changes[nameof(Budget.Name)].PreviousValue, Is.EqualTo(PreviousName));
        }

        [Test]
        public void EventsAreNotReplayed_OnEntityThatIsSourceOfTheEvent()
        {
            Assert.That(true, Is.False);
        }

        [TearDown]
        public void Teardown()
        {
            TestBudget = null;
            BudgetModel = null;
            Budget = null;
            Account = null;
            TestEvents = null;
        }
    }
}
