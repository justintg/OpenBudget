using NUnit.Framework;
using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Infrastructure.Entities;
using OpenBudget.Model.Infrastructure.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Tests
{
    /// <summary>
    /// Tests related to lazy loading entities and entity collections
    /// from the snapshot store, and coordinating save changes between different 
    /// copies of entities that exist.
    /// </summary>
    [TestFixture]
    public class LazyLoadingTests
    {
        TestBudget TestBudget;

        public static Budget CreateInitialBudget()
        {
            Budget initialBudget = new Budget();
            initialBudget.Name = "My Budget";
            Account account = new Account() { Name = "Checking" };
            initialBudget.Accounts.Add(account);

            return initialBudget;
        }
        /*[SetUp]
        public void Setup()
        {
            TestBudget = BudgetSetup.CreateBudget();
        }*/

        [Test]
        public void InitialBudget_IsAttachedAfterCreate()
        {
            Budget initialBudget = CreateInitialBudget();
            Account account = initialBudget.Accounts[0];

            var model = BudgetModel.CreateNew(Guid.NewGuid(), new MemoryBudgetStore(), initialBudget);
            Assert.That(model.EventStore.GetEvents().Count(), Is.EqualTo(2));
            Assert.That(initialBudget.SaveState, Is.EqualTo(EntitySaveState.AttachedNoChanges));
            Assert.That(account.SaveState, Is.EqualTo(EntitySaveState.AttachedNoChanges));
        }

        [Test]
        public void InitialBudget_And_GetBudget_AreDifferentCopies()
        {
            Budget initialBudget = CreateInitialBudget();

            var model = BudgetModel.CreateNew(Guid.NewGuid(), new MemoryBudgetStore(), initialBudget);

            var getBudget = model.GetBudget();
            Assert.That(getBudget.Name, Is.EqualTo(initialBudget.Name));

            getBudget.Name = "New Name";
            Assert.That(getBudget, Is.Not.EqualTo(initialBudget));
            Assert.That(getBudget.SaveState, Is.EqualTo(EntitySaveState.AttachedHasChanges));
            Assert.That(getBudget.Name, Is.Not.EqualTo(initialBudget.Name));
            Assert.That(initialBudget.SaveState, Is.EqualTo(EntitySaveState.AttachedNoChanges));
        }

        [Test]
        public void ChangingBudgetName_SetsStateTo_AttachedHasChanges()
        {
            Budget initialBudget = CreateInitialBudget();

            var model = BudgetModel.CreateNew(Guid.NewGuid(), new MemoryBudgetStore(), initialBudget);
            var getBudget = model.GetBudget();
            getBudget.Name = "New Name";

            Assert.That(getBudget.SaveState, Is.EqualTo(EntitySaveState.AttachedHasChanges));
        }

        [Test]
        public void ChangingBudgetName_RegistersEntityIn_UnitOfWork()
        {
            Budget initialBudget = CreateInitialBudget();

            var model = BudgetModel.CreateNew(Guid.NewGuid(), new MemoryBudgetStore(), initialBudget);
            var getBudget = model.GetBudget();
            getBudget.Name = "New Name";

            UnitOfWork unitOfWork = model.GetCurrentUnitOfWork();
            Assert.That(unitOfWork.ContainsEntity(getBudget), Is.True);
        }

        [Test]
        public void MultipleEntityCopies_ReceiveUpdates()
        {
            Budget initialBudget = CreateInitialBudget();

            var model = BudgetModel.CreateNew(Guid.NewGuid(), new MemoryBudgetStore(), initialBudget);
            var getBudget = model.GetBudget();
            getBudget.Name = "New Name";

            model.SaveChanges();
            Assert.That(initialBudget.Name, Is.EqualTo(getBudget.Name));
            Assert.That(initialBudget.SaveState, Is.EqualTo(EntitySaveState.AttachedNoChanges));
            Assert.That(getBudget.SaveState, Is.EqualTo(EntitySaveState.AttachedNoChanges));
        }

        [Test]
        public void ChildEntityCollection_Starts_Unattached()
        {
            Budget initialBudget = CreateInitialBudget();

            Assert.That(initialBudget.Accounts.CollectionState, Is.EqualTo(EntityCollectionState.Unattached));
        }

        [Test]
        public void ChildEntityCollection_IsAttachedLoaded_AfterCreate()
        {
            Budget initialBudget = CreateInitialBudget();

            var model = BudgetModel.CreateNew(Guid.NewGuid(), new MemoryBudgetStore(), initialBudget);
            Assert.That(initialBudget.Accounts.CollectionState, Is.EqualTo(EntityCollectionState.Attached));
            Assert.That(initialBudget.Accounts.IsLoaded, Is.True);
        }

        [Test]
        public void ChildEntityCollectionCopy_StartsUnloaded()
        {
            Budget initialBudget = CreateInitialBudget();

            var model = BudgetModel.CreateNew(Guid.NewGuid(), new MemoryBudgetStore(), initialBudget);
            var getBudget = model.GetBudget();

            Assert.That(getBudget.Accounts.CollectionState, Is.EqualTo(EntityCollectionState.Attached));
            Assert.That(getBudget.Accounts.IsLoaded, Is.False);
        }
    }
}
