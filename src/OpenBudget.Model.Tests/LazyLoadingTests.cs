using NUnit.Framework;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Infrastructure.Entities;
using OpenBudget.Model.Infrastructure.UnitOfWork;
using OpenBudget.Tests.Shared;
using System.Linq;

namespace OpenBudget.Model.Tests
{
    /// <summary>
    /// Tests related to lazy loading entities and entity collections
    /// from the snapshot store, and coordinating save changes between different 
    /// copies of entities that exist.
    /// </summary>
    [TestFixture(BudgetBackends.Memory)]
    [TestFixture(BudgetBackends.SQLite)]
    public class LazyLoadingTests
    {
        private readonly BudgetBackends _backend;
        private BudgetModel Model { get; set; }
        private Budget InitialBudget { get; set; }

        public LazyLoadingTests(BudgetBackends backend)
        {
            _backend = backend;
        }

        [SetUp]
        public void Setup()
        {
            InitialBudget = CreateInitialBudget();
            Model = BudgetSetup.CreateModelFrom(InitialBudget, _backend);
        }

        [TearDown]
        public void TearDown()
        {
            InitialBudget = null;
            Model?.Dispose();
            Model = null;
        }

        public static Budget CreateInitialBudget()
        {
            Budget initialBudget = new Budget();
            initialBudget.Name = "My Budget";
            Account account = new Account() { Name = "Checking" };
            initialBudget.Accounts.Add(account);

            return initialBudget;
        }

        [Test]
        public void InitialBudget_IsAttachedAfterCreate()
        {
            Account account = InitialBudget.Accounts[0];

            Assert.That(Model.EventStore.GetEvents().Count(), Is.EqualTo(2));
            Assert.That(InitialBudget.SaveState, Is.EqualTo(EntitySaveState.AttachedNoChanges));
            Assert.That(account.SaveState, Is.EqualTo(EntitySaveState.AttachedNoChanges));
        }

        [Test]
        public void InitialBudget_And_GetBudget_AreDifferentCopies()
        {
            var getBudget = Model.GetBudget();
            Assert.That(getBudget.Name, Is.EqualTo(InitialBudget.Name));

            getBudget.Name = "New Name";
            Assert.That(getBudget, Is.Not.EqualTo(InitialBudget));
            Assert.That(getBudget.SaveState, Is.EqualTo(EntitySaveState.AttachedHasChanges));
            Assert.That(getBudget.Name, Is.Not.EqualTo(InitialBudget.Name));
            Assert.That(InitialBudget.SaveState, Is.EqualTo(EntitySaveState.AttachedNoChanges));
        }

        [Test]
        public void ChangingBudgetName_SetsStateTo_AttachedHasChanges()
        {
            var getBudget = Model.GetBudget();
            getBudget.Name = "New Name";

            Assert.That(getBudget.SaveState, Is.EqualTo(EntitySaveState.AttachedHasChanges));
        }

        [Test]
        public void ChangingBudgetName_RegistersEntityIn_UnitOfWork()
        {
            var getBudget = Model.GetBudget();
            getBudget.Name = "New Name";

            UnitOfWork unitOfWork = Model.GetCurrentUnitOfWork();
            Assert.That(unitOfWork.ContainsEntity(getBudget), Is.True);
        }

        [Test]
        public void MultipleEntityCopies_ReceiveUpdates()
        {
            var getBudget = Model.GetBudget();
            getBudget.Name = "New Name";

            Model.SaveChanges();
            Assert.That(InitialBudget.Name, Is.EqualTo(getBudget.Name));
            Assert.That(InitialBudget.SaveState, Is.EqualTo(EntitySaveState.AttachedNoChanges));
            Assert.That(getBudget.SaveState, Is.EqualTo(EntitySaveState.AttachedNoChanges));
        }

        [Test]
        public void ChildEntityCollection_Starts_Unattached()
        {
            //Purposefully use local and unattached initial budget rather than the copy on
            //the test fixture since that one was attached already
            var initialBudget = CreateInitialBudget();

            Assert.That(initialBudget.Accounts.CollectionState, Is.EqualTo(EntityCollectionState.Unattached));
        }

        [Test]
        public void ChildEntityCollection_IsAttachedLoaded_AfterCreate()
        {
            Assert.That(InitialBudget.Accounts.CollectionState, Is.EqualTo(EntityCollectionState.Attached));
            Assert.That(InitialBudget.Accounts.IsLoaded, Is.True);
        }

        [Test]
        public void ChildEntityCollectionCopy_StartsUnloaded()
        {
            var getBudget = Model.GetBudget();

            Assert.That(getBudget.Accounts.CollectionState, Is.EqualTo(EntityCollectionState.Attached));
            Assert.That(getBudget.Accounts.IsLoaded, Is.False);
        }

