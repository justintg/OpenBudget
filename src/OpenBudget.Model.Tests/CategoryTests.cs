﻿using NUnit.Framework;
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
        public void AddNewCategoryTo_Loaded_CollectionAssignsLastSortOrder()
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

        [Test]
        public void AddNewCategoryTo_UnLoaded_CollectionAssignsLastSortOrder()
        {
            var masterCategory = TestBudget.Budget.MasterCategories[0];
            Assert.That(masterCategory.Categories.CollectionState, Is.EqualTo(EntityCollectionState.Attached));
            Assert.That(masterCategory.Categories.IsLoaded, Is.True);

            int initialCount = masterCategory.Categories.Count;

            Category category = new Category()
            {
                Name = "NewCategory"
            };

            var unloadedMasterCategory = TestBudget.BudgetModel.FindEntity<MasterCategory>(masterCategory.EntityID);
            Assert.That(unloadedMasterCategory.Categories.CollectionState, Is.EqualTo(EntityCollectionState.Attached));
            Assert.That(unloadedMasterCategory.Categories.IsLoaded, Is.False);

            unloadedMasterCategory.Categories.Add(category);
            TestBudget.SaveChanges();

            Assert.That(category.SortOrder, Is.EqualTo(initialCount));
        }

        [Test]
        public void AddMultipleNewCategoryTo_UnLoaded_CollectionAssignsLastSortOrder()
        {
            var masterCategory = TestBudget.Budget.MasterCategories[0];

            int initialCount = masterCategory.Categories.Count;

            Category category = new Category()
            {
                Name = "NewCategory"
            };

            Category category2 = new Category()
            {
                Name = "NewCategory2"
            };

            var unloadedMasterCategory = TestBudget.BudgetModel.FindEntity<MasterCategory>(masterCategory.EntityID);
            Assert.That(unloadedMasterCategory.Categories.CollectionState, Is.EqualTo(EntityCollectionState.Attached));
            Assert.That(unloadedMasterCategory.Categories.IsLoaded, Is.False);

            unloadedMasterCategory.Categories.Add(category);
            unloadedMasterCategory.Categories.Add(category2);
            TestBudget.SaveChanges();

            Assert.That(category.SortOrder, Is.EqualTo(initialCount));
            Assert.That(category2.SortOrder, Is.EqualTo(initialCount + 1));

            Category category3 = new Category()
            {
                Name = "NewCategory3"
            };

            unloadedMasterCategory.Categories.Add(category3);
            TestBudget.SaveChanges();
            Assert.That(category3.SortOrder, Is.EqualTo(initialCount + 2));
        }

        [Test]
        public void AddMultipleNewCategoryTo_Loaded_CollectionAssignsLastSortOrder()
        {
            var masterCategory = TestBudget.Budget.MasterCategories[0];
            Assert.That(masterCategory.Categories.CollectionState, Is.EqualTo(EntityCollectionState.Attached));
            Assert.That(masterCategory.Categories.IsLoaded, Is.True);

            int initialCount = masterCategory.Categories.Count;

            Category category = new Category()
            {
                Name = "NewCategory"
            };

            Category category2 = new Category()
            {
                Name = "NewCategory2"
            };

            masterCategory.Categories.Add(category);
            masterCategory.Categories.Add(category2);
            TestBudget.SaveChanges();

            Assert.That(category.SortOrder, Is.EqualTo(initialCount));
            Assert.That(category2.SortOrder, Is.EqualTo(initialCount + 1));
        }

        [Test]
        public void MovingCategoryBetweenMasterCategories_UpdatesSortingInBothCollections()
        {
            var masterCategory = TestBudget.Budget.MasterCategories[0];
            var masterCategory2 = TestBudget.Budget.MasterCategories[1];

            var categoryToMove = masterCategory.Categories[1];
            Assert.That(masterCategory.Categories.Count, Is.EqualTo(4));
            Assert.That(masterCategory2.Categories.Count, Is.EqualTo(4));
            Assert.That(categoryToMove.SortOrder, Is.EqualTo(1));

            masterCategory2.Categories.Add(categoryToMove);
            TestBudget.SaveChanges();

            Assert.That(categoryToMove.SortOrder, Is.EqualTo(4));

            for (int i = 0; i < masterCategory.Categories.Count; i++)
            {
                Assert.That(masterCategory.Categories[i].SortOrder, Is.EqualTo(i));
            }
        }

        [Test]
        public void MoveAndAddCategoriesOnSameCollection_ProducesCorrectSorting()
        {
            var masterCategory = TestBudget.Budget.MasterCategories[0];
            var masterCategory2 = TestBudget.Budget.MasterCategories[1];

            var categoryToMove = masterCategory.Categories[1];
            Assert.That(masterCategory.Categories.Count, Is.EqualTo(4));
            Assert.That(masterCategory2.Categories.Count, Is.EqualTo(4));
            Assert.That(categoryToMove.SortOrder, Is.EqualTo(1));

            var categoryBefore = new Category() { Name = "Before" };
            var categoryAfter = new Category() { Name = "After" };
            masterCategory2.Categories.Add(categoryBefore);
            masterCategory2.Categories.Add(categoryToMove);
            masterCategory2.Categories.Add(categoryAfter);
            TestBudget.SaveChanges();

            Assert.That(categoryBefore.SortOrder, Is.EqualTo(4));
            Assert.That(categoryToMove.SortOrder, Is.EqualTo(5));
            Assert.That(categoryAfter.SortOrder, Is.EqualTo(6));
        }

        [Test]
        public void MoveAndAddCategoriesOnSameCollection_ProducesCorrectSorting_WhenCollectionIsUnloaded()
        {
            var masterCategory = TestBudget.Budget.MasterCategories[0];
            var masterCategory2 = TestBudget.Budget.MasterCategories[1];

            var unloadedMasterCategory2 = TestBudget.BudgetModel.FindEntity<MasterCategory>(masterCategory2.EntityID);

            var categoryToMove = masterCategory.Categories[1];
            Assert.That(masterCategory.Categories.Count, Is.EqualTo(4));
            Assert.That(masterCategory2.Categories.Count, Is.EqualTo(4));
            Assert.That(categoryToMove.SortOrder, Is.EqualTo(1));

            var categoryBefore = new Category() { Name = "Before" };
            var categoryAfter = new Category() { Name = "After" };
            unloadedMasterCategory2.Categories.Add(categoryBefore);
            unloadedMasterCategory2.Categories.Add(categoryToMove);
            unloadedMasterCategory2.Categories.Add(categoryAfter);
            TestBudget.SaveChanges();

            Assert.That(categoryBefore.SortOrder, Is.EqualTo(4));
            Assert.That(categoryToMove.SortOrder, Is.EqualTo(5));
            Assert.That(categoryAfter.SortOrder, Is.EqualTo(6));
        }

        [Test]
        public void AttemptingToMoveACategoryWhenItsMasterCategory_CategoryCollectionIsNotLoadedShouldThrowException()
        {
            var masterCategory = TestBudget.Budget.MasterCategories[0];
            var masterCategory2 = TestBudget.Budget.MasterCategories[1];

            var categoryToMove = TestBudget.BudgetModel.FindEntity<Category>(masterCategory.Categories[1].EntityID);

            Assert.That(masterCategory2.Categories.Count, Is.EqualTo(4));
            Assert.That(categoryToMove.SortOrder, Is.EqualTo(1));

            Assert.Throws<InvalidBudgetActionException>(() => { masterCategory2.Categories.Add(categoryToMove); });
        }

        [Test]
        public void CanUpdateSortOrder_Back_WithinOriginalParent()
        {
            var masterCategory = TestBudget.Budget.MasterCategories[0];

            var category = masterCategory.Categories[2];
            category.SetSortOrder(1);

            Assert.That(masterCategory.Categories[0].SortOrder, Is.EqualTo(0));
            Assert.That(masterCategory.Categories[1].SortOrder, Is.EqualTo(2));
            Assert.That(masterCategory.Categories[2].SortOrder, Is.EqualTo(1));
            Assert.That(masterCategory.Categories[3].SortOrder, Is.EqualTo(3));
        }

        [Test]
        public void CanUpdateSortOrder_Forward_WithinOriginalParent()
        {
            var masterCategory = TestBudget.Budget.MasterCategories[0];

            var category = masterCategory.Categories[0];
            category.SetSortOrder(2);

            Assert.That(masterCategory.Categories[0].SortOrder, Is.EqualTo(1));
            Assert.That(masterCategory.Categories[1].SortOrder, Is.EqualTo(0));
            Assert.That(masterCategory.Categories[2].SortOrder, Is.EqualTo(2));
            Assert.That(masterCategory.Categories[3].SortOrder, Is.EqualTo(3));
        }
    }
}
