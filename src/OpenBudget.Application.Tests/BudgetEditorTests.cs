using NUnit.Framework;
using OpenBudget.Application.ViewModels.BudgetEditor;
using OpenBudget.Tests.Shared;
using System;

namespace OpenBudget.Application.Tests
{
    [TestFixture]
    public class BudgetEditorTests
    {
        public BudgetEditorTests()
        {

        }

        public TestBudget TestBudget { get; private set; }

        [SetUp]
        public void Setup()
        {
            TestBudget = BudgetSetup.CreateBudget(BudgetBackends.SQLite);
        }

        [TearDown]
        public void Teardown()
        {
            TestBudget?.BudgetModel?.Dispose();
            TestBudget = null;
        }

        [Test]
        public void TestBudgetEditor()
        {
            var budgetEditor = new BudgetEditorViewModel(TestBudget.BudgetModel);
            budgetEditor.MakeNumberOfMonthsVisible(5);

        }
    }
}
