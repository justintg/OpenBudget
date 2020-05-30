using NUnit.Framework;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Infrastructure.Entities;
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
            for (int i = 0; i < categories.Count; i++)
            {
                Assert.That(categories[i].SortOrder, Is.EqualTo(i));
            }
        }

        [Test]
        public void AddNewCategoryToLoadedCollectionAssignsLastSortOrder()
        {
            var masterCategory = TestBudget.Budget.MasterCategories[0];
            Assert.That(masterCategory.Categories.CollectionState, Is.EqualTo(EntityCollectionState.Attached));
            Assert.That(masterCategory.Categories.IsLoaded, Is.True);

            int initialCount = masterCategory.Categories.Count;

            Category category = new Category()
            {
                Name = "NewCategory"
            };

            masterCategory.Categories.Add(category);
            TestBudget.SaveChanges();

            Assert.That(category.SortOrder, Is.EqualTo(initialCount));
        }
    }
}
