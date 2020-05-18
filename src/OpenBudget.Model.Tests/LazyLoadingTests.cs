﻿using NUnit.Framework;
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

        [Test]
        public void UnloadedEntityCollectionThrowsException()
        {
            Budget initialBudget = CreateInitialBudget();

            var model = BudgetModel.CreateNew(Guid.NewGuid(), new MemoryBudgetStore(), initialBudget);
            var account = model.FindEntity<Account>(initialBudget.Accounts[0].EntityID);

            Assert.That(account.Transactions.IsLoaded, Is.False);
            Assert.Throws<InvalidBudgetActionException>(() => { int count = account.Transactions.Count; });
            Assert.Throws<InvalidBudgetActionException>(() => { foreach (var t in account.Transactions) { } });
        }

        [Test]
        public void CanLoadChildEntityCollection()
        {
            Budget initialBudget = CreateInitialBudget();

            var model = BudgetModel.CreateNew(Guid.NewGuid(), new MemoryBudgetStore(), initialBudget);
            var getBudget = model.GetBudget();

            getBudget.Accounts.LoadCollection();

            Assert.That(getBudget.Accounts.IsLoaded, Is.True);
            Assert.That(getBudget.Accounts.Count, Is.EqualTo(1));
        }

        [Test]
        public void SubEntity_IsNotAttached_OnCreate_WhenParentIsNotAttached()
        {
            Transaction transaction = new Transaction();
            transaction.MakeSplitTransaction();
            var subTransaction = transaction.SubTransactions.Create();

            Assert.That(transaction.IsAttached, Is.False);
            Assert.That(transaction.SaveState, Is.EqualTo(EntitySaveState.Unattached));
            Assert.That(subTransaction.IsAttached, Is.False);
            Assert.That(subTransaction.SaveState, Is.EqualTo(EntitySaveState.Unattached));
        }

        [Test]
        public void SubEntity_Unattached_IsAttachedWithParent()
        {
            Transaction transaction = new Transaction();
            transaction.MakeSplitTransaction();
            var subTransaction = transaction.SubTransactions.Create();

            Budget initialBudget = CreateInitialBudget();
            var model = BudgetModel.CreateNew(Guid.NewGuid(), new MemoryBudgetStore(), initialBudget);
            initialBudget.Accounts[0].Transactions.Add(transaction);
            model.SaveChanges();

            Assert.That(subTransaction.IsAttached, Is.True);
            Assert.That(subTransaction.SaveState, Is.EqualTo(EntitySaveState.AttachedNoChanges));
        }

        [Test]
        public void SubEntity_IsAttached_OnCreate_WhenParentIsAttached()
        {
            Transaction transaction = new Transaction();

            Budget initialBudget = CreateInitialBudget();
            var model = BudgetModel.CreateNew(Guid.NewGuid(), new MemoryBudgetStore(), initialBudget);

            initialBudget.Accounts[0].Transactions.Add(transaction);
            transaction.MakeSplitTransaction();
            model.SaveChanges();

            Assert.That(transaction.IsAttached, Is.True);
            Assert.That(transaction.SaveState, Is.EqualTo(EntitySaveState.AttachedNoChanges));

            var subTransaction = transaction.SubTransactions.Create();

            Assert.That(subTransaction.IsAttached, Is.True);
            Assert.That(subTransaction.SaveState, Is.EqualTo(EntitySaveState.AttachedHasChanges));
        }

        [Test]
        public void SubEntity_CopiesStayInSync()
        {
            Transaction transaction = new Transaction();

            Budget initialBudget = CreateInitialBudget();
            var model = BudgetModel.CreateNew(Guid.NewGuid(), new MemoryBudgetStore(), initialBudget);

            initialBudget.Accounts[0].Transactions.Add(transaction);
            transaction.MakeSplitTransaction();
            transaction.Amount = 100;

            var subTransaction = transaction.SubTransactions.Create();
            subTransaction.Amount = 100;
            model.SaveChanges();

            var accountCopy = model.FindEntity<Account>(initialBudget.Accounts[0].EntityID);
            accountCopy.Transactions.LoadCollection();
            var transactionCopy = accountCopy.Transactions[0];
            var subTransactionCopy = transactionCopy.SubTransactions[0];

            transactionCopy.Amount = 150;
            subTransactionCopy.Amount = 150;
            model.SaveChanges();

            Assert.That(subTransactionCopy.Amount, Is.EqualTo(subTransaction.Amount));
            Assert.That(transactionCopy.Amount, Is.EqualTo(transaction.Amount));

            var subTransactionCopy2 = transactionCopy.SubTransactions.Create();
            subTransactionCopy.Amount = 75;
            subTransactionCopy2.Amount = 75;

            model.SaveChanges();
            Assert.That(transaction.SubTransactions, Has.Count.EqualTo(2));

            var subTransaction2 = transaction.SubTransactions.Where(st => st.EntityID == subTransactionCopy2.EntityID).Single();

            Assert.That(subTransactionCopy.Amount, Is.EqualTo(subTransaction.Amount));
            Assert.That(subTransactionCopy2.Amount, Is.EqualTo(subTransaction2.Amount));
            Assert.That(transactionCopy.Amount, Is.EqualTo(transaction.Amount));
        }

        [Test]
        public void ParentsAndChildrenInTree_ResolveToActualEntityAndNotCopy()
        {
            Assert.That(true, Is.False);
        }

        [Test]
        public void CanUnloadChildEntityCollection()
        {
            Assert.That(true, Is.False);
        }

        [Test]
        public void ChildEntityCollections_EntitiesInsideAreAttached()
        {
            Assert.That(true, Is.False);
        }
    }
}
