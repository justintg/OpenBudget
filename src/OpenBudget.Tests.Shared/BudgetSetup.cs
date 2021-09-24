using Microsoft.Data.Sqlite;
using OpenBudget.Model;
using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.Entities;
using OpenBudget.Model.SQLite;
using System;

namespace OpenBudget.Tests.Shared
{
    public enum BudgetBackends
    {
        Memory = 1,
        SQLite = 2
    }

    public class BudgetSetup
    {
        public static TestBudget CreateBudget(BudgetBackends budgetBackend, Budget initialBudget = null)
        {
            TestBudget testBudget = null;
            if (budgetBackend == BudgetBackends.Memory)
            {
                testBudget = new TestBudget();
                testBudget.DeviceID = Guid.NewGuid();

                testBudget.EventStore = new TestEventStore(new MemoryEventStore());
                testBudget.BudgetStore = new MemoryBudgetStore(testBudget.EventStore);
            }
            else if (budgetBackend == BudgetBackends.SQLite)
            {
                testBudget = new TestBudget();
                testBudget.DeviceID = Guid.NewGuid();

                SqliteConnectionStringBuilder builder = new SqliteConnectionStringBuilder();
                builder.Mode = SqliteOpenMode.Memory;
                builder.Cache = SqliteCacheMode.Shared;
                builder.DataSource = "BudgetTests";

                testBudget.BudgetStore = new SQLiteBudgetStore(testBudget.DeviceID, builder.ToString(), (es) => new TestEventStore(es));
                testBudget.EventStore = testBudget.BudgetStore.EventStore as TestEventStore;
            }

            if (initialBudget == null)
            {
                initialBudget = InitializeBudget();
            }

            testBudget.BudgetModel = BudgetModel.CreateNew(testBudget.DeviceID, testBudget.BudgetStore, initialBudget);
            testBudget.BudgetModel.SaveChanges();

            testBudget.EventStore.TestEvents.Clear();
            testBudget.TestEvents = testBudget.EventStore.TestEvents;
            testBudget.Budget = initialBudget;

            return testBudget;
        }

        public static BudgetModel CreateModelFrom(Budget initialBudget, BudgetBackends budgetBackend)
        {
            var testBudget = CreateBudget(budgetBackend, initialBudget);
            return testBudget.BudgetModel;
        }

        private static Budget InitializeBudget()
        {
            Budget budget = new Budget();
            budget.Accounts.Add(new Account() { Name = "Checking" });
            budget.Accounts.Add(new Account() { Name = "Savings" });
            budget.Accounts.Add(new Account() { Name = "Other" });

            budget.MasterCategories.Add(CreateCategory("Monthly Bills", new string[]
            {
                "Mortgage",
                "Electricity",
                "Phone",
                "Property Taxes"
            }));

            budget.MasterCategories.Add(CreateCategory("Everyday Expenses", new string[]
            {
                "Groceries",
                "Household Goods",
                "Clothing",
                "Restaurants"
            }));

            return budget;
        }

        private static MasterCategory CreateCategory(string name, string[] subCategories)
        {
            var category = new MasterCategory() { Name = name };
            foreach (string subCategoryName in subCategories)
            {
                var subCategory = new Category() { Name = subCategoryName };
                category.Categories.Add(subCategory);
            }
            return category;
        }
    }
}