        [Test]
        public void UnloadedEntityCollectionThrowsException()
        {
            var account = Model.FindEntity<Account>(InitialBudget.Accounts[0].EntityID);

            Assert.That(account.Transactions.IsLoaded, Is.False);
            Assert.Throws<InvalidBudgetActionException>(() => { int count = account.Transactions.Count; });
            Assert.Throws<InvalidBudgetActionException>(() => { foreach (var t in account.Transactions) { } });
        }

        [Test]
        public void CanLoadChildEntityCollection()
        {
            var getBudget = Model.GetBudget();

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

            InitialBudget.Accounts[0].Transactions.Add(transaction);
            Model.SaveChanges();

            Assert.That(subTransaction.IsAttached, Is.True);
            Assert.That(subTransaction.SaveState, Is.EqualTo(EntitySaveState.AttachedNoChanges));
        }

        [Test]
        public void SubEntity_IsAttached_OnCreate_WhenParentIsAttached()
        {
            Transaction transaction = new Transaction();

            InitialBudget.Accounts[0].Transactions.Add(transaction);
            transaction.MakeSplitTransaction();
            Model.SaveChanges();

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


            InitialBudget.Accounts[0].Transactions.Add(transaction);
            transaction.MakeSplitTransaction();
            transaction.Amount = 100;

            var subTransaction = transaction.SubTransactions.Create();
            subTransaction.Amount = 100;
            Model.SaveChanges();

            var accountCopy = Model.FindEntity<Account>(InitialBudget.Accounts[0].EntityID);
            accountCopy.Transactions.LoadCollection();
            var transactionCopy = accountCopy.Transactions[0];
            var subTransactionCopy = transactionCopy.SubTransactions[0];

            transactionCopy.Amount = 150;
            subTransactionCopy.Amount = 150;
            Model.SaveChanges();

            Assert.That(subTransactionCopy.Amount, Is.EqualTo(subTransaction.Amount));
            Assert.That(transactionCopy.Amount, Is.EqualTo(transaction.Amount));

            var subTransactionCopy2 = transactionCopy.SubTransactions.Create();
            subTransactionCopy.Amount = 75;
            subTransactionCopy2.Amount = 75;

            Model.SaveChanges();
            Assert.That(transaction.SubTransactions, Has.Count.EqualTo(2));

            var subTransaction2 = transaction.SubTransactions.Where(st => st.EntityID == subTransactionCopy2.EntityID).Single();

            Assert.That(subTransactionCopy.Amount, Is.EqualTo(subTransaction.Amount));
            Assert.That(subTransactionCopy2.Amount, Is.EqualTo(subTransaction2.Amount));
            Assert.That(transactionCopy.Amount, Is.EqualTo(transaction.Amount));
        }

        [Test]
        public void EntitiesInTree_ResolveToRightEntityAndNotAnotherCopy()
        {
            //First Case: Parent To Child
            var budget = Model.GetBudget();
            budget.Accounts.EnsureCollectionLoaded();

            var account = budget.Accounts[0];
            Assert.That(account.Parent, Is.EqualTo(budget));

            //Second Case: Child To Parent
            account = Model.FindEntity<Account>(account.EntityID);
            budget = (Budget)account.Parent;
            Assert.That(budget.Accounts.IsLoaded, Is.False);
            budget.Accounts.EnsureCollectionLoaded();
            Assert.That(budget.Accounts, Has.Member(account));
        }

        [Test]
        public void ChildEntityCollections_EntitiesInsideAreAttached()
        {
            var budget = Model.GetBudget();
            budget.Accounts.EnsureCollectionLoaded();

            Assert.That(budget.Accounts.CollectionState, Is.EqualTo(EntityCollectionState.Attached));

            Account account = new Account();
            Assert.That(account.SaveState, Is.EqualTo(EntitySaveState.Unattached));

            budget.Accounts.Add(account);
            Assert.That(account.SaveState, Is.EqualTo(EntitySaveState.UnattachedRegistered));

            Model.SaveChanges();
            Assert.That(account.SaveState, Is.EqualTo(EntitySaveState.AttachedNoChanges));
        }

        [Test]
        public void EntityCollection_ParentEventHasNoEffectWhenCollectionNotLoaded()
        {
            var budgetCopy = Model.GetBudget();
            //budgetCopy.Accounts.EnsureCollectionLoaded();

            Account account = new Account();
            Transaction transaction = new Transaction();
            account.Transactions.Add(transaction);
            InitialBudget.Accounts.Add(account);
            Model.SaveChanges();

            var internalAccounts = budgetCopy.Accounts.GetInternalCollection();

            Assert.That(internalAccounts.Count, Is.EqualTo(0));

            //Check for a bug where EntityCollection loaded an entity when collection was not in
            //loaded state and then a subsequent load would throw an exception.
            Assert.That(() =>
            {
                budgetCopy.Accounts.EnsureCollectionLoaded();
            }, Throws.Nothing);

            Assert.That(budgetCopy.Accounts.Count, Is.EqualTo(2));
            Assert.That(budgetCopy.Accounts.Select(a => a.EntityID), Has.Member(account.EntityID));
        }
    }
}
