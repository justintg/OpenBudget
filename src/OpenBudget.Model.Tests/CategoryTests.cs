using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenBudget.Model.Tests
{
    [TestFixture]
    public class CategoryTests
    {
        TestBudget TestBudget;

        [SetUp]
        public void Setup()
        {
            TestBudget = BudgetSetup.CreateBudget();
        }

        [Test]
        public void CategoriesAreAssignedSortOrderAtInitialSave()
        {
            var categories = TestBudget.Budget.MasterCategories[0].Categories.ToList();
            for(int i = 0; i < categories.Count; i++)
            {
                Assert.That(categories[i].SortOrder, Is.EqualTo(i));
            }
        }
    }
}
