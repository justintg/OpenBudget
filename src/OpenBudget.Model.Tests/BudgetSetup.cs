using OpenBudget.Model.Synchronization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Events;
using OpenBudget.Model.BudgetStore;
using NUnit.Framework.Internal;
using Microsoft.Data.Sqlite;
using OpenBudget.Model.SQLite;

namespace OpenBudget.Model.Tests
{
    public class TestBudget
    {
        public IBudgetStore BudgetStore;
        public TestEventStore EventStore;
        public BudgetModel BudgetModel;
        public Guid DeviceID;
        public Budget Budget;
        public List<ModelEvent> TestEvents;

        public void ReloadBudget()
        {
            BudgetModel = BudgetModel.Load(DeviceID, BudgetStore);
            Budget = BudgetModel.GetBudget();
        }

        public void ClearEvents()
        {
            EventStore.TestEvents.Clear();
        }

        public void SaveChanges()
        {
            BudgetModel.SaveChanges();
        }
    }

    public enum BudgetBackends
    {
        Memory = 1,
        SQLite = 2
    }

    public class BudgetSetup
    {
        public static TestBudget CreateBudget(BudgetBackends budgetBackend)
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

            var initialBudget = InitializeBudget();
            testBudget.BudgetModel = BudgetModel.CreateNew(testBudget.DeviceID, testBudget.BudgetStore, initialBudget);
            testBudget.BudgetModel.SaveChanges();

            testBudget.EventStore.TestEvents.Clear();
            testBudget.TestEvents = testBudget.EventStore.TestEvents;
            testBudget.Budget = initialBudget;

            return testBudget;
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
